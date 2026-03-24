
namespace Pollon.Backoffice.Models;

public class ContentItem
{
    public string Id { get; set; } = string.Empty;

    public string ContentTypeId { get; set; } = string.Empty;

    public string Status { get; set; } = "Draft"; // Draft, Published, Archived
    public string Slug { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    // Flessibile dictionary per contenere i campi dinamici
    public Dictionary<string, object> Data { get; set; } = new();
}
