using System.ComponentModel.DataAnnotations;

namespace Pollon.Contracts.Models;

public class PublishedContent
{
    public string Id { get; set; } = string.Empty;
    public string ContentTypeId { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public DateTime PublishedAt { get; set; }
    public string JsonData { get; set; } = "{}";
}
