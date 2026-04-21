namespace Pollon.Contracts.Models;

public class PluginInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConsulServiceId { get; set; } = string.Empty;
    public string HeartbeatUrl { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string? Description { get; set; }
    public List<string> SupportedContentTypes { get; set; } = new();
    public DateTime LastSeen { get; set; }
    public string Status { get; set; } = "Offline";
}

