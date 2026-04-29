using Scriban;
using Scriban.Runtime;

namespace Pollon.Content.Api.Services;

public interface ITemplateRenderer
{
    Task<string> RenderAsync(string templateName, object data, string? inlineContent = null, Dictionary<string, string>? variables = null);
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

    public async Task<string> RenderAsync(string templateName, object data, string? inlineContent = null, Dictionary<string, string>? variables = null)
    {
        string templateSource;

        if (!string.IsNullOrWhiteSpace(inlineContent))
        {
            // Inline content stored in DB has priority over filesystem files
            templateSource = inlineContent;
        }
        else
        {
            if (string.IsNullOrEmpty(templateName))
                templateName = "default";

            if (!templateName.EndsWith(".sbn"))
                templateName += ".sbn";

            var templatePath = Path.Combine(_env.ContentRootPath, TemplatesFolder, templateName);

            if (!File.Exists(templatePath))
            {
                LogTemplateNotFound(_logger, templatePath);
                templatePath = Path.Combine(_env.ContentRootPath, TemplatesFolder, "default.sbn");

                if (!File.Exists(templatePath))
                    throw new FileNotFoundException($"Template not found: {templateName} and default fallback also missing.");
            }

            templateSource = await File.ReadAllTextAsync(templatePath);
        }

        try
        {
            var template = Template.Parse(templateSource);

            if (template.HasErrors)
            {
                var errors = string.Join(" | ", template.Messages.Select(x => x.ToString()));
                LogParsingErrors(_logger, templateName, errors);
                throw new Exception($"Template parsing errors: {errors}");
            }

            var scriptObject = new ScriptObject();

            // Inject data fields
            if (data is Dictionary<string, object> dict)
            {
                foreach (var kv in dict)
                    scriptObject.SetValue(kv.Key, kv.Value, readOnly: false);
            }
            else
            {
                scriptObject.Import(data);
            }

            // Inject template variables under the "vars" key
            if (variables is { Count: > 0 })
            {
                var varsObj = new ScriptObject();
                foreach (var kv in variables)
                    varsObj.SetValue(kv.Key, kv.Value, readOnly: false);
                scriptObject.SetValue("vars", varsObj, readOnly: false);
            }

            var context = new TemplateContext();
            context.PushGlobal(scriptObject);

            var result = await template.RenderAsync(context);
            return result;
        }
        catch (Exception ex)
        {
            LogRenderError(_logger, ex, templateName);
            throw;
        }
    }
}
