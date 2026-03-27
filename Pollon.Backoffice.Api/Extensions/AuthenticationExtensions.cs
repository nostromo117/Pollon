using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Pollon.Backoffice.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddBackofficeAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var keycloakUrl = configuration.GetConnectionString("keycloak");
                if (string.IsNullOrEmpty(keycloakUrl))
                {
                    // Fallback for some Aspire versions/configurations
                    keycloakUrl = configuration["services:keycloak:http:0"] ?? "http://localhost:8080";
                }
                if (!keycloakUrl.StartsWith("http")) keycloakUrl = $"http://{keycloakUrl}";

                options.Authority = $"{keycloakUrl.TrimEnd('/')}/realms/Pollon";
                options.Audience = "backoffice";
                options.RequireHttpsMetadata = false; 
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = false, // Keycloak uses JWKS, metadata handles keys
                    SignatureValidator = delegate (string token, TokenValidationParameters parameters)
                    {
                        var jwt = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token);
                        return jwt;
                    }
                };
            });

        services.AddAuthorization();
        
        return services;
    }
}
