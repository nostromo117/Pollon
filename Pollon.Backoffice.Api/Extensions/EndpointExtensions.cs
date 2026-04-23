using Pollon.Publication.Models;
using Pollon.Backoffice.Repositories;
using Pollon.Backoffice.Services;

namespace Pollon.Backoffice.Api.Extensions;

public static partial class EndpointExtensions
{
    public static IEndpointRouteBuilder MapBackofficeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapContentTypeEndpoints();
        endpoints.MapContentItemEndpoints();
        endpoints.MapGalleryEndpoints();
        endpoints.MapMediaEndpoints();
        endpoints.MapPluginEndpoints();

        return endpoints;
    }

    public static IEndpointRouteBuilder MapPluginEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/plugins").WithTags("Plugins").RequireAuthorization();

        group.MapGet("/", async (IRepository<Pollon.Contracts.Models.PluginInfo> repo) =>
        {
            return Results.Ok(await repo.GetAllAsync());
        });
        
        group.MapPut("/{id}", async (string id, Pollon.Contracts.Models.PluginInfo item, IRepository<Pollon.Contracts.Models.PluginInfo> repo) =>
        {
            var existing = await repo.GetByIdAsync(id);
            if (existing is null) return Results.NotFound();
            
            existing.EnabledContentTypes = item.EnabledContentTypes;
            await repo.UpdateAsync(id, existing);
            return Results.NoContent();
        });

        group.MapPost("/identity", async (PluginIdentityRequest request, IKeycloakAdminClient adminClient) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest("Plugin name is required.");
            
            var credentials = await adminClient.CreatePluginClientAsync(request.Name);
            return credentials != null ? Results.Ok(credentials) : Results.Problem("Failed to create plugin identity in Keycloak.");
        });

        return endpoints;
    }

    public record PluginIdentityRequest(string Name);


    public static IEndpointRouteBuilder MapContentTypeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/content-types").WithTags("Content Types").RequireAuthorization();

        group.MapGet("/", async (IRepository<ContentType> repo) =>
        {
            return Results.Ok(await repo.GetAllAsync());
        });

        group.MapGet("/{id}", async (string id, IRepository<ContentType> repo) =>
        {
            var item = await repo.GetByIdAsync(id);
            return item is not null ? Results.Ok(item) : Results.NotFound();
        });

        group.MapPost("/", async (ContentType item, IRepository<ContentType> repo, ILogger<ContentType> logger) =>
        {
            if (string.IsNullOrWhiteSpace(item.DisplayName) || 
                string.IsNullOrWhiteSpace(item.SystemName) || 
                string.IsNullOrWhiteSpace(item.Slug))
            {
                return Results.BadRequest("DisplayName, SystemName and Slug are required.");
            }

            await repo.CreateAsync(item);
            Log.LogContentTypeCreated(logger, item.DisplayName, item.SystemName, item.Fields.Count);
            return Results.Created($"/api/content-types/{item.Id}", item);
        });

        group.MapPut("/{id}", async (string id, ContentType item, IRepository<ContentType> repo, ILogger<ContentType> logger) =>
        {
            var existingItem = await repo.GetByIdAsync(id);
            if (existingItem is null) return Results.NotFound();
            
            if (string.IsNullOrWhiteSpace(item.DisplayName) || 
                string.IsNullOrWhiteSpace(item.SystemName) || 
                string.IsNullOrWhiteSpace(item.Slug))
            {
                return Results.BadRequest("DisplayName, SystemName and Slug are required.");
            }

            item.Id = id;
            await repo.UpdateAsync(id, item);
            Log.LogContentTypeUpdated(logger, item.DisplayName, item.SystemName, 
                string.Join(", ", item.Fields.OrderBy(f => f.Position).Select(f => $"{f.Name} [pos:{f.Position}]")));
            return Results.NoContent();
        });

        group.MapDelete("/{id}", async (string id, IRepository<ContentType> repo) =>
        {
            var existingItem = await repo.GetByIdAsync(id);
            if (existingItem is null) return Results.NotFound();

            await repo.DeleteAsync(id);
            return Results.NoContent();
        });

        return endpoints;
    }

    public static IEndpointRouteBuilder MapContentItemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/content-items").WithTags("Content Items").RequireAuthorization();

        group.MapGet("/", async (
            string? status, 
            string? sortBy, 
            bool? sortDescending,
            string? searchTerm,
            IContentItemService service) =>
        {
            return Results.Ok(await service.GetAllAsync(status, sortBy, sortDescending ?? true, searchTerm));
        });

        group.MapGet("/{id}", async (string id, IContentItemService service) =>
        {
            var item = await service.GetByIdAsync(id);
            return item is not null ? Results.Ok(item) : Results.NotFound();
        });

        group.MapPost("/", async (ContentItem item, IContentItemService service) =>
        {
            var created = await service.CreateAndPublishAsync(item);
            return Results.Created($"/api/content-items/{created.Id}", created);
        });

        group.MapPut("/{id}", async (string id, ContentItem item, IContentItemService service) =>
        {
            var updated = await service.UpdateAndPublishAsync(id, item);
            return updated is not null ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{id}", async (string id, IContentItemService service) =>
        {
            await service.DeleteAndPublishAsync(id);
            return Results.NoContent();
        });
        
        group.MapPost("/sync", async (IContentItemService service) =>
        {
            await service.RepublishAllAsync();
            return Results.Accepted();
        });

        return endpoints;
    }

    public static IEndpointRouteBuilder MapGalleryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/galleries").WithTags("Galleries").RequireAuthorization();

        group.MapGet("/", async (bool? includeUnpublished, IRepository<MediaGallery> repo) =>
        {
            var all = await repo.GetAllAsync();
            if (includeUnpublished == true) return Results.Ok(all);
            return Results.Ok(all.Where(x => x.IsPublished));
        });

        group.MapGet("/{id}", async (string id, IRepository<MediaGallery> repo) =>
        {
            var item = await repo.GetByIdAsync(id);
            return item is not null ? Results.Ok(item) : Results.NotFound();
        });

        group.MapPost("/", async (HttpContext context, IHttpClientFactory factory, IRepository<MediaGallery> repo) =>
        {
            if (context.Request.HasFormContentType)
            {
                // Forward multipart request to Media.Api
                var client = factory.CreateClient("MediaApi");
                
                // Get token from incoming request
                if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader.ToArray());
                }

                // Copy headers and stream the content
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/galleries");
                
                var streamContent = new StreamContent(context.Request.Body);
                foreach (var header in context.Request.Headers)
                {
                    // Skip Host and Authorization (manually added)
                    if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) && 
                        !header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                    {
                        streamContent.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }
                request.Content = streamContent;

                try 
                {
                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                    {
                        return Results.StatusCode((int)response.StatusCode);
                    }
                    
                    return Results.Stream(await response.Content.ReadAsStreamAsync(), 
                                         response.Content.Headers.ContentType?.ToString());
                }
                catch (Exception ex)
                {
                    return Results.Problem("Error forwarding request to Media.Api: " + ex.Message);
                }
            }
            else
            {
                // Handle JSON as before
                var item = await context.Request.ReadFromJsonAsync<MediaGallery>();
                if (item == null || string.IsNullOrWhiteSpace(item.Name)) return Results.BadRequest("Gallery name is required.");
                await repo.CreateAsync(item);
                return Results.Created($"/api/galleries/{item.Id}", item);
            }
        }).DisableAntiforgery();

        group.MapPut("/{id}", async (string id, MediaGallery item, IRepository<MediaGallery> repo) =>
        {
            var existing = await repo.GetByIdAsync(id);
            if (existing is null) return Results.NotFound();
            item.Id = id;
            await repo.UpdateAsync(id, item);
            return Results.NoContent();
        });

        group.MapDelete("/{id}", async (string id, IRepository<MediaGallery> repo) =>
        {
            await repo.DeleteAsync(id);
            return Results.NoContent();
        });

        return endpoints;
    }

    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/media").WithTags("Media").RequireAuthorization();

        group.MapGet("/{id}", async (string id, IHttpClientFactory factory, HttpContext context) =>
        {
            var client = factory.CreateClient("MediaApi");

            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader.ToArray());
            }

            var response = await client.GetAsync($"/api/media/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return Results.StatusCode((int)response.StatusCode);
            }

            return Results.Stream(await response.Content.ReadAsStreamAsync(), 
                                 response.Content.Headers.ContentType?.ToString());
        });

        group.MapPost("/", async (HttpContext context, IHttpClientFactory factory) =>
        {
            if (!context.Request.HasFormContentType)
            {
                return Results.BadRequest("Expected form content.");
            }

            var client = factory.CreateClient("MediaApi");

            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader.ToArray());
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/media");
            var streamContent = new StreamContent(context.Request.Body);
            
            foreach (var header in context.Request.Headers)
            {
                if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) && 
                    !header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) &&
                    !header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    streamContent.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
            request.Content = streamContent;

            try 
            {
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return Results.StatusCode((int)response.StatusCode);
                }
                
                return Results.Stream(await response.Content.ReadAsStreamAsync(), 
                                     response.Content.Headers.ContentType?.ToString());
            }
            catch (Exception ex)
            {
                return Results.Problem("Error forwarding request to Media.Api: " + ex.Message);
            }
        }).DisableAntiforgery();

        group.MapDelete("/{id}", async (string id, IHttpClientFactory factory, HttpContext context) =>
        {
            var client = factory.CreateClient("MediaApi");

            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader.ToArray());
            }

            var response = await client.DeleteAsync($"/api/media/{id}");
            return Results.StatusCode((int)response.StatusCode);
        });

        return endpoints;
    }
}
