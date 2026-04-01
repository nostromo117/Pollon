using Wolverine;
using Marten;
using Pollon.Backoffice.Models;
using Pollon.Backoffice.Repositories;
using Pollon.Backoffice.Services;
using Pollon.Contracts.Events;

namespace Pollon.Backoffice.Api.Services;

public class ContentItemService : IContentItemService
{
    private readonly IRepository<ContentItem> _repository;
    private readonly IMessageBus _messageBus;
    private readonly IRepository<ContentType> _contentTypeRepository;
    private readonly IDocumentSession _session;

    public ContentItemService(
        IRepository<ContentItem> repository,
        IMessageBus messageBus,
        IRepository<ContentType> contentTypeRepository,
        IDocumentSession session)
    {
        _repository = repository;
        _messageBus = messageBus;
        _contentTypeRepository = contentTypeRepository;
        _session = session;
    }

    public async Task<IEnumerable<ContentItem>> GetAllAsync(string? status = null, string? sortBy = null, bool sortDescending = true)
    {
        IQueryable<ContentItem> query = _session.Query<ContentItem>();

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
        var batch = _session.CreateBatchQuery();
        var itemTask = batch.Load<ContentItem>(id);
        var childrenTask = batch.Query<ContentItem>().Where(x => x.ParentId == id).ToList();

        await batch.Execute();

        var item = await itemTask;
        if (item != null)
        {
            var children = await childrenTask;
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

        // Auto-generate Slug if missing
        if (string.IsNullOrWhiteSpace(item.Slug))
        {
            item.Slug = GenerateSlugFromData(item);
        }
        
        // Save to Marten
        await _repository.CreateAsync(item);

        if (item.Status == "Published")
        {
            // Publish Event to RabbitMQ (only ID)
            var publishedEvent = new ContentPublishedEvent(item.Id);
            await _messageBus.PublishAsync(publishedEvent);
        }

        await _session.SaveChangesAsync();
        return item;
    }

    private string GenerateSlugFromData(ContentItem item)
    {
        string? title = null;
        if (item.Data.TryGetValue("title", out var t) && t != null) title = t.ToString();
        else if (item.Data.TryGetValue("name", out var n) && n != null) title = n.ToString();

        if (string.IsNullOrWhiteSpace(title)) return item.Id;

        var str = title.ToLowerInvariant();
        str = System.Text.RegularExpressions.Regex.Replace(str, @"\s+", "-");
        str = System.Text.RegularExpressions.Regex.Replace(str, @"[^a-z0-9\-]", "");
        return str.Trim('-');
    }

    public async Task<ContentItem?> UpdateAndPublishAsync(string id, ContentItem item)
    {
        // Non salvare i figli annidati
        item.Children = new();

        var existingItem = await _repository.GetByIdAsync(id);
        if (existingItem == null)
            return null;

        // Validation: Verify ContentType exists if changed
        if (item.ContentTypeId != existingItem.ContentTypeId)
        {
            var contentType = await _contentTypeRepository.GetByIdAsync(item.ContentTypeId);
            if (contentType == null)
            {
                throw new ArgumentException($"ContentType with ID {item.ContentTypeId} not found.");
            }
        }

        item.Id = id;
        item.UpdatedAt = DateTime.UtcNow;

        // Auto-generate Slug if missing
        if (string.IsNullOrWhiteSpace(item.Slug))
        {
            item.Slug = GenerateSlugFromData(item);
        }

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
        
        await _repository.UpdateAsync(id, item);

        if (item.Status == "Published")
        {
            var updatedEvent = new ContentUpdatedEvent(item.Id);
            await _messageBus.PublishAsync(updatedEvent);
        }
        else if (existingItem.Status == "Published" && item.Status != "Published")
        {
            // Se prima era pubblicato e ora non lo è più (es. "Archived" o "Draft"), 
            // lo trattiamo come un'eliminazione per la read model, così viene rimosso dal front-end.
            var deletedEvent = new ContentDeletedEvent(item.Id);
            await _messageBus.PublishAsync(deletedEvent);
        }

        await _session.SaveChangesAsync();
        return item;
    }

    public async Task DeleteAndPublishAsync(string id)
    {
        await DeleteRecursiveAsync(id);
        await _session.SaveChangesAsync();
    }

    private async Task DeleteRecursiveAsync(string id)
    {
        // Find all children IDs first to recurse
        var childrenIds = await _session.Query<ContentItem>()
            .Where(x => x.ParentId == id)
            .Select(x => x.Id)
            .ToListAsync();

        foreach (var childId in childrenIds)
        {
            await DeleteRecursiveAsync(childId);
        }

        // Delete the item itself from the session
        _session.Delete<ContentItem>(id);

        // Publish the deletion event for this specific ID
        var deletedEvent = new ContentDeletedEvent(id);
        await _messageBus.PublishAsync(deletedEvent);
    }
}
