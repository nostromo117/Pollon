
namespace Pollon.Backoffice.Models;

public class MediaAsset
{
    public string Id { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
