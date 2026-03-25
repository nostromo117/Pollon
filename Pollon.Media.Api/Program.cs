using Marten;
using Pollon.Backoffice.Models;
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

app.Run();
