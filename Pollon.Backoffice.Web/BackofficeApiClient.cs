using System.Net.Http.Headers;
using System.Net.Http.Json;
using Pollon.Backoffice.Models;

namespace Pollon.Backoffice.Web;

public class BackofficeApiClient(HttpClient httpClient, TokenProvider tokenProvider)
{
    private void PrepareClient()
    {
        if (!string.IsNullOrEmpty(tokenProvider.AccessToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);
        }
    }

    public async Task<ContentType[]> GetContentTypesAsync(CancellationToken cancellationToken = default)
    {
        PrepareClient();
        return await httpClient.GetFromJsonAsync<ContentType[]>("/api/content-types", cancellationToken) ?? [];
    }

    public async Task<ContentType?> GetContentTypeByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        return await httpClient.GetFromJsonAsync<ContentType>($"/api/content-types/{id}", cancellationToken);
    }

    public async Task CreateContentTypeAsync(ContentType item, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        await httpClient.PostAsJsonAsync("/api/content-types", item, cancellationToken);
    }

    public async Task UpdateContentTypeAsync(string id, ContentType item, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        await httpClient.PutAsJsonAsync($"/api/content-types/{id}", item, cancellationToken);
    }

    public async Task DeleteContentTypeAsync(string id, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        await httpClient.DeleteAsync($"/api/content-types/{id}", cancellationToken);
    }

    public async Task<ContentItem[]> GetContentItemsAsync(
        string? status = null, 
        string? sortBy = null, 
        bool sortDescending = true, 
        CancellationToken cancellationToken = default)
    {
        PrepareClient();
        var url = "/api/content-items?";
        if (!string.IsNullOrEmpty(status)) url += $"status={status}&";
        if (!string.IsNullOrEmpty(sortBy)) url += $"sortBy={sortBy}&";
        url += $"sortDescending={sortDescending.ToString().ToLower()}";

        return await httpClient.GetFromJsonAsync<ContentItem[]>(url, cancellationToken) ?? [];
    }

    public async Task<ContentItem?> GetContentItemByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        return await httpClient.GetFromJsonAsync<ContentItem>($"/api/content-items/{id}", cancellationToken);
    }

    public async Task CreateContentItemAsync(ContentItem item, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        await httpClient.PostAsJsonAsync("/api/content-items", item, cancellationToken);
    }

    public async Task UpdateContentItemAsync(string id, ContentItem item, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        await httpClient.PutAsJsonAsync($"/api/content-items/{id}", item, cancellationToken);
    }

    public async Task DeleteContentItemAsync(string id, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        await httpClient.DeleteAsync($"/api/content-items/{id}", cancellationToken);
    }

    public async Task<MediaAsset?> UploadMediaAsync(MultipartFormDataContent content, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        var response = await httpClient.PostAsync("/api/media", content, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<MediaAsset>(cancellationToken: cancellationToken);
        }
        return null;
    }

    public async Task<HttpResponseMessage> GetMediaResponseAsync(string id, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        return await httpClient.GetAsync($"/api/media/{id}", cancellationToken);
    }

    // Gallery Methods
    public async Task<MediaGallery[]> GetGalleriesAsync(bool includeUnpublished = false, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        return await httpClient.GetFromJsonAsync<MediaGallery[]>($"/api/galleries?includeUnpublished={includeUnpublished.ToString().ToLower()}", cancellationToken) ?? [];
    }

    public async Task<MediaGallery?> GetGalleryByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        return await httpClient.GetFromJsonAsync<MediaGallery>($"/api/galleries/{id}", cancellationToken);
    }

    public async Task CreateGalleryAsync(MediaGallery item, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        await httpClient.PostAsJsonAsync("/api/galleries", item, cancellationToken);
    }

    public async Task<MediaGallery?> CreateGalleryWithUploadAsync(MultipartFormDataContent content, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        // This is tricky: where does the upload go? Media.Api or Backoffice.Api?
        // Usually, the Media.Api handles the files. 
        // I configured /api/galleries POST in Media.Api to handle multi-upload.
        // If the Backoffice.Web is talking to the Gateway, I need to make sure the Gateway proxies this.
        var response = await httpClient.PostAsync("/api/galleries", content, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<MediaGallery>(cancellationToken: cancellationToken);
        }
        return null;
    }

    public async Task UpdateGalleryAsync(string id, MediaGallery item, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        await httpClient.PutAsJsonAsync($"/api/galleries/{id}", item, cancellationToken);
    }

    public async Task DeleteGalleryAsync(string id, CancellationToken cancellationToken = default)
    {
        PrepareClient();
        await httpClient.DeleteAsync($"/api/galleries/{id}", cancellationToken);
    }
}
