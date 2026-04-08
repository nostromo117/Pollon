using Pollon.Frontend.Web.Clients;
using Pollon.Frontend.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add HttpClient for Content API
builder.Services.AddHttpClient<ContentApiClient>(client =>
{
    client.BaseAddress = new("https+http://contentapi");
});

// Configure explicit proxy client for Media Assets
builder.Services.AddHttpClient("MediaApi", client =>
{
    client.BaseAddress = new("https+http://mediaapi");
});

// Configure YARP for Static Files (Reverse Proxy to MinIO)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

// YARP Reverse Proxy Mapping
app.MapReverseProxy();

app.MapGet("/api/media/{id}", async (string id, IHttpClientFactory factory, CancellationToken ct) =>
{
    var client = factory.CreateClient("MediaApi");
    var response = await client.GetAsync($"/api/media/{id}", ct);
    if (!response.IsSuccessStatusCode) return Results.NotFound();
    
    var stream = await response.Content.ReadAsStreamAsync(ct);
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
    return Results.File(stream, contentType);
}).ExcludeFromDescription();

app.Run();
