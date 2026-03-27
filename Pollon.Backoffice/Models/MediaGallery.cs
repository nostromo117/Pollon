using System;
using System.Collections.Generic;

namespace Pollon.Backoffice.Models;

public class MediaGallery
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public List<string> AssetIds { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
