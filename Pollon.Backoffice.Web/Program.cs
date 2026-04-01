using Pollon.Backoffice.Web;
using Pollon.Backoffice.Web.Components;
using Pollon.Backoffice.Web.Extensions;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<TokenProvider>();

builder.Services.AddOutputCache();
builder.Services.AddMudServices();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<BackofficeApiClient>(client =>
    {
        client.BaseAddress = new("https+http://backofficeapi");
        client.Timeout = TimeSpan.FromMinutes(5);
    });

builder.Services.AddHttpClient("MediaApi", client =>
{
    client.BaseAddress = new("https+http://mediaapi");
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddBackofficeAuthentication(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.MapAuthenticationEndpoints();

app.MapGet("/api/media/{id}", async (string id, IHttpClientFactory factory, TokenProvider tokenProvider, CancellationToken ct) =>
{
    var client = factory.CreateClient("MediaApi");
    if (!string.IsNullOrEmpty(tokenProvider.AccessToken))
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);
    }
    
    var response = await client.GetAsync($"/api/media/{id}", ct);
    if (!response.IsSuccessStatusCode) return Results.NotFound();
    
    var stream = await response.Content.ReadAsStreamAsync(ct);
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
    return Results.File(stream, contentType);
}).ExcludeFromDescription();

app.Run();
