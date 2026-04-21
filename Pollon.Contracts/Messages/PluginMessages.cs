namespace Pollon.Contracts.Messages;

public record RegisterPlugin(
    string Id, 
    string Name, 
    string ConsulServiceId, 
    string HeartbeatUrl, 
    string? AccessToken = null,
    string Version = "1.0.0", 
    string? Description = null,
    List<string>? SupportedContentTypes = null);

public record RegisterPluginResponse(bool Success, string? ErrorMessage = null);
