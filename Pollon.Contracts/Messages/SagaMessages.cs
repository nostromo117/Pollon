namespace Pollon.Contracts.Messages;

public record StartContentPublication(string Id, string ContentType);

public record PluginValidationRequest(string Id, string ContentJson);

public record PluginValidationResponse(string Id, string PluginId, bool Success, string? Warning = null);

public record PublicationTimeout(string Id);

public record PublicationCompleted(string Id, List<string> Warnings);
