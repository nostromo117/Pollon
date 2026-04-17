using Microsoft.Extensions.Hosting;
using Pollon.Contracts.Events;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = Host.CreateApplicationBuilder(args);

// 1. Add Service Defaults (OTel, Health, etc.)
builder.AddServiceDefaults();

// 2. Add Consul Configuration (auto-config)
// This service will wait for Consul to provide its messaging configuration
builder.AddConsulConfiguration();

// 3. Configure Wolverine using the configuration loaded from Consul
builder.Services.AddWolverine(opts =>
{
    // The key "messaging" is mapped from the Consul key "pollon/messaging"
    var connectionString = builder.Configuration["messaging"];
    
    if (!string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine($"[Plugin] Configuring RabbitMQ from Consul: {connectionString}");
        opts.UseRabbitMq(new Uri(connectionString)).AutoProvision();
    }
    else
    {
        Console.WriteLine("[Plugin] RabbitMQ connection string not found in Consul yet.");
    }
    
    // Subscribe to all events published by the host applications
    opts.ListenToRabbitQueue("plugin-example-events");
});

var host = builder.Build();

Console.WriteLine("Pollon Plugin Example is running and participating in messaging...");

await host.RunAsync();

// Internal handler for the messages
public class ContentPublishedHandler
{
    public void Handle(ContentPublishedEvent message, ILogger<ContentPublishedHandler> logger)
    {
        logger.LogInformation(" [PLUGIN] RECEIVED: Content Published Event! Item ID: {Id}", message.ContentItemId);
        logger.LogInformation(" [PLUGIN] This plugin is now processing the published content autonomously.");
    }
    
    public void Handle(ContentUpdatedEvent message, ILogger<ContentPublishedHandler> logger)
    {
        logger.LogInformation(" [PLUGIN] RECEIVED: Content Updated Event! Item ID: {Id}", message.ContentItemId);
    }
}
