using Microsoft.AspNetCore.Components.RenderTree;
using Pollon.Backoffice.Models;
using Pollon.Content.Api.Services;
using System.Text.Json;

namespace Pollon.Content.Api.Templates
{
   public static class RenderTemplate
   {
      private static string GetItemDisplayName(ContentItem ci)
      {
         if (ci.Data.TryGetValue("Title", out var t) || ci.Data.TryGetValue("title", out t) ||
             ci.Data.TryGetValue("Name", out t) || ci.Data.TryGetValue("name", out t))
         {
            if (t is JsonElement el && el.ValueKind == JsonValueKind.String)
               return el.GetString() ?? ci.Id;
            return t.ToString() ?? ci.Id;
         }
         return ci.Id;
      }
      public static async Task<string?> RenderContent(ContentItem contentItem, ContentType contentType, ITemplateRenderer renderer, MediaGallery? gallery)
      {
         var templateData = new Dictionary<string, object>(contentItem.Data);

         // Metadata
         templateData["id"] = contentItem.Id;
         templateData["slug"] = contentItem.Slug;
         templateData["published_at"] = contentItem.PublishedAt ?? DateTime.UtcNow;
         templateData["content_type"] = contentType.DisplayName;

         // Helper for Title if not already in Data
         if (!templateData.ContainsKey("title") && !templateData.ContainsKey("Title"))
         {
            templateData["title"] = GetItemDisplayName(contentItem);
         }

         // Gallery
    
            if (gallery != null && gallery.AssetIds.Any())
            {
               templateData["images"] = gallery.AssetIds.Select(id => new Dictionary<string, string>
                            {
                                { "url", $"/api/media/{id}" },
                                { "alt", "Gallery Image" }
                            }).ToList();
            }
         

        return await renderer.RenderAsync(contentType.TemplateName ?? "default", templateData);
        
      }
   }
}
