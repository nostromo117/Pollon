using Consul;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Pollon.Contracts.Messages;
using Wolverine;

namespace Pollon.Plugin.Example.Services;

public class PluginRegistrationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PluginRegistrationService> _logger;
    private readonly string _pluginId;
    private readonly string _pluginName;
    private string? _consulServiceId;

    public PluginRegistrationService(
        IServiceScopeFactory scopeFactory, 
        IConfiguration configuration, 
        ILogger<PluginRegistrationService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        
        _pluginId = _configuration["Plugin:Id"] ?? "plugin-example-01";
        _pluginName = _configuration["Plugin:Name"] ?? "Example Plugin";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. Wait for the server to start to get the actual address
        await Task.Delay(5000, stoppingToken);

        var consulAddr = _configuration["CONSUL_URL"] ?? _configuration["CONSUL_HTTP_ADDR"] ?? "http://localhost:8500";
        var selfUrl = _configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault() ?? "http://localhost:5500";
        
        // Se stiamo lanciando standalone (fuori da Aspire), ma Consul è in Docker, 
        // localhost non funzionerà per l'health check. Usiamo l'IP dell'host.
        var healthCheckUrl = selfUrl.Replace("localhost", "host.docker.internal").Replace("127.0.0.1", "host.docker.internal");

        _logger.LogInformation("Registering with Consul at {ConsulAddr}. Self URL: {SelfUrl}, HealthCheck: {HC}", consulAddr, selfUrl, healthCheckUrl);

        using var client = new ConsulClient(cfg => cfg.Address = new Uri(consulAddr));

        _consulServiceId = $"{_pluginId}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        var uri = new Uri(selfUrl);

        var registration = new AgentServiceRegistration()
        {
            ID = _consulServiceId,
            Name = "pollon-plugin",
            Address = "host.docker.internal", // Comunica a Consul di cercare sull'host
            Port = uri.Port,
            Tags = new[] { "plugin", _pluginId },
            Check = new AgentServiceCheck()
            {
                HTTP = $"{healthCheckUrl}/health",
                Interval = TimeSpan.FromSeconds(10),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
            }
        };

        try
        {
            await client.Agent.ServiceRegister(registration, stoppingToken);
            _logger.LogInformation("Successfully registered in Consul with ID: {Id}", _consulServiceId);

            // 2. Send Registration Message via Wolverine
            using (var scope = _scopeFactory.CreateScope())
            {
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                await bus.SendAsync(new RegisterPlugin(
                    _pluginId, 
                    _pluginName, 
                    _consulServiceId, 
                    $"{healthCheckUrl}/health"));
            }

            _logger.LogInformation("Sent RegisterPlugin message to Host.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register plugin in Consul or send registration message.");
        }

        // 3. Keep the service alive
        try 
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Plugin service is stopping...");
        }

        // Cleanup on shutdown
        if (_consulServiceId != null)
        {
            _logger.LogInformation("Deregistering from Consul...");
            try 
            {
                // Qui non usiamo stoppingToken perché è già cancellato
                await client.Agent.ServiceDeregister(_consulServiceId);
            }
            catch(Exception ex)
            {
                _logger.LogWarning("Failed to deregister from Consul: {Message}", ex.Message);
            }
        }
    }
}


