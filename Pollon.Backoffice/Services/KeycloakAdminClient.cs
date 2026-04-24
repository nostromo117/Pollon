using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Pollon.Backoffice.Services;

public record KeycloakClientCredentials(string ClientId, string ClientSecret);

public interface IKeycloakAdminClient
{
    Task<KeycloakClientCredentials?> CreatePluginClientAsync(string pluginName);
    Task<IEnumerable<string>> GetPluginClientsAsync();
    Task<bool> DeletePluginClientAsync(string clientId);
    Task<KeycloakClientCredentials?> RegeneratePluginSecretAsync(string clientId);
}

public class KeycloakAdminClient : IKeycloakAdminClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakAdminClient> _logger;

    public KeycloakAdminClient(HttpClient httpClient, IConfiguration configuration, ILogger<KeycloakAdminClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private async Task<string?> GetAdminTokenAsync()
    {
        var keycloakUrl = _configuration.GetConnectionString("keycloak") ?? _configuration["Keycloak:Url"] ?? "http://localhost:8080";
        var realm = _configuration["Keycloak:Realm"] ?? "Pollon";
        
        // Use the specialized privileged client
        var clientId = "pollon-identity-manager";
        var clientSecret = "identity-manager-secret"; // In production this would be in KeyVault/UserSecrets

        var tokenUrl = $"{keycloakUrl.TrimEnd('/')}/realms/{realm}/protocol/openid-connect/token";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret)
        });

        var response = await _httpClient.PostAsync(tokenUrl, content);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get Keycloak Admin token. Status: {Status}", response.StatusCode);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return result?.AccessToken;
    }

    public async Task<KeycloakClientCredentials?> CreatePluginClientAsync(string pluginName)
    {
        var token = await GetAdminTokenAsync();
        if (token == null) return null;

        var keycloakUrl = _configuration.GetConnectionString("keycloak") ?? _configuration["Keycloak:Url"] ?? "http://localhost:8080";
        var realm = _configuration["Keycloak:Realm"] ?? "Pollon";
        var adminUrl = $"{keycloakUrl.TrimEnd('/')}/admin/realms/{realm}/clients";

        var newClientId = $"plugin-{pluginName.ToLowerInvariant().Replace(" ", "-")}-{Guid.NewGuid().ToString()[..8]}";

        var clientPayload = new
        {
            clientId = newClientId,
            name = pluginName,
            enabled = true,
            protocol = "openid-connect",
            publicClient = false,
            serviceAccountsEnabled = true,
            clientAuthenticatorType = "client-secret",
            description = "Automated plugin identity created by Pollon Backoffice"
        };

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var response = await _httpClient.PostAsJsonAsync(adminUrl, clientPayload);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to create Keycloak client. Status: {Status}, Error: {Error}", response.StatusCode, error);
            return null;
        }

        // To get the secret, we need to find the internal ID of the client we just created
        // Keycloak returns the location of the new resource in the Location header
        var location = response.Headers.Location;
        if (location == null) return null;

        var secretUrl = $"{location.ToString()}/client-secret";
        var secretResponse = await _httpClient.GetAsync(secretUrl);
        if (!secretResponse.IsSuccessStatusCode) return null;

        var secretResult = await secretResponse.Content.ReadFromJsonAsync<SecretResponse>();
        return new KeycloakClientCredentials(newClientId, secretResult?.Value ?? "");
    }

    public async Task<IEnumerable<string>> GetPluginClientsAsync()
    {
        var token = await GetAdminTokenAsync();
        if (token == null) return Enumerable.Empty<string>();

        var keycloakUrl = _configuration.GetConnectionString("keycloak") ?? _configuration["Keycloak:Url"] ?? "http://localhost:8080";
        var realm = _configuration["Keycloak:Realm"] ?? "Pollon";
        var adminUrl = $"{keycloakUrl.TrimEnd('/')}/admin/realms/{realm}/clients";

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var clients = await _httpClient.GetFromJsonAsync<List<KeycloakClientInfo>>(adminUrl);
        return clients?.Where(c => c.ClientId.StartsWith("plugin-")).Select(c => c.ClientId) ?? Enumerable.Empty<string>();
    }

    public async Task<bool> DeletePluginClientAsync(string clientId)
    {
        var token = await GetAdminTokenAsync();
        if (token == null) return false;

        var keycloakUrl = _configuration.GetConnectionString("keycloak") ?? _configuration["Keycloak:Url"] ?? "http://localhost:8080";
        var realm = _configuration["Keycloak:Realm"] ?? "Pollon";
        var adminUrl = $"{keycloakUrl.TrimEnd('/')}/admin/realms/{realm}/clients";

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Find internal ID
        var clients = await _httpClient.GetFromJsonAsync<List<KeycloakClientInfo>>(adminUrl);
        var client = clients?.FirstOrDefault(c => c.ClientId == clientId);
        if (client == null) return false;

        var response = await _httpClient.DeleteAsync($"{adminUrl}/{client.Id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<KeycloakClientCredentials?> RegeneratePluginSecretAsync(string clientId)
    {
        var token = await GetAdminTokenAsync();
        if (token == null) return null;

        var keycloakUrl = _configuration.GetConnectionString("keycloak") ?? _configuration["Keycloak:Url"] ?? "http://localhost:8080";
        var realm = _configuration["Keycloak:Realm"] ?? "Pollon";
        var adminUrl = $"{keycloakUrl.TrimEnd('/')}/admin/realms/{realm}/clients";

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Find internal ID
        var clients = await _httpClient.GetFromJsonAsync<List<KeycloakClientInfo>>(adminUrl);
        var client = clients?.FirstOrDefault(c => c.ClientId == clientId);
        if (client == null) return null;

        var secretUrl = $"{adminUrl}/{client.Id}/client-secret";
        var response = await _httpClient.PostAsync(secretUrl, null);
        if (!response.IsSuccessStatusCode) return null;

        var secretResult = await response.Content.ReadFromJsonAsync<SecretResponse>();
        return new KeycloakClientCredentials(clientId, secretResult?.Value ?? "");
    }

    private record TokenResponse([property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken);
    private record SecretResponse(string Value);
    private record KeycloakClientInfo(string Id, string ClientId);
}
