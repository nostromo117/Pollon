using Microsoft.Extensions.Logging;

namespace Pollon.Backoffice.Handlers;

public partial class ContentPublicationSaga
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Starting Content Publication Saga for Item: {Id} (Type: {Type})")]
    static partial void LogStartingSaga(ILogger logger, string id, string type);

    [LoggerMessage(Level = LogLevel.Information, Message = "Waiting for {Count} plugins: {PluginIds}")]
    static partial void LogWaitingForPlugins(ILogger logger, int count, string pluginIds);

    [LoggerMessage(Level = LogLevel.Information, Message = "No plugins found for content type {Type}. Completing immediately.")]
    static partial void LogNoPluginsFound(ILogger logger, string type);

    [LoggerMessage(Level = LogLevel.Information, Message = "Received response from Plugin {PluginId} for Item {Id}. Success: {Success}")]
    static partial void LogReceivedResponse(ILogger logger, string pluginId, string id, bool success);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Publication timeout for Item {Id}. {Count} plugins did not respond: {PluginIds}")]
    static partial void LogPublicationTimeout(ILogger logger, string? id, int count, string pluginIds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finalizing publication for Item {Id}. Total Warnings: {Warnings}")]
    static partial void LogFinalizingPublication(ILogger logger, string? id, int warnings);
}
