using System.Net.Http.Json;
using Pollon.Backoffice.Models;

namespace Pollon.Content.Api;

public class BackofficeApiClient(HttpClient httpClient)
{
    public async Task<ContentType?> GetContentTypeByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ContentType>($"/api/content-types/{id}", cancellationToken);
    }

    public async Task<ContentItem?> GetContentItemByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ContentItem>($"/api/content-items/{id}", cancellationToken);
    }
}
