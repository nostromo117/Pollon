using System.Text.Json;
using Wolverine;
using Microsoft.EntityFrameworkCore;
using Pollon.Content.Api.Data;
using Pollon.Contracts.Models;
using Pollon.Backoffice.Models;
using Pollon.Contracts.Events;
using Pollon.Content.Api.Services;

namespace Pollon.Content.Api.Consumers;

public partial class ContentPublishedConsumer
{
    private readonly ApiDbContext _dbContext;
    private readonly BackofficeApiClient _apiClient;
    private readonly ITemplateRenderer _renderer;
    private readonly IStaticStorage _staticStorage;
    private readonly ILogger<ContentPublishedConsumer> _logger;

    public ContentPublishedConsumer(
        ApiDbContext dbContext,
        BackofficeApiClient apiClient,
        ITemplateRenderer renderer,
        IStaticStorage staticStorage,
        ILogger<ContentPublishedConsumer> logger)
    {
        _dbContext = dbContext;
        _apiClient = apiClient;
        _renderer = renderer;
        _staticStorage = staticStorage;
        _logger = logger;
    }

    public async Task Handle(ContentPublishedEvent message)
    {
        LogProcessingEvent(_logger, message.ContentItemId);
        await ProcessContentInternalAsync(message.ContentItemId);
    }

    public async Task Handle(ContentUpdatedEvent message)
    {
        LogProcessingEvent(_logger, message.ContentItemId);
        await ProcessContentInternalAsync(message.ContentItemId);
    }

    private async Task ProcessContentInternalAsync(string contentItemId)
    {
        try
        {
            var contentItem = await _apiClient.GetContentItemByIdAsync(contentItemId);
            if (contentItem == null)
            {
                LogItemNotFound(_logger, contentItemId);
                return;
            }

            // Only process items that are actually Published
            if (contentItem.Status != "Published")
            {
                LogSkippingNonPublished(_logger, contentItemId, contentItem.Status);
                return;
            }

            LogRetrievedContentItem(_logger, contentItem.Slug, contentItem.ContentTypeId);
            LogPublishModeOverride(_logger, contentItem.PublishModeOverride?.ToString() ?? "NULL");

            var contentType = await _apiClient.GetContentTypeByIdAsync(contentItem.ContentTypeId);
            if (contentType == null)
            {
                LogContentTypeNotFound(_logger, contentItem.ContentTypeId, contentItemId);
                return;
            }

            LogRetrievedContentType(_logger, contentType.SystemName, contentType.PublishMode);

            var json = JsonSerializer.Serialize(contentItem.Data);
            LogSerializedData(_logger, json);

            // Determine publication mode (Item override takes precedence over ContentType default)
            var effectiveMode = contentItem.PublishModeOverride ?? contentType.PublishMode;
            LogEffectivePublishMode(_logger, effectiveMode);

            string? html = null;
            if (effectiveMode == PublishMode.Static || effectiveMode == PublishMode.Both)
            {
                LogTriggeringRender(_logger, contentItemId, contentType.TemplateName);
                try {
                    // Enrich data for template
                    var templateData = new Dictionary<string, object>(contentItem.Data);
                    
                    // Metadata
                    templateData["id"] = contentItem.Id;
                    templateData["slug"] = contentItem.Slug;
                    templateData["published_at"] = contentItem.PublishedAt ?? DateTime.UtcNow;
                    templateData["content_type"] = contentType.DisplayName;
                    
                    // Helper for Title if not already in Data
                    if (!templateData.ContainsKey("title") && !templateData.ContainsKey("Title"))
                    {
                        templateData["title"] = GetItemDisplayName(contentItem);
                    }

                    // Gallery
                    if (!string.IsNullOrEmpty(contentItem.GalleryId))
                    {
                        var gallery = await _apiClient.GetGalleryByIdAsync(contentItem.GalleryId);
                        if (gallery != null && gallery.AssetIds.Any())
                        {
                            templateData["images"] = gallery.AssetIds.Select(id => new { url = $"/api/media/{id}", alt = "Gallery Image" }).ToList();
                        }
                    }

                    html = await _renderer.RenderAsync(contentType.TemplateName ?? "default", templateData);

                    // Push to Static Storage (MinIO)
                    if (!string.IsNullOrEmpty(html))
                    {
                        var fileName = string.IsNullOrWhiteSpace(contentItem.Slug) 
                                       ? $"{contentItem.Id}.html" 
                                       : $"{contentItem.Slug}.html";
                                       
                        await _staticStorage.SaveFileAsync(fileName, html, "text/html");
                        LogStaticFileSaved(_logger, fileName);
                    }
                } catch (Exception ex) {
                    LogRenderFailed(_logger, ex, contentItemId);
                }
            }

            var existingContent = await _dbContext.PublishedContents.FirstOrDefaultAsync(c => c.Id == contentItemId);

            if (existingContent == null)
            {
                LogContentAction(_logger, "Creating new", contentItemId);
                var newContent = new PublishedContent
                {
                    Id = contentItem.Id,
                    ContentTypeId = contentItem.ContentTypeId,
                    SystemName = contentType.SystemName,
                    Slug = string.IsNullOrWhiteSpace(contentItem.Slug) 
                           ? $"{contentType.Slug}/{contentItem.Id}" 
                           : contentItem.Slug,
                    Icon = contentItem.Icon,
                    PublishedAt = contentItem.PublishedAt ?? DateTime.UtcNow,
                    JsonData = json,
                    HtmlContent = html,
                    PublishMode = effectiveMode.ToString(),
                    SearchText = contentItem.SearchText
                };
                _dbContext.PublishedContents.Add(newContent);
            }
            else
            {
                LogContentAction(_logger, "Updating existing", contentItemId);
                existingContent.ContentTypeId = contentItem.ContentTypeId;
                existingContent.SystemName = contentType.SystemName;
                existingContent.Slug = string.IsNullOrWhiteSpace(contentItem.Slug) 
                                       ? $"{contentType.Slug}/{contentItem.Id}" 
                                       : contentItem.Slug;
                existingContent.Icon = contentItem.Icon;
                existingContent.PublishedAt = contentItem.PublishedAt ?? DateTime.UtcNow;
                existingContent.JsonData = json;
                existingContent.HtmlContent = html;
                existingContent.PublishMode = effectiveMode.ToString();
                existingContent.SearchText = contentItem.SearchText;
                _dbContext.PublishedContents.Update(existingContent);
            }

            await _dbContext.SaveChangesAsync();
            LogProcessingSuccess(_logger, contentItemId);
        }
        catch (Exception ex)
        {
            LogProcessingError(_logger, ex, contentItemId);
            throw; // Re-throw to allow Wolverine to handle retries/failure
        }
    }

    private string GetItemDisplayName(ContentItem ci)
    {
        if (ci.Data.TryGetValue("Title", out var t) || ci.Data.TryGetValue("title", out t) || 
            ci.Data.TryGetValue("Name", out t) || ci.Data.TryGetValue("name", out t))
        {
            if (t is JsonElement el && el.ValueKind == JsonValueKind.String)
                return el.GetString() ?? ci.Id;
            return t.ToString() ?? ci.Id;
        }
        return ci.Id;
    }
}
