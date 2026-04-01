using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Pollon.Backoffice.Web.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddBackofficeAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            var keycloakUrl = configuration.GetConnectionString("keycloak");
            if (string.IsNullOrEmpty(keycloakUrl))
            {
                // Fallback for some Aspire versions/configurations
                keycloakUrl = configuration["services:keycloak:http:0"] ?? "http://localhost:8080";
            }
            if (!keycloakUrl.StartsWith("http")) keycloakUrl = $"http://{keycloakUrl}";

            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.Authority = $"{keycloakUrl.TrimEnd('/')}/realms/Pollon";
            options.ClientId = "backoffice";
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "preferred_username",
                RoleClaimType = "roles"
            };

            options.Events.OnRedirectToIdentityProviderForSignOut = context =>
            {
                var idToken = context.Properties.GetTokenValue("id_token");
                if (!string.IsNullOrEmpty(idToken))
                {
                    context.ProtocolMessage.IdTokenHint = idToken;
                }
                return System.Threading.Tasks.Task.CompletedTask;
            };
        });

        services.AddAuthorization();
        
        return services;
    }

    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/login", () => Results.Challenge(
            new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" }, 
            [OpenIdConnectDefaults.AuthenticationScheme]));

        endpoints.MapGet("/logout", () => Results.SignOut(
            new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" }, 
            [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));

        return endpoints;
    }
}
