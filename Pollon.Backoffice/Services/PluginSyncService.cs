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
                var consulServices = await client.Health.Service("pollon-plugin", tag: null, passingOnly: false, ct: stoppingToken);
                var activeConsulIds = consulServices.Response.Select(s => s.Service.ID).ToHashSet();

                using var session = _store.LightweightSession();

                // 2. Fetch all plugins from DB and delete those missing from Consul
                var dbPlugins = await session.Query<PluginInfo>().ToListAsync(stoppingToken);
                foreach (var dbPlugin in dbPlugins)
                {
                    if (!activeConsulIds.Contains(dbPlugin.ConsulServiceId))
                    {
                        _logger.LogInformation("Plugin {Id} (ConsulID: {ConsulId}) is gone from Consul. Removing from database.", dbPlugin.Id, dbPlugin.ConsulServiceId);
                        session.Delete(dbPlugin);
                    }
                }


                // 3. Update status for the ones still in Consul
                foreach (var entry in consulServices.Response)
                {
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

                await session.SaveChangesAsync(stoppingToken);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing plugin status from Consul.");
            }

            // Poll every 60 seconds
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
