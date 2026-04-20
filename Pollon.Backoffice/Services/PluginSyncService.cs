using Consul;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pollon.Contracts.Models;

namespace Pollon.Backoffice.Services;

public class PluginSyncService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IDocumentStore _store;
    private readonly ILogger<PluginSyncService> _logger;

    public PluginSyncService(
        IConfiguration configuration,
        IDocumentStore store,
        ILogger<PluginSyncService> logger)
    {
        _configuration = configuration;
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consulAddr = _configuration["CONSUL_URL"] ?? "http://localhost:8500";
        // We use a single client for the lifetime of the service
        using var client = new ConsulClient(cfg => cfg.Address = new Uri(consulAddr));

        _logger.LogInformation("PluginSyncService started. Polling Consul at {Addr}", consulAddr);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1. Fetch current status from Consul
                // "pollon-plugin" is the name used by all our plugins
                var services = await client.Health.Service("pollon-plugin", tag: null, passingOnly: false, ct: stoppingToken);

                using var session = _store.LightweightSession();

                // 2. Update status in Marten
                foreach (var entry in services.Response)
                {
                    // The Plugin ID is stored in the tags (excluding the general "plugin" tag)
                    var pluginId = entry.Service.Tags.FirstOrDefault(t => t != "plugin");
                    if (string.IsNullOrEmpty(pluginId)) continue;

                    var isHealthy = entry.Checks.All(c => c.Status == HealthStatus.Passing);
                    var statusString = isHealthy ? "Online" : "Warning";
                    if (entry.Checks.Any(c => c.Status == HealthStatus.Critical)) statusString = "Offline";

                    var plugin = await session.LoadAsync<PluginInfo>(pluginId);
                    if (plugin != null)
                    {
                        plugin.LastSeen = DateTime.UtcNow;
                        plugin.Status = statusString;
                        session.Store(plugin);
                    }
                }

                // 3. Optional: Handle plugins that are in DB but completely gone from Consul
                // (This could be a separate cleanup task or done here by comparing IDs)
                
                await session.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing plugin status from Consul.");
            }

            // Poll every 10 seconds
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
