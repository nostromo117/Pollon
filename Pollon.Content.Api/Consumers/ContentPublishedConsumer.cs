using System.Text.Json;
using Wolverine;
using Microsoft.EntityFrameworkCore;
using Pollon.Content.Api.Data;
using Pollon.Contracts.Models;
using Pollon.Backoffice.Models;
using Pollon.Contracts.Events;

namespace Pollon.Content.Api.Consumers;

public class ContentPublishedConsumer
{
    private readonly ApiDbContext _dbContext;
    private readonly BackofficeApiClient _apiClient;
    private readonly ILogger<ContentPublishedConsumer> _logger;

    public ContentPublishedConsumer(
        ApiDbContext dbContext,
        BackofficeApiClient apiClient,
        ILogger<ContentPublishedConsumer> logger)
    {
        _dbContext = dbContext;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task Handle(ContentPublishedEvent message)
    {
        var contentItemId = message.ContentItemId;
        _logger.LogInformation("Processing ContentPublishedEvent for ContentItemId {ContentItemId}", contentItemId);

        try
        {
            var contentItem = await _apiClient.GetContentItemByIdAsync(contentItemId);
            if (contentItem == null)
            {
                _logger.LogWarning("ContentItem {ContentItemId} not found in Backoffice API. Ignoring event.", contentItemId);
                return;
            }

            var contentType = await _apiClient.GetContentTypeByIdAsync(contentItem.ContentTypeId);
            if (contentType == null)
            {
                _logger.LogWarning("ContentType {ContentTypeId} not found in Backoffice API. Ignoring event.", contentItem.ContentTypeId);
                return;
            }

            var json = JsonSerializer.Serialize(contentItem.Data);
            _logger.LogDebug("Serialized data: {Json}", json);

            var existingContent = await _dbContext.PublishedContents.FirstOrDefaultAsync(c => c.Id == contentItemId);

            if (existingContent == null)
            {
                _logger.LogInformation("Creating new published content for {ContentItemId}", contentItemId);
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
                    JsonData = json
                };
                _dbContext.PublishedContents.Add(newContent);
            }
            else
            {
                _logger.LogInformation("Updating existing published content for {ContentItemId}", contentItemId);
                existingContent.ContentTypeId = contentItem.ContentTypeId;
                existingContent.SystemName = contentType.SystemName;
                existingContent.Slug = string.IsNullOrWhiteSpace(contentItem.Slug) 
                                       ? $"{contentType.Slug}/{contentItem.Id}" 
                                       : contentItem.Slug;
                existingContent.Icon = contentItem.Icon;
                existingContent.PublishedAt = contentItem.PublishedAt ?? DateTime.UtcNow;
                existingContent.JsonData = json;
                _dbContext.PublishedContents.Update(existingContent);
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Successfully processed ContentPublishedEvent for ContentItemId {ContentItemId}", contentItemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ContentPublishedEvent for ContentItemId {ContentItemId}", contentItemId);
            throw; // Re-throw to allow Wolverine to handle retries/failure
        }
    }
}
