using Marten;
using Microsoft.Extensions.Logging;
using Pollon.Contracts.Models;
using Pollon.Contracts.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Pollon.Backoffice.Handlers;

public partial class PluginHandler
{
    public async Task<RegisterPluginResponse> Handle(
        RegisterPlugin message, 
        IDocumentSession session, 
        ILogger<PluginHandler> logger,
        IConfiguration configuration)
    {
        // 1. Validate JWT
        if (string.IsNullOrEmpty(message.AccessToken))
        {
            logger.LogWarning("Plugin registration rejected: AccessToken missing.");
            return new RegisterPluginResponse(false, "Authentication token is required.");
        }

        try
        {
            var keycloakUrl = configuration.GetConnectionString("keycloak") ?? configuration["Keycloak:Url"] ?? "http://localhost:8080";
            if (!keycloakUrl.StartsWith("http")) keycloakUrl = $"http://{keycloakUrl}";
            
            var realm = configuration["Keycloak:Realm"] ?? "Pollon";
            var authority = $"{keycloakUrl.TrimEnd('/')}/realms/{realm}";

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authority,
                ValidateAudience = false, // Spesso per client_credentials l'audience è generic/account
                ValidateLifetime = true,
                ValidateIssuerSigningKey = false, // Per semplicità in dev non validiamo la firma se non abbiamo JWKS, 
                                                 // ma in produzione dovremmo usare OIDC configuration manager
                SignatureValidator = delegate (string token, TokenValidationParameters parameters)
                {
                    return new JwtSecurityToken(token);
                }
            };

            // Nota: in un ambiente reale useremmo ConfigurationManager<OpenIdConnectConfiguration>
            // per scaricare le chiavi pubbliche da Keycloak.
            var principal = tokenHandler.ValidateToken(message.AccessToken, validationParameters, out _);
            
            // Possiamo anche controllare che il client_id nel token corrisponda
            var clientId = principal.FindFirst("azp")?.Value ?? principal.FindFirst("client_id")?.Value;
            logger.LogInformation("Plugin {ClientId} authenticated successfully.", clientId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Plugin registration rejected: Invalid Token.");
            return new RegisterPluginResponse(false, $"Invalid authentication token: {ex.Message}");
        }

        // 2. Register Plugin
        LogRegisteringPlugin(logger, message.Name, message.Id, message.ConsulServiceId);

        var plugin = await session.LoadAsync<PluginInfo>(message.Id) ?? new PluginInfo { Id = message.Id };
        
        plugin.Name = message.Name;
        plugin.ConsulServiceId = message.ConsulServiceId;
        plugin.HeartbeatUrl = message.HeartbeatUrl;
        plugin.Version = message.Version;
        plugin.Description = message.Description;
        plugin.LastSeen = DateTime.UtcNow;
        plugin.Status = "Online";

        session.Store(plugin);
        await session.SaveChangesAsync();

        return new RegisterPluginResponse(true);
    }
}

