
namespace Pollon.Backoffice.Models;

public class ContentItem
{
    public string Id { get; set; } = string.Empty;

    public string ContentTypeId { get; set; } = string.Empty;

    public string Status { get; set; } = "Draft"; // Draft, Published, Archived
    public string Slug { get; set; } = string.Empty;
    public string? Icon { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public string? ParentId { get; set; }
    public List<ContentItem> Children { get; set; } = new();
    
    public string? GalleryId { get; set; }
    
    public PublishMode? PublishModeOverride { get; set; }

    // Flessibile dictionary per contenere i campi dinamici
    public Dictionary<string, object> Data { get; set; } = new();

    public string SearchText { get; set; } = string.Empty;

    public override bool Equals(object? obj)
    {
        if (obj is ContentItem other)
        {
            return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Id?.ToLowerInvariant().GetHashCode() ?? 0;
    }
}
