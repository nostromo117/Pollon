
namespace Pollon.Publication.Models;

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

    public List<string> Warnings { get; set; } = new();

    public string SearchText { get; set; } = string.Empty;

    public string? UseAsTitle { get; set; }

    public string GetTitle()
    {
        if (!string.IsNullOrEmpty(UseAsTitle) && Data.TryGetValue(UseAsTitle, out var val) && val != null)
        {
            if (val is System.Text.Json.JsonElement el && el.ValueKind == System.Text.Json.JsonValueKind.String)
                return el.GetString() ?? Id;
            return val.ToString() ?? Id;
        }

        if (string.IsNullOrEmpty(UseAsTitle))
        {
            if (Data.TryGetValue("Title", out var t) || Data.TryGetValue("title", out t) || 
                Data.TryGetValue("Name", out t) || Data.TryGetValue("name", out t))
            {
                if (t is System.Text.Json.JsonElement el && el.ValueKind == System.Text.Json.JsonValueKind.String)
                    return el.GetString() ?? Id;
                return t.ToString() ?? Id;
            }
            return Id;
        }

        return UseAsTitle!;
    }

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
