using Microsoft.Extensions.Logging;

namespace Pollon.Backoffice.Api.Extensions;

public static partial class EndpointExtensions
{
    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Created ContentType {DisplayName} ({SystemName}) with {FieldCount} fields.")]
        public static partial void LogContentTypeCreated(ILogger logger, string displayName, string systemName, int fieldCount);

        [LoggerMessage(Level = LogLevel.Information, Message = "Updated ContentType {DisplayName} ({SystemName}). Fields updated: {FieldNames}")]
        public static partial void LogContentTypeUpdated(ILogger logger, string displayName, string systemName, string fieldNames);
    }
}
