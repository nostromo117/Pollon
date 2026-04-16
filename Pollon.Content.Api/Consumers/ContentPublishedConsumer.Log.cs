using Microsoft.Extensions.Logging;
using Pollon.Publication.Models;

namespace Pollon.Content.Api.Consumers;

public partial class ContentPublishedConsumer
{
    #region Logger Messages

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing event for ContentItemId {ContentItemId}")]
    static partial void LogProcessingEvent(ILogger logger, string contentItemId);

    [LoggerMessage(Level = LogLevel.Error, Message = "[DEBUG-PUBS] Content Item {ContentItemId} not found in Backoffice API!")]
    static partial void LogItemNotFound(ILogger logger, string contentItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[DEBUG-PUBS] Item {ContentItemId} is in state {Status}. Skipping publication processing.")]
    static partial void LogSkippingNonPublished(ILogger logger, string contentItemId, string status);

    [LoggerMessage(Level = LogLevel.Information, Message = "[DEBUG-PUBS] Retrieved ContentItem: {Slug} (ContentTypeId: {ContentTypeId})")]
    static partial void LogRetrievedContentItem(ILogger logger, string slug, string contentTypeId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[DEBUG-PUBS] PublishModeOverride: {Override}")]
    static partial void LogPublishModeOverride(ILogger logger, string? @override);

    [LoggerMessage(Level = LogLevel.Error, Message = "[DEBUG-PUBS] Content Type {ContentTypeId} not found for Item {ContentItemId}!")]
    static partial void LogContentTypeNotFound(ILogger logger, string contentTypeId, string contentItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[DEBUG-PUBS] Retrieved ContentType: {SystemName} (Default PublishMode: {DefaultMode})")]
    static partial void LogRetrievedContentType(ILogger logger, string systemName, PublishMode defaultMode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Serialized data: {Json}")]
    static partial void LogSerializedData(ILogger logger, string json);

    [LoggerMessage(Level = LogLevel.Information, Message = "[DEBUG-PUBS] Effective PublishMode: {EffectiveMode}")]
    static partial void LogEffectivePublishMode(ILogger logger, PublishMode effectiveMode);

    [LoggerMessage(Level = LogLevel.Information, Message = "[DEBUG-PUBS] Triggering Render for {ContentItemId} using template {TemplateName}")]
    static partial void LogTriggeringRender(ILogger logger, string contentItemId, string? templateName);

    [LoggerMessage(Level = LogLevel.Information, Message = "[DEBUG-PUBS] Static file saved successfully: {FileName}")]
    static partial void LogStaticFileSaved(ILogger logger, string fileName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Template rendering or storage failed for {ContentItemId}. Static output will be empty.")]
    static partial void LogRenderFailed(ILogger logger, Exception ex, string contentItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Action} published content for {ContentItemId}")]
    static partial void LogContentAction(ILogger logger, string action, string contentItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully processed content {ContentItemId}")]
    static partial void LogProcessingSuccess(ILogger logger, string contentItemId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing content {ContentItemId}")]
    static partial void LogProcessingError(ILogger logger, Exception ex, string contentItemId);

    #endregion
}
