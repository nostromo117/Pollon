using Marten;
using Pollon.Publication.Models;
using Pollon.Backoffice.Repositories;
using Pollon.Backoffice.Services;
using Pollon.Media.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();

// Setup Marten specific to MediaAsset
builder.Services.AddMarten(opts => 
{
    opts.Connection(builder.Configuration.GetConnectionString("backofficedb")!);
}).UseLightweightSessions();

builder.Services.AddScoped(typeof(IRepository<>), typeof(MartenRepository<>));
builder.Services.AddScoped<IMediaStorageService, DatabaseMediaStorageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Media Endpoints
var media = app.MapGroup("/api/media").WithTags("Media");

media.MapPost("/", async (IFormFile file, IMediaStorageService mediaService, CancellationToken ct) =>
{
    if (file is null || file.Length == 0) return Results.BadRequest("Invalid file.");
    
    using var stream = file.OpenReadStream();
    var asset = await mediaService.SaveFileAsync(file.FileName, stream, file.ContentType, ct);
    
    return Results.Created($"/api/media/{asset.Id}", new {
        asset.Id, asset.FileName, asset.MimeType, asset.SizeInBytes, asset.Url, asset.CreatedAt
    });
}).DisableAntiforgery();

media.MapGet("/{id}", async (string id, IMediaStorageService mediaService, CancellationToken ct) =>
{
    var asset = await mediaService.GetFileAsync(id, ct);
    if (asset is null) return Results.NotFound();
    
    return Results.File(asset.Data, asset.MimeType, asset.FileName);
});

// Gallery Endpoints
var galleries = app.MapGroup("/api/galleries").WithTags("Galleries");

galleries.MapGet("/", async (IRepository<MediaGallery> repository, CancellationToken ct) =>
{
    var list = await repository.GetAllAsync();
    return Results.Ok(list);
});

galleries.MapGet("/{id}", async (string id, IRepository<MediaGallery> repository, CancellationToken ct) =>
{
    var gallery = await repository.GetByIdAsync(id);
    return gallery is not null ? Results.Ok(gallery) : Results.NotFound();
});

galleries.MapPost("/", async (HttpRequest request, IMediaStorageService mediaService, IRepository<MediaGallery> repository, CancellationToken ct) =>
{
    if (!request.HasFormContentType) return Results.BadRequest("Expected form content.");
    
    var form = await request.ReadFormAsync(ct);
    var name = form["name"].ToString();
    if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest("Gallery name is required.");
    
    var isPublished = bool.TryParse(form["isPublished"], out var published) && published;
    
    var files = form.Files;
    var assets = new List<MediaAsset>();
    
    foreach (var file in files)
    {
        using var stream = file.OpenReadStream();
        var asset = await mediaService.SaveFileAsync(file.FileName, stream, file.ContentType, ct);
        assets.Add(asset);
    }
    
    var gallery = new MediaGallery
    {
        Id = Guid.NewGuid().ToString(),
        Name = name,
        IsPublished = isPublished,
        AssetIds = assets.Select(a => a.Id).ToList()
    };
    
    await repository.CreateAsync(gallery);
    
    return Results.Created($"/api/galleries/{gallery.Id}", gallery);
}).DisableAntiforgery();

galleries.MapPut("/{id}", async (string id, MediaGallery gallery, IRepository<MediaGallery> repository, CancellationToken ct) =>
{
    if (id != gallery.Id) return Results.BadRequest("ID mismatch.");
    await repository.UpdateAsync(id, gallery);
    return Results.NoContent();
});

galleries.MapDelete("/{id}", async (string id, IRepository<MediaGallery> repository, CancellationToken ct) =>
{
    await repository.DeleteAsync(id);
    return Results.NoContent();
});

app.Run();
