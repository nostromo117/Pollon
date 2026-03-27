using Pollon.Backoffice.Models;
using Pollon.Backoffice.Repositories;
using Pollon.Backoffice.Services;

namespace Pollon.Backoffice.Api.Extensions;

public static class EndpointExtensions
{
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

        group.MapPost("/", async (ContentType item, IRepository<ContentType> repo) =>
        {
            if (string.IsNullOrWhiteSpace(item.DisplayName) || 
                string.IsNullOrWhiteSpace(item.SystemName) || 
                string.IsNullOrWhiteSpace(item.Slug))
            {
                return Results.BadRequest("DisplayName, SystemName and Slug are required.");
            }

            await repo.CreateAsync(item);
            return Results.Created($"/api/content-types/{item.Id}", item);
        });

        group.MapPut("/{id}", async (string id, ContentType item, IRepository<ContentType> repo) =>
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
            IContentItemService service) =>
        {
            return Results.Ok(await service.GetAllAsync(status, sortBy, sortDescending ?? true));
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

        return endpoints;
    }
}
