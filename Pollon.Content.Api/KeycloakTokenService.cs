using System.Net.Http.Json;
using System.Text.Json;

namespace Pollon.Content.Api;

public class KeycloakTokenService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private string? _accessToken;
    private DateTime _expiresAt;

    public KeycloakTokenService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> GetTokenAsync()
    {
        if (_accessToken != null && DateTime.UtcNow < _expiresAt.AddSeconds(-30))
        {
            return _accessToken;
        }

        var keycloakUrl = _configuration.GetConnectionString("keycloak");
        if (string.IsNullOrEmpty(keycloakUrl))
        {
             // Fallback for some Aspire versions/configurations
             keycloakUrl = _configuration["services:keycloak:http:0"] ?? "http://localhost:8080";
        }
        if (!keycloakUrl.StartsWith("http")) keycloakUrl = $"http://{keycloakUrl}";

        var tokenEndpoint = $"{keycloakUrl.TrimEnd('/')}/realms/Pollon/protocol/openid-connect/token";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", "contentapi"),
            new KeyValuePair<string, string>("client_secret", "contentapi-secret")
        });

        var response = await _httpClient.PostAsync(tokenEndpoint, content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        _accessToken = json.GetProperty("access_token").GetString();
        var expiresIn = json.GetProperty("expires_in").GetInt32();
        _expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

        return _accessToken!;
    }
}
