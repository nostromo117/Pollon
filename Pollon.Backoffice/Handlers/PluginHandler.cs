using Marten;
using Microsoft.Extensions.Logging;
using Pollon.Contracts.Models;
using Pollon.Contracts.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Pollon.Backoffice.Repositories;

namespace Pollon.Backoffice.Handlers;

public partial class PluginHandler
{
    public async Task<RegisterPluginResponse> Handle(
        RegisterPlugin message, 
        IRepository<PluginInfo> repository, 
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
            
            // 2. Enforce identity: the Id in the message must match the authenticated clientId from the token
            var authenticatedClientId = principal.FindFirst("azp")?.Value ?? principal.FindFirst("client_id")?.Value;
            
            if (string.IsNullOrEmpty(authenticatedClientId))
            {
                logger.LogWarning("Plugin registration rejected: Could not determine clientId from token.");
                return new RegisterPluginResponse(false, "Authentication token is missing identity information.");
            }

            if (authenticatedClientId != message.Id)
            {
                logger.LogWarning("Plugin identity mismatch! Claimed: {ClaimedId}, Authenticated: {AuthenticatedId}", message.Id, authenticatedClientId);
                return new RegisterPluginResponse(false, "Plugin identity mismatch. You must register using your assigned Client ID.");
            }

            logger.LogInformation("Plugin {ClientId} authenticated and identity verified successfully.", authenticatedClientId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Plugin registration rejected: Invalid Token.");
            return new RegisterPluginResponse(false, $"Invalid authentication token: {ex.Message}");
        }

        // 3. Register Plugin
        LogRegisteringPlugin(logger, message.Name, message.Id, message.ConsulServiceId);

        var plugin = await repository.GetByIdAsync(message.Id) ?? new PluginInfo { Id = message.Id };
        
        plugin.Name = message.Name;
        plugin.ConsulServiceId = message.ConsulServiceId;
        plugin.HeartbeatUrl = message.HeartbeatUrl;
        plugin.Version = message.Version;
        plugin.Description = message.Description;
        plugin.LastSeen = DateTime.UtcNow;
        plugin.Status = "Online";
        plugin.SupportedContentTypes = message.SupportedContentTypes ?? [];
        // EnabledContentTypes is managed by the user via UI and should not be overwritten by registration

        if (string.IsNullOrEmpty(plugin.Id) || (await repository.GetByIdAsync(message.Id)) == null)
        {
             await repository.CreateAsync(plugin);
        }
        else 
        {
             await repository.UpdateAsync(plugin.Id, plugin);
        }

        return new RegisterPluginResponse(true);
    }
}
