using Pollon.Backoffice.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddBackofficeAuthentication(builder.Configuration);
builder.AddBackofficeData();
builder.Services.AddBackofficeServices();
builder.Services.AddBackofficeHttpClients();
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
