using Marten;
using Microsoft.Extensions.Logging;
using Pollon.Contracts.Models;
using Pollon.Contracts.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Pollon.Backoffice.Handlers;

public partial class PluginHandler
{
    public async Task<RegisterPluginResponse> Handle(
        RegisterPlugin message, 
        IDocumentSession session, 
        ILogger<PluginHandler> logger,
        IConfiguration configuration,
        IConfigurationManager<OpenIdConnectConfiguration> oidcManager)
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

            // 1. Fetch OIDC configuration (metadata and public keys)
            var config = await oidcManager.GetConfigurationAsync(CancellationToken.None);

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = config.Issuer,
                IssuerSigningKeys = config.SigningKeys,
                ValidateIssuerSigningKey = true,
                ValidateAudience = false, 
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = tokenHandler.ValidateToken(message.AccessToken, validationParameters, out _);
            
            // Possiamo anche controllare che il client_id nel token corrisponda
            var clientId = principal.FindFirst("azp")?.Value ?? principal.FindFirst("client_id")?.Value;
            logger.LogInformation("Plugin {ClientId} authenticated successfully using OIDC-verified signature.", clientId);
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
        plugin.SupportedContentTypes = message.SupportedContentTypes ?? [];

        session.Store(plugin);
        await session.SaveChangesAsync();

        return new RegisterPluginResponse(true);
    }
}

