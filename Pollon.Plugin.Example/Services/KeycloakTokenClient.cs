using System.Net.Http.Json;
using System.Text.Json;

namespace Pollon.Plugin.Example.Services;

public class KeycloakTokenClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private string? _accessToken;
    private DateTime _expiresAt;

    public KeycloakTokenClient(HttpClient httpClient, IConfiguration configuration)
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

        var keycloakUrl = _configuration.GetConnectionString("keycloak") ?? _configuration["Keycloak:Url"] ?? "http://localhost:8080";
        var realm = _configuration["Keycloak:Realm"] ?? "Pollon";
        var clientId = _configuration["Keycloak:ClientId"] ?? "plugin-example";
        var clientSecret = _configuration["Keycloak:ClientSecret"] ?? "plugin-example-secret";

        if (!keycloakUrl.StartsWith("http")) keycloakUrl = $"http://{keycloakUrl}";

        var tokenEndpoint = $"{keycloakUrl.TrimEnd('/')}/realms/{realm}/protocol/openid-connect/token";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret)
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
