using System.Text.Json;
using Wolverine;
using Microsoft.EntityFrameworkCore;
using Pollon.Content.Api.Data;
using Pollon.Contracts.Models;
using Pollon.Backoffice.Models;
using Pollon.Contracts.Events;

namespace Pollon.Content.Api.Consumers;

public partial class ContentUpdatedConsumer
{
    private readonly ApiDbContext _dbContext;
    private readonly BackofficeApiClient _apiClient;
    private readonly ILogger<ContentUpdatedConsumer> _logger;

    public ContentUpdatedConsumer(
        ApiDbContext dbContext,
        BackofficeApiClient apiClient,
        ILogger<ContentUpdatedConsumer> logger)
    {
        _dbContext = dbContext;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task Handle(ContentUpdatedEvent message)
    {
        var contentItemId = message.ContentItemId;
        LogProcessingEvent(_logger, contentItemId);

        try
        {
            var contentItem = await _apiClient.GetContentItemByIdAsync(contentItemId);
            if (contentItem == null)
            {
                LogItemNotFound(_logger, contentItemId);
                return;
            }

            var contentType = await _apiClient.GetContentTypeByIdAsync(contentItem.ContentTypeId);
            if (contentType == null)
            {
                LogContentTypeNotFound(_logger, contentItem.ContentTypeId);
                return;
            }

            var json = JsonSerializer.Serialize(contentItem.Data);
            LogSerializedData(_logger, json);

            var existingContent = await _dbContext.PublishedContents.FirstOrDefaultAsync(c => c.Id == contentItemId);

            if (existingContent == null)
            {
                LogCreatingNewItem(_logger, contentItemId);
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
                LogUpdatingExistingItem(_logger, contentItemId);
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
            LogProcessingSuccess(_logger, contentItemId);
        }
        catch (Exception ex)
        {
            LogProcessingError(_logger, ex, contentItemId);
            throw;
        }
    }
}
