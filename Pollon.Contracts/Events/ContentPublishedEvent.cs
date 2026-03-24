namespace Pollon.Contracts.Events;

public record ContentPublishedEvent(string ContentItemId);

public record ContentUpdatedEvent(string ContentItemId);

public record ContentDeletedEvent(string ContentItemId);
