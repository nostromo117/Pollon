using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Pollon.Contracts.Events;
using Pollon.Plugin.Example.Services;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Service Defaults (OTel, Health, etc.)
builder.AddServiceDefaults();

// 2. Add Consul Configuration (auto-config)
builder.AddConsulConfiguration();

// 3. Configure Wolverine using the configuration loaded from Consul
builder.Host.UseWolverine(opts =>
{
    var connectionString = builder.Configuration["messaging"];
    
    if (!string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine($"[Plugin] Configuring RabbitMQ from Consul: {connectionString}");
        var rabbit = opts.UseRabbitMq(new Uri(connectionString)).AutoProvision()
            .UseConventionalRouting();

        // Targeted validation queue using Routing Key
        var pluginId = builder.Configuration["Plugin:Id"] ?? "plugin-example-01";
        opts.ListenToRabbitQueue($"validation-{pluginId}");
    }
    
    opts.ListenToRabbitQueue("plugin-example-events");
});


// 4. Register Plugin Registration & Heartbeat Service
builder.Services.AddHttpClient<KeycloakTokenClient>();
builder.Services.AddHostedService<PluginRegistrationService>();

var app = builder.Build();

// Expose health checks for Consul
app.MapHealthChecks("/health");

Console.WriteLine("Pollon Plugin Example is running and participating in messaging...");

await app.RunAsync();

// Internal handler for the messages
public class ContentPublishedHandler
{
    public void Handle(ContentPublishedEvent message, ILogger<ContentPublishedHandler> logger)
    {
        logger.LogInformation(" [PLUGIN] RECEIVED: Content Published Event! Item ID: {Id}", message.ContentItemId);
    }
    
    public void Handle(ContentUpdatedEvent message, ILogger<ContentPublishedHandler> logger)
    {
        logger.LogInformation(" [PLUGIN] RECEIVED: Content Updated Event! Item ID: {Id}", message.ContentItemId);
    }
}

