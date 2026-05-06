namespace Pollon.Publication.Models;

public class FieldItem
{
    public string Name { get; set; } = string.Empty;
    public object? Value { get; set; }
    public bool IsTitle { get; set; }
}
