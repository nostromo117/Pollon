using Microsoft.Extensions.Logging;

namespace Pollon.Plugin.Example.Services;

public partial class PluginRegistrationService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Registering with Consul at {ConsulAddr}. Self URL: {SelfUrl}")]
    static partial void LogRegisteringWithConsul(ILogger logger, string consulAddr, string selfUrl);

    [LoggerMessage(Level = LogLevel.Information, Message = "Requesting JWT from Keycloak...")]
    static partial void LogRequestingToken(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "JWT obtained successfully.")]
    static partial void LogTokenObtained(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully registered in Consul with ID: {Id}")]
    static partial void LogConsulRegistrationSuccess(ILogger logger, string id);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully registered in Backoffice! Dashboard should show the plugin now.")]
    static partial void LogBackofficeRegistrationSuccess(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Backoffice REJECTED registration: {Message}")]
    static partial void LogBackofficeRegistrationRejected(ILogger logger, string? message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to register plugin. Security check failed or host unreachable.")]
    static partial void LogRegistrationError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Plugin service is stopping...")]
    static partial void LogServiceStopping(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deregistering from Consul...")]
    static partial void LogDeregisteringConsul(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to deregister from Consul: {Message}")]
    static partial void LogDeregistrationWarning(ILogger logger, string message);
}
