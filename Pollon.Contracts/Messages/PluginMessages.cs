namespace Pollon.Contracts.Messages;

public record RegisterPlugin(
    string Id, 
    string Name, 
    string ConsulServiceId, 
    string HeartbeatUrl, 
    string Version = "1.0.0", 
    string? Description = null);
