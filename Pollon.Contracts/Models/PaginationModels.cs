using System.Text.Json.Serialization;

namespace Pollon.Contracts.Models;

public record ContentQueryParameters(
    int Page = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? SortBy = "PublishedAt",
    bool SortDescending = true
);

public record PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }

    [JsonConstructor]
    public PagedResult(IEnumerable<T> items, int totalCount, int page, int pageSize, int totalPages)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = totalPages;
    }

    public PagedResult(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}
