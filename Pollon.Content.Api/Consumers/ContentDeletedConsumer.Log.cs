using Microsoft.Extensions.Logging;

namespace Pollon.Content.Api.Consumers;

public partial class ContentDeletedConsumer
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Received ContentDeletedEvent for ContentItemId {ContentItemId}")]
    static partial void LogReceivedEvent(ILogger logger, string contentItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted ContentItemId {ContentItemId} from SQL Server")]
    static partial void LogDeletedSuccess(ILogger logger, string contentItemId);
}
