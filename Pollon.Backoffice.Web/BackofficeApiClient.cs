using System.Net.Http.Json;
using Pollon.Backoffice.Models;

namespace Pollon.Backoffice.Web;

public class BackofficeApiClient(HttpClient httpClient)
{
    public async Task<ContentType[]> GetContentTypesAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ContentType[]>("/api/content-types", cancellationToken) ?? [];
    }

    public async Task<ContentType?> GetContentTypeByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ContentType>($"/api/content-types/{id}", cancellationToken);
    }

    public async Task CreateContentTypeAsync(ContentType item, CancellationToken cancellationToken = default)
    {
        await httpClient.PostAsJsonAsync("/api/content-types", item, cancellationToken);
    }

    public async Task UpdateContentTypeAsync(string id, ContentType item, CancellationToken cancellationToken = default)
    {
        await httpClient.PutAsJsonAsync($"/api/content-types/{id}", item, cancellationToken);
    }

    public async Task DeleteContentTypeAsync(string id, CancellationToken cancellationToken = default)
    {
        await httpClient.DeleteAsync($"/api/content-types/{id}", cancellationToken);
    }

    public async Task<ContentItem[]> GetContentItemsAsync(
        string? status = null, 
        string? sortBy = null, 
        bool sortDescending = true, 
        CancellationToken cancellationToken = default)
    {
        var url = "/api/content-items?";
        if (!string.IsNullOrEmpty(status)) url += $"status={status}&";
        if (!string.IsNullOrEmpty(sortBy)) url += $"sortBy={sortBy}&";
        url += $"sortDescending={sortDescending.ToString().ToLower()}";

        return await httpClient.GetFromJsonAsync<ContentItem[]>(url, cancellationToken) ?? [];
    }

    public async Task<ContentItem?> GetContentItemByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ContentItem>($"/api/content-items/{id}", cancellationToken);
    }

    public async Task CreateContentItemAsync(ContentItem item, CancellationToken cancellationToken = default)
    {
        await httpClient.PostAsJsonAsync("/api/content-items", item, cancellationToken);
    }

    public async Task UpdateContentItemAsync(string id, ContentItem item, CancellationToken cancellationToken = default)
    {
        await httpClient.PutAsJsonAsync($"/api/content-items/{id}", item, cancellationToken);
    }

    public async Task DeleteContentItemAsync(string id, CancellationToken cancellationToken = default)
    {
        await httpClient.DeleteAsync($"/api/content-items/{id}", cancellationToken);
    }

    public async Task<MediaAsset?> UploadMediaAsync(MultipartFormDataContent content, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync("/api/media", content, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<MediaAsset>(cancellationToken: cancellationToken);
        }
        return null;
    }

    public async Task<HttpResponseMessage> GetMediaResponseAsync(string id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetAsync($"/api/media/{id}", cancellationToken);
    }
}
