using Microsoft.Extensions.Logging;

namespace Pollon.Backoffice.Handlers;

public partial class PluginHandler
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Registering plugin: {Name} ({Id}) with Consul Service ID: {ConsulServiceId}")]
    static partial void LogRegisteringPlugin(ILogger logger, string name, string id, string consulServiceId);
}
