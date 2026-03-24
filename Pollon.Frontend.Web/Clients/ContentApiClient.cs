using System.Net.Http.Json;
using Pollon.Contracts.Models;

namespace Pollon.Frontend.Web.Clients;

public class ContentApiClient(HttpClient httpClient)
{
    public async Task<PagedResult<PublishedContent>?> GetLatestContentAsync(int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PagedResult<PublishedContent>>($"/api/content?page={page}&pageSize={pageSize}", ct);
    }

    public async Task<PublishedContent?> GetContentBySlugAsync(string slug, CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<PagedResult<PublishedContent>>($"/api/content/{slug}?pageSize=1", ct);
        return result?.Items.FirstOrDefault();
    }

    public async Task<PublishedContent?> GetContentByIdAsync(string id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<PublishedContent>($"/api/content/item/{id}", ct);
    }
}
