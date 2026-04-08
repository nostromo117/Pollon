using Scriban;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;

namespace Pollon.Content.Api.Services;

public interface ITemplateRenderer
{
    Task<string> RenderAsync(string templateName, object data);
}

public partial class ScribanTemplateRenderer : ITemplateRenderer
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ScribanTemplateRenderer> _logger;
    private const string TemplatesFolder = "Templates";

    public ScribanTemplateRenderer(IWebHostEnvironment env, ILogger<ScribanTemplateRenderer> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<string> RenderAsync(string templateName, object data)
    {
        // Fallback to default if not specified
        if (string.IsNullOrEmpty(templateName))
        {
            templateName = "default";
        }

        // Ensure extension
        if (!templateName.EndsWith(".sbn"))
        {
            templateName += ".sbn";
        }

        var templatePath = Path.Combine(_env.ContentRootPath, TemplatesFolder, templateName);

        if (!File.Exists(templatePath))
        {
            LogTemplateNotFound(_logger, templatePath);
            templatePath = Path.Combine(_env.ContentRootPath, TemplatesFolder, "default.sbn");
            
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template not found: {templateName} and default fallback also missing.");
            }
        }

        try
        {
            var templateSource = await File.ReadAllTextAsync(templatePath);
            var template = Template.Parse(templateSource);

            if (template.HasErrors)
            {
                var errors = string.Join(" | ", template.Messages.Select(x => x.ToString()));
                LogParsingErrors(_logger, templateName, errors);
                throw new Exception($"Template parsing errors: {errors}");
            }

            // If data is a JsonElement or Dictionary, Scriban can handle it, 
            // but for better compatibility we might want to normalize it.
            // Scriban natively supports anonymous objects and dictionaries.
            
            var result = await template.RenderAsync(data);
            return result;
        }
        catch (Exception ex)
        {
            LogRenderError(_logger, ex, templateName);
            throw;
        }
    }
}
