namespace Pollon.Contracts.Events;

public record ContentPublishedEvent(string ContentItemId);

public record ContentUpdatedEvent(string ContentItemId);

public record ContentDeletedEvent(string ContentItemId);

public record TemplatePublishedEvent(
    string Id,
    string Name,
    string DisplayName,
    string Description,
    string FileName,
    string PreviewImageUrl,
    string? TemplateContent,
    System.Collections.Generic.Dictionary<string, string> Variables,
    bool IsActive,
    string? AssociatedContentTypeId,
    System.Collections.Generic.List<string> Tags,
    System.DateTime CreatedAt,
    System.DateTime UpdatedAt
);

public record TemplateDeletedEvent(string Id);
