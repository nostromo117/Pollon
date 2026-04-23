using Microsoft.Extensions.Logging;

namespace Pollon.Backoffice.Services;

public partial class PluginSyncService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "PluginSyncService started. Polling Consul at {Addr}")]
    static partial void LogStarted(ILogger logger, string addr);

    [LoggerMessage(Level = LogLevel.Information, Message = "Plugin {Id} (ConsulID: {ConsulId}) is gone from Consul. Marking as Offline.")]
    static partial void LogPluginOffline(ILogger logger, string id, string consulId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error syncing plugin status from Consul.")]
    static partial void LogSyncError(ILogger logger, Exception ex);
}
