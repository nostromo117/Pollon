using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.EntityFrameworkCore;
using Pollon.Publication.Models;
using Pollon.Content.Api.Data;
using Pollon.Content.Api.Services;
using Pollon.Content.Api.Templates;
using Pollon.Contracts.Events;
using Pollon.Publication.Models;
using System.Text.Json;
using Wolverine;

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

            // Calculate unified Slug combining ContentType and ContentItem rules
            var baseSlug = contentType.Slug?.Trim('/') ?? string.Empty;
            var itemSlugPart = string.IsNullOrWhiteSpace(contentItem.Slug) ? contentItem.Id : contentItem.Slug;
            var publishedSlug = string.IsNullOrEmpty(baseSlug) 
                ? itemSlugPart 
                : (itemSlugPart.StartsWith($"{baseSlug}/") ? itemSlugPart : $"{baseSlug}/{itemSlugPart}");

            var html = string.Empty;
            if (effectiveMode == PublishMode.Static || effectiveMode == PublishMode.Both)
            {
                LogTriggeringRender(_logger, contentItemId, contentType.TemplateName);
                try
            {
               MediaGallery? gallery = null;
               if (!string.IsNullOrEmpty(contentItem.GalleryId))
               {
                 gallery = await _apiClient.GetGalleryByIdAsync(contentItem.GalleryId);
               }
                  // Enrich data for template
                  html = await RenderTemplate.RenderContent(contentItem, contentType, _renderer, gallery);

               // Push to Static Storage (MinIO)
               if (!string.IsNullOrEmpty(html))
               {
                  var fileName = $"{publishedSlug}.html";

                  await _staticStorage.SaveFileAsync(fileName, html, "text/html");
                  LogStaticFileSaved(_logger, fileName);
               }
            }
            catch (Exception ex) {
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
                    Slug = publishedSlug,
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
                existingContent.Slug = publishedSlug;
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

 

 
}
