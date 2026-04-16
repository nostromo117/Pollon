using System.ComponentModel.DataAnnotations;

namespace Pollon.Publication.Models;

public class PublishedContent
{
    public string Id { get; set; } = string.Empty;
    public string ContentTypeId { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public DateTime PublishedAt { get; set; }
    public string JsonData { get; set; } = "{}";
    public string? HtmlContent { get; set; }
    public string PublishMode { get; set; } = "Headless";
    public string SearchText { get; set; } = string.Empty;
}
