
namespace Pollon.Publication.Models;

public class ContentType
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // SEO-friendly name
    public string Description { get; set; } = string.Empty;

    public PublishMode PublishMode { get; set; } = PublishMode.Headless;
    public string? TemplateName { get; set; }

    public List<ContentField> Fields { get; set; } = new();
}

public enum PublishMode
{
    Headless,
    Static,
    Both
}
