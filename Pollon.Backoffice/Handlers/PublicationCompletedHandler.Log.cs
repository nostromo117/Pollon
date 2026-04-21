using Microsoft.Extensions.Logging;

namespace Pollon.Backoffice.Handlers;

public partial class PublicationCompletedHandler
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Finalizing Publication in DB for Item: {Id}")]
    static partial void LogFinalizingInDb(ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Error, Message = "Content Item {Id} not found during publication completion.")]
    static partial void LogItemNotFound(ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Information, Message = "Content Item {Id} is now officially PUBLISHED with {Count} warnings.")]
    static partial void LogOfficiallyPublished(ILogger logger, string id, int count);
}
