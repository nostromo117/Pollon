
namespace Pollon.Backoffice.Models;

public class Category
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    
    public string? ParentId { get; set; }
    
    public int Order { get; set; }
}
