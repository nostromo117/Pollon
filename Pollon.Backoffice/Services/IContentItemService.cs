using Pollon.Backoffice.Models;

namespace Pollon.Backoffice.Services;

public interface IContentItemService
{
    Task<ContentItem> CreateAndPublishAsync(ContentItem item);
    Task<ContentItem?> UpdateAndPublishAsync(string id, ContentItem item);
    Task DeleteAndPublishAsync(string id);
    Task<ContentItem?> GetByIdAsync(string id);
    Task<IEnumerable<ContentItem>> GetAllAsync(string? status = null, string? sortBy = null, bool sortDescending = true, string? searchTerm = null);
}
