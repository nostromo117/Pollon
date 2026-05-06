
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

    public List<FieldItem> Data { get; set; } = new();

    public List<string> Warnings { get; set; } = new();

    public string SearchText { get; set; } = string.Empty;

    public string GetTitle()
    {
        var titleField = Data.FirstOrDefault(f => f.IsTitle) ?? 
                         Data.FirstOrDefault(f => 
                             f.Name.Equals("Title", StringComparison.OrdinalIgnoreCase) || 
                             f.Name.Equals("Name", StringComparison.OrdinalIgnoreCase));

        if (titleField?.Value is not null)
        {
            return titleField.Value switch
            {
                System.Text.Json.JsonElement el when el.ValueKind == System.Text.Json.JsonValueKind.String => el.GetString() ?? Id,
                _ => titleField.Value.ToString() ?? Id
            };
        }

        return Id;
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
