using System.Net.Http.Headers;
using System.Net.Http.Json;
using Pollon.Backoffice.Models;

namespace Pollon.Content.Api;

public class BackofficeApiClient(HttpClient httpClient, KeycloakTokenService tokenService)
{
    private async Task PrepareClientAsync()
    {
        var token = await tokenService.GetTokenAsync();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<ContentType?> GetContentTypeByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await PrepareClientAsync();
        return await httpClient.GetFromJsonAsync<ContentType>($"/api/content-types/{id}", cancellationToken);
    }

    public async Task<ContentItem?> GetContentItemByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await PrepareClientAsync();
        return await httpClient.GetFromJsonAsync<ContentItem>($"/api/content-items/{id}", cancellationToken);
    }
}
