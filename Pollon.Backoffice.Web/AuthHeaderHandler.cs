using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace Pollon.Backoffice.Web;

/// <summary>
/// Intercepts outgoing HttpClient requests to inject the Bearer token
/// and handles 401 Unauthorized responses by redirecting to logout.
/// </summary>
public class AuthHeaderHandler(
    TokenProvider tokenProvider, 
    NavigationManager navigationManager, 
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Inject the Access Token if available
        if (!string.IsNullOrEmpty(tokenProvider.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);
        }

        // 2. Perform the request
        var response = await base.SendAsync(request, cancellationToken);

        // 3. Centralized 401 Handling & Refresh Attempt
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(tokenProvider.RefreshToken))
        {
            Console.WriteLine("[AUTH] 401 Unauthorized detected. Attempting to refresh token...");
            
            var refreshed = await RefreshTokensAsync(cancellationToken);
            if (refreshed)
            {
                Console.WriteLine("[AUTH] Token refresh successful. Retrying original request...");
                
                // Clone the request for retry (cannot reuse the same request object)
                using var retryRequest = await CloneRequestAsync(request);
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);
                
                return await base.SendAsync(retryRequest, cancellationToken);
            }

            Console.WriteLine("[AUTH] Token refresh failed. Forcing logout...");
            navigationManager.NavigateTo("/logout", forceLoad: true);
        }

        return response;
    }

    private async Task<bool> RefreshTokensAsync(CancellationToken ct)
    {
        try
        {
            var keycloakUrl = configuration.GetConnectionString("keycloak");
            if (string.IsNullOrEmpty(keycloakUrl))
            {
                keycloakUrl = configuration["services:keycloak:http:0"] ?? "http://localhost:8080";
            }
            if (!keycloakUrl.StartsWith("http")) keycloakUrl = $"http://{keycloakUrl}";

            var tokenEndpoint = $"{keycloakUrl.TrimEnd('/')}/realms/Pollon/protocol/openid-connect/token";

            // We need a clean HttpClient without this handler to avoid recursion
            using var client = httpClientFactory.CreateClient();
            
            var dict = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", tokenProvider.RefreshToken! },
                { "client_id", "backoffice" }
            };

            var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(dict), ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
                if (result != null)
                {
                    tokenProvider.AccessToken = result.access_token;
                    tokenProvider.RefreshToken = result.refresh_token;
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUTH-ERROR] Exception during token refresh: {ex.Message}");
        }

        return false;
    }

    private async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Content = request.Content,
            Version = request.Version
        };
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        foreach (var property in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(property.Key), property.Value);
        }
        if (request.Content != null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
        return clone;
    }

    private class TokenResponse
    {
        public string access_token { get; set; } = "";
        public string refresh_token { get; set; } = "";
    }
}
