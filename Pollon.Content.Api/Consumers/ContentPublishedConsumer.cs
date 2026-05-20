using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.EntityFrameworkCore;
using Pollon.Publication.Models;
using Pollon.Content.Api.Data;
using Pollon.Content.Api.Services;
using Pollon.Content.Api.Templates;
using Pollon.Contracts.Events;
using System.Text.Json;
using Pollon.Content.Api.Domain.Interfaces;
using Wolverine;

namespace Pollon.Content.Api.Consumers;

public partial class ContentPublishedConsumer
{
    private readonly IPublishedContentRepository _repository;
    private readonly IContentTemplateRepository _templateRepository;
    private readonly BackofficeApiClient _apiClient;
    private readonly ITemplateRenderer _renderer;
    private readonly IStaticStorage _staticStorage;
    private readonly ILogger<ContentPublishedConsumer> _logger;

    public ContentPublishedConsumer(
        IPublishedContentRepository repository,
        IContentTemplateRepository templateRepository,
        BackofficeApiClient apiClient,
        ITemplateRenderer renderer,
        IStaticStorage staticStorage,
        ILogger<ContentPublishedConsumer> logger)
    {
        _repository = repository;
        _templateRepository = templateRepository;
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

            var dictionaryData = contentItem.Data
                .Where(x => x.Value != null)
                .GroupBy(x => x.Name)
                .ToDictionary(g => g.Key, g => g.First().Value!);
            var json = JsonSerializer.Serialize(dictionaryData);
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
                        gallery = await _apiClient.GetGalleryByIdAsync(contentItem.GalleryId);

                    // Resolve the ContentTemplate record (for inline content and variables)
                    ContentTemplate? contentTemplate = null;
                    if (!string.IsNullOrEmpty(contentType.TemplateName))
                    {
                        // 1. First try to load from the local replicated repository
                        contentTemplate = await _templateRepository
                            .GetByFileNameAsync(contentType.TemplateName);

                        if (contentTemplate != null)
                        {
                            LogResolvedTemplateFromDb(_logger, contentType.TemplateName);
                        }
                        else
                        {
                            // 2. Fallback to API call in case of replica lag / race condition
                            LogTemplateDbFallback(_logger, contentType.TemplateName);
                            contentTemplate = await _apiClient.GetContentTemplateByFileNameAsync(contentType.TemplateName);
                        }
                    }

                    html = await RenderTemplate.RenderContent(contentItem, contentType, _renderer, gallery, contentTemplate);

                    if (!string.IsNullOrEmpty(html))
                    {
                        var fileName = $"{publishedSlug}.html";
                        await _staticStorage.SaveFileAsync(fileName, html, "text/html");
                        LogStaticFileSaved(_logger, fileName);
                    }
                }
                catch (Exception ex)
                {
                    LogRenderFailed(_logger, ex, contentItemId);
                }
            }

            var existingContent = await _repository.GetByIdAsync(contentItemId);

            var publishedContent = new PublishedContent
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
                IsInteractive = contentType.IsInteractive,
                SchemaJson = JsonSerializer.Serialize(contentType.Fields),
                SearchText = contentItem.SearchText
            };

            LogContentAction(_logger, existingContent == null ? "Creating new" : "Updating existing", contentItemId);
            await _repository.AddOrUpdateAsync(publishedContent);

            LogProcessingSuccess(_logger, contentItemId);
        }
        catch (Exception ex)
        {
            LogProcessingError(_logger, ex, contentItemId);
            throw; // Re-throw to allow Wolverine to handle retries/failure
        }
    }

 

 
}
