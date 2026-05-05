using Wolverine;
using Marten;
using Marten.Linq;
using Pollon.Publication.Models;
using Pollon.Backoffice.Repositories;
using Pollon.Backoffice.Services;
using Pollon.Contracts.Events;
using Pollon.Contracts.Messages;

namespace Pollon.Backoffice.Services;

public class ContentItemService : IContentItemService
{
    private readonly IRepository<ContentItem> _repository;
    private readonly IMessageBus _messageBus;
    private readonly IRepository<ContentType> _contentTypeRepository;

    public ContentItemService(
        IRepository<ContentItem> repository,
        IMessageBus bus,
        IRepository<ContentType> contentTypeRepository)
    {
        _repository = repository;
        _messageBus = bus;
        _contentTypeRepository = contentTypeRepository;
    }

    public async Task<IEnumerable<ContentItem>> GetAllAsync(string? status = null, string? sortBy = null, bool sortDescending = true, string? searchTerm = null)
    {
        var query = _repository.Query();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.SearchText.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrEmpty(sortBy))
        {
            // Simple sorting logic based on property name
            query = sortBy.ToLower() switch
            {
                "createdat" => sortDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
                "publishedat" => sortDescending ? query.OrderByDescending(x => x.PublishedAt) : query.OrderBy(x => x.PublishedAt),
                "status" => sortDescending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
                _ => query
            };
        }
        else
        {
            // Default sort
            query = query.OrderByDescending(x => x.CreatedAt);
        }

        var list = await query.ToListAsync();

        // Populate Children dynamically without persisting them
        var lookup = list.ToLookup(x => x.ParentId);
        foreach (var item in list)
        {
            item.Children = lookup[item.Id].ToList();
        }

        return list;
    }

    public async Task<ContentItem?> GetByIdAsync(string id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item != null)
        {
            var children = await _repository.Query().Where(x => x.ParentId == id).ToListAsync();
            item.Children = children.ToList();
        }
        return item;
    }

    public async Task<ContentItem> CreateAndPublishAsync(ContentItem item)
    {
        // Non salvare i figli annidati
        item.Children = new();
        // Validation: Verify ContentType exists
        var contentType = await _contentTypeRepository.GetByIdAsync(item.ContentTypeId);
        if (contentType == null)
        {
            throw new ArgumentException($"ContentType with ID {item.ContentTypeId} not found.");
        }

        item.CreatedAt = DateTime.UtcNow;
        if (item.Status == "Published" && !item.PublishedAt.HasValue)
        {
            item.PublishedAt = DateTime.UtcNow;
        }

        var titleField = contentType.Fields.FirstOrDefault(f => f.UseAsTitle);
        item.UseAsTitle = titleField != null ? titleField.Name : contentType.SystemName;

        // Auto-generate Slug if missing
        if (string.IsNullOrWhiteSpace(item.Slug))
        {
            item.Slug = GenerateSlugFromData(item);
        }

        PopulateSearchText(item);
        
        var isPublished = item.Status == "Published";
        if (isPublished)
        {
            item.Status = "Draft";
        }

        // Save to Marten via repository
        await _repository.CreateAsync(item);

        if (isPublished)
        {
            // Start Saga instead of direct event
            await _messageBus.PublishAsync(new StartContentPublication(item.Id, contentType.SystemName));
        }

        return item;
    }

    private string GenerateSlugFromData(ContentItem item)
    {
        var title = item.GetTitle();

        if (string.IsNullOrWhiteSpace(title) || title == item.Id) return item.Id;

        var str = title.ToLowerInvariant();
        str = System.Text.RegularExpressions.Regex.Replace(str, @"\s+", "-");
        str = System.Text.RegularExpressions.Regex.Replace(str, @"[^a-z0-9\-]", "");
        return str.Trim('-');
    }

    private void PopulateSearchText(ContentItem item)
    {
        if (item.Data != null && item.Data.Count > 0)
        {
            var values = item.Data.Values
                .Where(v => v != null)
                .Select(v => v!.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s));
            item.SearchText = string.Join(" ", values);
        }
        else
        {
            item.SearchText = string.Empty;
        }
    }

    public async Task<ContentItem?> UpdateAndPublishAsync(string id, ContentItem item)
    {
        // Non salvare i figli annidati
        item.Children = new();

        var existingItem = await _repository.GetByIdAsync(id);
        if (existingItem == null)
            return null;

        var contentType = await _contentTypeRepository.GetByIdAsync(item.ContentTypeId);
        if (contentType == null)
        {
            throw new ArgumentException($"ContentType with ID {item.ContentTypeId} not found.");
        }

        var titleField = contentType.Fields.FirstOrDefault(f => f.UseAsTitle);
        item.UseAsTitle = titleField != null ? titleField.Name : contentType.SystemName;

        item.Id = id;
        item.UpdatedAt = DateTime.UtcNow;

        // Auto-generate Slug if missing
        if (string.IsNullOrWhiteSpace(item.Slug))
        {
            item.Slug = GenerateSlugFromData(item);
        }

        PopulateSearchText(item);

        // Se passa da Draft a Published, impostare la data
        if (item.Status == "Published" && !existingItem.PublishedAt.HasValue)
        {
            item.PublishedAt = DateTime.UtcNow;
        }
        else
        {
            // Mantieni la data di pubblicazione originale se già esistente
            item.PublishedAt = existingItem.PublishedAt;
        }

        item.CreatedAt = existingItem.CreatedAt;
        
        var shouldStartSaga = false;
        var originalStatus = item.Status;

        if (item.Status == "Published")
        {
            // Se prima era Draft/Archived e ora vogliamo pubblicare, impostiamo lo stato a quello precedente
            // e avviamo la Saga. Il PublicationCompletedHandler lo metterà a "Published".
            if (existingItem.Status != "Published")
            {
                item.Status = existingItem.Status; 
            }
            shouldStartSaga = true;
        }

        await _repository.UpdateAsync(id, item);

        if (shouldStartSaga)
        {
            await _messageBus.PublishAsync(new StartContentPublication(item.Id, contentType!.SystemName));
        }
        else if (existingItem.Status == "Published" && originalStatus != "Published")
        {
            // Se prima era pubblicato e ora non lo è più (es. "Archived" o "Draft"), 
            // lo trattiamo come un'eliminazione per la read model, così viene rimosso dal front-end.
            var deletedEvent = new ContentDeletedEvent(item.Id);
            await _messageBus.PublishAsync(deletedEvent);
        }

        return item;
    }

    public async Task DeleteAndPublishAsync(string id)
    {
        await DeleteRecursiveAsync(id);
    }

    private async Task DeleteRecursiveAsync(string id)
    {
        // Find all children IDs first to recurse
        var childrenIds = await _repository.Query()
            .Where(x => x.ParentId == id)
            .Select(x => x.Id)
            .ToListAsync();

        foreach (var childId in childrenIds)
        {
            await DeleteRecursiveAsync(childId);
        }

        // Delete the item itself via repository
        await _repository.DeleteAsync(id);

        // Publish the deletion event for this specific ID
        var deletedEvent = new ContentDeletedEvent(id);
        await _messageBus.PublishAsync(deletedEvent);
    }

    public async Task RepublishAllAsync()
    {
        var publishedItems = await _repository.Query()
            .Where(x => x.Status == "Published")
            .ToListAsync();

        foreach (var item in publishedItems)
        {
            var publishedEvent = new ContentPublishedEvent(item.Id);
            await _messageBus.PublishAsync(publishedEvent);
        }
    }
}
