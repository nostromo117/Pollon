using Microsoft.Extensions.Logging;

namespace Pollon.Content.Api.Consumers;

public partial class ContentUpdatedConsumer
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Processing ContentUpdatedEvent for ContentItemId {ContentItemId}")]
    static partial void LogProcessingEvent(ILogger logger, string contentItemId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "ContentItem {ContentItemId} not found in Backoffice API. Ignoring event.")]
    static partial void LogItemNotFound(ILogger logger, string contentItemId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "ContentType {ContentTypeId} not found in Backoffice API. Ignoring event.")]
    static partial void LogContentTypeNotFound(ILogger logger, string contentTypeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Serialized data: {Json}")]
    static partial void LogSerializedData(ILogger logger, string json);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating new published content for {ContentItemId} (from update event)")]
    static partial void LogCreatingNewItem(ILogger logger, string contentItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updating existing published content for {ContentItemId}")]
    static partial void LogUpdatingExistingItem(ILogger logger, string contentItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully processed ContentUpdatedEvent for ContentItemId {ContentItemId}")]
    static partial void LogProcessingSuccess(ILogger logger, string contentItemId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing ContentUpdatedEvent for ContentItemId {ContentItemId}")]
    static partial void LogProcessingError(ILogger logger, Exception ex, string contentItemId);
}
