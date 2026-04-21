using Consul;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Pollon.Contracts.Messages;
using Wolverine;

namespace Pollon.Plugin.Example.Services;

public class PluginRegistrationService : BackgroundService
{
    private readonly KeycloakTokenClient _tokenClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PluginRegistrationService> _logger;
    private readonly string _pluginId;
    private readonly string _pluginName;
    private string? _consulServiceId;

    public PluginRegistrationService(
        KeycloakTokenClient tokenClient,
        IServiceScopeFactory scopeFactory, 
        IConfiguration configuration, 
        ILogger<PluginRegistrationService> logger)
    {
        _tokenClient = tokenClient;
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
        var selfUrl = _configuration["Plugin:SelfUrl"] ?? _configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault() ?? "http://localhost:5500";
        
        // If we are running in a container-orchestrated environment (like Aspire with some containers), 
        // and this service is on the host, containers (like Consul) need to use host.docker.internal.
        var healthCheckUrl = selfUrl.Replace("localhost", "host.docker.internal").Replace("127.0.0.1", "host.docker.internal");

        _logger.LogInformation("Registering with Consul at {ConsulAddr}. Self URL: {SelfUrl}", consulAddr, selfUrl);

        using var client = new ConsulClient(cfg => cfg.Address = new Uri(consulAddr));

        _consulServiceId = $"{_pluginId}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        var uri = new Uri(selfUrl);

        var registration = new AgentServiceRegistration()
        {
            ID = _consulServiceId,
            Name = "pollon-plugin",
            Address = "host.docker.internal",
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
            // Otteniamo il Token JWT prima di procedere
            _logger.LogInformation("Requesting JWT from Keycloak...");
            var token = await _tokenClient.GetTokenAsync();
            _logger.LogInformation("JWT obtained successfully.");

            await client.Agent.ServiceRegister(registration, stoppingToken);
            _logger.LogInformation("Successfully registered in Consul with ID: {Id}", _consulServiceId);

            // 2. Send Registration Message via Wolverine and wait for feedback
            using (var scope = _scopeFactory.CreateScope())
            {
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                var response = await bus.InvokeAsync<RegisterPluginResponse>(new RegisterPlugin(
                    _pluginId, 
                    _pluginName, 
                    _consulServiceId, 
                    $"{healthCheckUrl}/health",
                    token)); // Passiamo il token JWT

                if (response.Success)
                {
                    _logger.LogInformation("Successfully registered in Backoffice! Dashboard should show the plugin now.");
                }
                else
                {
                    _logger.LogError("Backoffice REJECTED registration: {Message}", response.ErrorMessage);
                    // In real world we might want to shut down or retry
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register plugin. Security check failed or host unreachable.");
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
                await client.Agent.ServiceDeregister(_consulServiceId);
            }
            catch(Exception ex)
            {
                _logger.LogWarning("Failed to deregister from Consul: {Message}", ex.Message);
            }
        }
    }
}


