using Microsoft.Extensions.Logging;
using System;

namespace Pollon.Content.Api.Consumers;

public partial class TemplateEventConsumers
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Received TemplatePublishedEvent for TemplateId {TemplateId} (FileName: {FileName})")]
    static partial void LogReceivedPublishedEvent(ILogger logger, string templateId, string fileName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping TemplatePublishedEvent for TemplateId {TemplateId} because incoming UpdatedAt ({IncomingUpdated}) is older than existing UpdatedAt ({ExistingUpdated})")]
    static partial void LogSkippingOutofOrder(ILogger logger, string templateId, DateTime incomingUpdated, DateTime existingUpdated);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updating existing template {TemplateId} (FileName: {FileName}) in read-db")]
    static partial void LogUpdatingExistingTemplate(ILogger logger, string templateId, string fileName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating new template {TemplateId} (FileName: {FileName}) in read-db")]
    static partial void LogCreatingNewTemplate(ILogger logger, string templateId, string fileName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully saved template {TemplateId} to read-db")]
    static partial void LogSavedSuccess(ILogger logger, string templateId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Received TemplateDeletedEvent for TemplateId {TemplateId}")]
    static partial void LogReceivedDeletedEvent(ILogger logger, string templateId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully deleted template {TemplateId} from read-db")]
    static partial void LogDeletedSuccess(ILogger logger, string templateId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Template {TemplateId} not found in read-db for deletion")]
    static partial void LogTemplateNotFoundForDeletion(ILogger logger, string templateId);
}
