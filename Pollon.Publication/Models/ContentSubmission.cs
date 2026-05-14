using System;

namespace Pollon.Publication.Models;

public class ContentSubmission
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ContentItemId { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    
    // Auth context (who submitted it, since it requires auth)
    public string? UserId { get; set; }
    public string? UserName { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // The raw JSON data submitted
    public string JsonData { get; set; } = string.Empty;
}
