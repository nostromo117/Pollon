using Microsoft.Extensions.Logging;

namespace Pollon.Content.Api.Consumers;

public partial class ContentDeletedConsumer
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Received ContentDeletedEvent for ContentItemId {ContentItemId}")]
    static partial void LogReceivedEvent(ILogger logger, string contentItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted ContentItemId {ContentItemId} from SQL Server")]
    static partial void LogDeletedSuccess(ILogger logger, string contentItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted static file {FileName} from MinIO")]
    static partial void LogDeletedStaticFile(ILogger logger, string fileName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No static file found to delete for {FileName} in MinIO")]
    static partial void LogStaticFileNotFound(ILogger logger, string fileName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error deleting static file {FileName} from MinIO")]
    static partial void LogDeleteStaticFileError(ILogger logger, Exception ex, string fileName);
}
