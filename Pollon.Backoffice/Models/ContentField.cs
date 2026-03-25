namespace Pollon.Backoffice.Models;

public class ContentField
{
    public string Name { get; set; } = string.Empty;
    public ContentFieldType FieldType { get; set; } = ContentFieldType.Text;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
}
