using Pollon.Publication.Models;
using Pollon.Content.Api.Services;
using System.Text.Json;

namespace Pollon.Content.Api.Templates
{
    public static class RenderTemplate
    {


        public static async Task<string?> RenderContent(
            ContentItem contentItem,
            ContentType contentType,
            ITemplateRenderer renderer,
            MediaGallery? gallery,
            ContentTemplate? contentTemplate = null)
        {
            var templateData = contentItem.Data
                .Where(x => x.Value != null)
                .GroupBy(x => x.Name)
                .ToDictionary(g => g.Key, g => g.First().Value!);

            // Metadata
            templateData["id"] = contentItem.Id;
            templateData["slug"] = contentItem.Slug;
            templateData["published_at"] = contentItem.PublishedAt ?? DateTime.UtcNow;
            templateData["content_type"] = contentType.DisplayName;
            templateData["content_type_is_interactive"] = contentType.IsInteractive;
            
            // Pass fields structure to template for form generation
            templateData["content_type_fields"] = contentType.Fields.OrderBy(f => f.Position).Select(f => new Dictionary<string, object>
            {
                { "name", f.Name },
                { "type", f.FieldType.ToString() },
                { "is_required", f.IsRequired }
            }).ToList();

            if (!templateData.ContainsKey("title") && !templateData.ContainsKey("Title"))
                templateData["title"] = contentItem.GetTitle();

            if (gallery != null && gallery.AssetIds.Any())
            {
                List<Dictionary<string, string>> images = [.. gallery.AssetIds.Select(id => new Dictionary<string, string>
                {
                    { "url", $"/api/media/{id}" },
                    { "alt", "Gallery Image" }
                })];
                templateData["images"] = images;
            }

            var inlineContent = contentTemplate?.TemplateContent;
            var variables = contentTemplate?.Variables;

            return await renderer.RenderAsync(
                contentType.TemplateName ?? "default",
                templateData,
                inlineContent,
                variables);
        }
    }
}
