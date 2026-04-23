using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Pollon.Publication.Models;

namespace Pollon.Backoffice.Web;

public class BackofficeApiClient(
    HttpClient httpClient, 
    TokenProvider tokenProvider, 
    NavigationManager navigationManager,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory)
{
    private static readonly System.Text.Json.JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await SendWithAuthAsync(request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(_options, cancellationToken: ct);
        }
        return default;
    }

    private async Task<HttpResponseMessage> SendWithAuthAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // 1. Inject Access Token
        if (!string.IsNullOrEmpty(tokenProvider.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);
        }

        var response = await httpClient.SendAsync(request, ct);

        // 2. Handle 401 & Refresh
        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(tokenProvider.RefreshToken))
        {
            Console.WriteLine($"[AUTH-DEBUG] BackofficeApiClient: 401 Unauthorized from {request.RequestUri}. Attempting refresh...");
            var refreshed = await RefreshTokensAsync(ct);
            if (refreshed)
            {
                Console.WriteLine("[AUTH-DEBUG] BackofficeApiClient: Refresh successful. Retrying original request...");
                using var retryRequest = await CloneRequestAsync(request);
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);
                return await httpClient.SendAsync(retryRequest, ct);
            }

            Console.WriteLine("[AUTH-DEBUG] BackofficeApiClient: Refresh failed. Redirecting to logout.");
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

            // Important: Use a clean client for refresh to avoid cycles
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
                var result = await response.Content.ReadFromJsonAsync<TokenResponse>(_options, cancellationToken: ct);
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
            Console.WriteLine($"[AUTH-ERROR] BackofficeApiClient refresh failure: {ex.Message}");
        }

        return false;
    }

    private async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
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

    // --- API Methods ---

    public async Task<ContentType[]> GetContentTypesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<ContentType[]>("/api/content-types", cancellationToken) ?? [];
    }

    public async Task<ContentType?> GetContentTypeByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ContentType>($"/api/content-types/{id}", cancellationToken);
    }

    public async Task CreateContentTypeAsync(ContentType item, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/content-types")
        {
            Content = JsonContent.Create(item)
        };
        await SendWithAuthAsync(request, cancellationToken);
    }

    public async Task UpdateContentTypeAsync(string id, ContentType item, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/content-types/{id}")
        {
            Content = JsonContent.Create(item)
        };
        await SendWithAuthAsync(request, cancellationToken);
    }

    public async Task DeleteContentTypeAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/content-types/{id}");
        await SendWithAuthAsync(request, cancellationToken);
    }

    public async Task<ContentItem[]> GetContentItemsAsync(
        string? status = null, 
        string? sortBy = null, 
        bool sortDescending = true, 
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/content-items?sortDescending={sortDescending}";
        if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
        if (!string.IsNullOrEmpty(sortBy)) url += $"&sortBy={Uri.EscapeDataString(sortBy)}";
        if (!string.IsNullOrEmpty(searchTerm)) url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";

        return await GetAsync<ContentItem[]>(url, cancellationToken) ?? [];
    }

    public async Task<ContentItem?> GetContentItemByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ContentItem>($"/api/content-items/{id}", cancellationToken);
    }

    public async Task CreateContentItemAsync(ContentItem item, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/content-items")
        {
            Content = JsonContent.Create(item)
        };
        await SendWithAuthAsync(request, cancellationToken);
    }

    public async Task UpdateContentItemAsync(string id, ContentItem item, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/content-items/{id}")
        {
            Content = JsonContent.Create(item)
        };
        await SendWithAuthAsync(request, cancellationToken);
    }

    public async Task DeleteContentItemAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/content-items/{id}");
        await SendWithAuthAsync(request, cancellationToken);
    }

    public async Task<MediaAsset?> UploadMediaAsync(MultipartFormDataContent content, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/media")
        {
            Content = content
        };
        var response = await SendWithAuthAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<MediaAsset>(_options, cancellationToken);
        }
        return null;
    }

    public async Task<HttpResponseMessage> GetMediaResponseAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/media/{id}");
        return await SendWithAuthAsync(request, cancellationToken);
    }

    public async Task<MediaGallery[]> GetGalleriesAsync(bool includeUnpublished = false, CancellationToken cancellationToken = default)
    {
        return await GetAsync<MediaGallery[]>($"/api/galleries?includeUnpublished={includeUnpublished.ToString().ToLower()}", cancellationToken) ?? [];
    }

    public async Task<MediaGallery?> GetGalleryByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<MediaGallery>($"/api/galleries/{id}", cancellationToken);
    }

    public async Task<MediaGallery?> CreateMediaGalleryAsync(MediaGallery item, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/galleries")
        {
            Content = JsonContent.Create(item)
        };
        var response = await SendWithAuthAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<MediaGallery>(_options, cancellationToken);
        }
        return null;
    }

    public async Task<MediaGallery?> CreateGalleryWithUploadAsync(MultipartFormDataContent content, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/galleries")
        {
            Content = content
        };
        var response = await SendWithAuthAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<MediaGallery>(cancellationToken);
        }
        return null;
    }

    public async Task UpdateGalleryAsync(string id, MediaGallery item, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/galleries/{id}")
        {
            Content = JsonContent.Create(item)
        };
        await SendWithAuthAsync(request, cancellationToken);
    }

    public async Task DeleteGalleryAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/galleries/{id}");
        await SendWithAuthAsync(request, cancellationToken);
    }

    public async Task<Pollon.Contracts.Models.PluginInfo[]> GetPluginsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<Pollon.Contracts.Models.PluginInfo[]>("/api/plugins", cancellationToken) ?? [];
    }

    public async Task UpdatePluginAsync(string id, Pollon.Contracts.Models.PluginInfo plugin, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/plugins/{id}")
        {
            Content = JsonContent.Create(plugin)
        };
        await SendWithAuthAsync(request, cancellationToken);
    }
}
