using Pollon.Backoffice.Mcp.Extensions;
using Pollon.Backoffice.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddBackofficeData();
builder.Services.AddBackofficeServices();
builder.Host.AddBackofficeMessaging(builder.Configuration);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<ContentTypeTools>()
    .WithTools<ContentItemTools>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapMcp("/mcp");

app.Run();
