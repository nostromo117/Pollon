using Microsoft.Extensions.Logging;

namespace Pollon.Plugin.Example.Handlers;

public partial class SagaParticipantHandler
{
    [LoggerMessage(Level = LogLevel.Information, Message = " [SAGA] RECEIVED: Validation Request for Item ID: {Id}")]
    static partial void LogReceivedValidationRequest(ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Information, Message = " [SAGA] SENDING: Validation Result for Item ID: {Id}. Success: {Success}")]
    static partial void LogSendingValidationResult(ILogger logger, string id, bool success);
}
