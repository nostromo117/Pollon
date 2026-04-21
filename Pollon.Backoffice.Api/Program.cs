using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Pollon.Backoffice.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddConsulConfiguration();

builder.Services.AddOpenApi();

builder.Services.AddBackofficeAuthentication(builder.Configuration);
builder.AddBackofficeData();
builder.Services.AddBackofficeServices();
builder.Services.AddBackofficeHttpClients();

// Register OIDC ConfigurationManager for Keycloak
var keycloakUrl = builder.Configuration.GetConnectionString("keycloak") ?? builder.Configuration["Keycloak:Url"] ?? "http://localhost:8080";
if (!keycloakUrl.StartsWith("http")) keycloakUrl = $"http://{keycloakUrl}";
var metadataAddress = $"{keycloakUrl.TrimEnd('/')}/realms/Pollon/.well-known/openid-configuration";

builder.Services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(sp => 
    new ConfigurationManager<OpenIdConnectConfiguration>(
        metadataAddress, 
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever(sp.GetRequiredService<IHttpClientFactory>().CreateClient()) { RequireHttps = false }
    ));
builder.Host.AddBackofficeMessaging(builder.Configuration, typeof(Pollon.Backoffice.Handlers.PluginHandler).Assembly);

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map Endpoints
app.MapBackofficeEndpoints();

app.Run();
