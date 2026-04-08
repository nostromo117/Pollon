using Microsoft.Extensions.Logging;

namespace Pollon.Content.Api.Services;

public partial class ScribanTemplateRenderer
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Template {TemplatePath} not found. Falling back to default.sbn.")]
    static partial void LogTemplateNotFound(ILogger logger, string templatePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scriban parsing errors in {TemplateName}: {Errors}")]
    static partial void LogParsingErrors(ILogger logger, string templateName, string errors);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error rendering template {TemplateName}")]
    static partial void LogRenderError(ILogger logger, Exception ex, string templateName);
}
