using Wolverine;
using Wolverine.RabbitMQ;
using Marten;
using Pollon.Backoffice.Models;
using Pollon.Backoffice.Repositories;
using Pollon.Backoffice.Services;
using Pollon.Backoffice.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();

// Setup Marten
builder.Services.AddMarten(opts => 
{
    opts.Connection(builder.Configuration.GetConnectionString("backofficedb")!);
}).UseLightweightSessions();

builder.Services.AddScoped(typeof(IRepository<>), typeof(MartenRepository<>));

builder.Services.AddScoped<IContentItemService, ContentItemService>();

// Setup Wolverine / RabbitMQ
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMq(builder.Configuration.GetConnectionString("messaging")!)
        .AutoProvision()
        .UseConventionalRouting();

    opts.Policies.UseDurableLocalQueues();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var contentTypes = app.MapGroup("/api/content-types").WithTags("Content Types");

contentTypes.MapGet("/", async (IRepository<ContentType> repo) =>
{
    return Results.Ok(await repo.GetAllAsync());
});

contentTypes.MapGet("/{id}", async (string id, IRepository<ContentType> repo) =>
{
    var item = await repo.GetByIdAsync(id);
    return item is not null ? Results.Ok(item) : Results.NotFound();
});

contentTypes.MapPost("/", async (ContentType item, IRepository<ContentType> repo) =>
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

contentTypes.MapPut("/{id}", async (string id, ContentType item, IRepository<ContentType> repo) =>
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

contentTypes.MapDelete("/{id}", async (string id, IRepository<ContentType> repo) =>
{
    var existingItem = await repo.GetByIdAsync(id);
    if (existingItem is null) return Results.NotFound();

    await repo.DeleteAsync(id);
    return Results.NoContent();
});

var contentItems = app.MapGroup("/api/content-items").WithTags("Content Items");

contentItems.MapGet("/", async (
    string? status, 
    string? sortBy, 
    bool? sortDescending,
    IContentItemService service) =>
{
    return Results.Ok(await service.GetAllAsync(status, sortBy, sortDescending ?? true));
});

contentItems.MapGet("/{id}", async (string id, IContentItemService service) =>
{
    var item = await service.GetByIdAsync(id);
    return item is not null ? Results.Ok(item) : Results.NotFound();
});

contentItems.MapPost("/", async (ContentItem item, IContentItemService service) =>
{
    var created = await service.CreateAndPublishAsync(item);
    return Results.Created($"/api/content-items/{created.Id}", created);
});

contentItems.MapPut("/{id}", async (string id, ContentItem item, IContentItemService service) =>
{
    var updated = await service.UpdateAndPublishAsync(id, item);
    return updated is not null ? Results.NoContent() : Results.NotFound();
});

contentItems.MapDelete("/{id}", async (string id, IContentItemService service) =>
{
    await service.DeleteAndPublishAsync(id);
    return Results.NoContent();
});

app.Run();
