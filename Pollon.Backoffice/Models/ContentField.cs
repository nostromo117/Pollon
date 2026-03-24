namespace Pollon.Backoffice.Models;

public class ContentField
{
    public string Name { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty; // e.g., "text", "richtext", "number", "media", "reference"
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
}
