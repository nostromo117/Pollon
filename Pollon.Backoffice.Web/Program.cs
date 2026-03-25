using Pollon.Backoffice.Web;
using Pollon.Backoffice.Web.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();
builder.Services.AddMudServices();

builder.Services.AddHttpClient<BackofficeApiClient>(client =>
    {
        client.BaseAddress = new("https+http://backofficeapi");
    });

builder.Services.AddHttpClient("MediaApi", client =>
{
    client.BaseAddress = new("https+http://mediaapi");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

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
