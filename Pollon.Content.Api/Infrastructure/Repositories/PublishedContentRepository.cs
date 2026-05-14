using Microsoft.EntityFrameworkCore;
using Pollon.Content.Api.Data;
using Pollon.Content.Api.Domain.Interfaces;
using Pollon.Publication.Models;

namespace Pollon.Content.Api.Infrastructure.Repositories;

public class PublishedContentRepository(ApiDbContext dbContext) : IPublishedContentRepository
{
    public async Task<PublishedContent?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await dbContext.PublishedContents.FindAsync([id], ct);
    }

    public async Task<(IEnumerable<PublishedContent> Items, int TotalCount)> GetPaginatedAsync(ContentQueryParameters query, CancellationToken ct = default)
    {
        var queryable = dbContext.PublishedContents.AsQueryable();
        return await GetResultsAsync(queryable, query, ct);
    }

    public async Task<(IEnumerable<PublishedContent> Items, int TotalCount)> GetBySlugPaginatedAsync(string slug, ContentQueryParameters query, CancellationToken ct = default)
    {
        var queryable = dbContext.PublishedContents.Where(c => c.Slug == slug);
        return await GetResultsAsync(queryable, query, ct);
    }

    public async Task AddOrUpdateAsync(PublishedContent content, CancellationToken ct = default)
    {
        var existing = await dbContext.PublishedContents.FirstOrDefaultAsync(c => c.Id == content.Id, ct);
        if (existing == null)
        {
            dbContext.PublishedContents.Add(content);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(content);
            dbContext.PublishedContents.Update(existing);
        }
        await dbContext.SaveChangesAsync(ct);
    }

    private static async Task<(IEnumerable<PublishedContent> Items, int TotalCount)> GetResultsAsync(IQueryable<PublishedContent> queryable, ContentQueryParameters query, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            queryable = queryable.Where(c => c.SearchText.Contains(query.SearchTerm));
        }

        queryable = query.SortBy?.Equals("PublishedAt", StringComparison.OrdinalIgnoreCase) == true
            ? (query.SortDescending ? queryable.OrderByDescending(c => c.PublishedAt) : queryable.OrderBy(c => c.PublishedAt))
            : queryable.OrderBy(c => c.Id);

        var totalCount = await queryable.CountAsync(ct);
        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
