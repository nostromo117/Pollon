using System.Net.Http.Headers;
using System.Net.Http.Json;
using Pollon.Backoffice.Models;

namespace Pollon.Content.Api;

public class BackofficeApiClient(HttpClient httpClient, KeycloakTokenService tokenService)
{
    private static readonly System.Text.Json.JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private async Task PrepareClientAsync()
    {
        var token = await tokenService.GetTokenAsync();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<ContentType?> GetContentTypeByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await PrepareClientAsync();
        return await httpClient.GetFromJsonAsync<ContentType>($"/api/content-types/{id}", _options, cancellationToken);
    }

    public async Task<ContentItem?> GetContentItemByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await PrepareClientAsync();
        return await httpClient.GetFromJsonAsync<ContentItem>($"/api/content-items/{id}", _options, cancellationToken);
    }

    public async Task<MediaGallery?> GetGalleryByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await PrepareClientAsync();
        return await httpClient.GetFromJsonAsync<MediaGallery>($"/api/galleries/{id}", _options, cancellationToken);
    }
}
