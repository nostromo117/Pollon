using Pollon.Publication.Models;

namespace Pollon.Content.Api.Domain.Interfaces;

public interface IPublishedContentRepository
{
    Task<PublishedContent?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<(IEnumerable<PublishedContent> Items, int TotalCount)> GetPaginatedAsync(ContentQueryParameters query, CancellationToken ct = default);
    Task<(IEnumerable<PublishedContent> Items, int TotalCount)> GetBySlugPaginatedAsync(string slug, ContentQueryParameters query, CancellationToken ct = default);
    Task AddOrUpdateAsync(PublishedContent content, CancellationToken ct = default);
}
