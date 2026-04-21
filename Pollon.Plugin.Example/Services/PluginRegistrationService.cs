using Consul;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Pollon.Contracts.Messages;
using Wolverine;

namespace Pollon.Plugin.Example.Services;

public partial class PluginRegistrationService : BackgroundService
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

        LogRegisteringWithConsul(_logger, consulAddr, selfUrl);

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
            LogRequestingToken(_logger);
            var token = await _tokenClient.GetTokenAsync();
            LogTokenObtained(_logger);

            await client.Agent.ServiceRegister(registration, stoppingToken);
            LogConsulRegistrationSuccess(_logger, _consulServiceId);

            // 2. Send Registration Message via Wolverine and wait for feedback
            using (var scope = _scopeFactory.CreateScope())
            {
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                var response = await bus.InvokeAsync<RegisterPluginResponse>(new RegisterPlugin(
                    _pluginId, 
                    _pluginName, 
                    _consulServiceId, 
                    $"{healthCheckUrl}/health",
                    token,
                    SupportedContentTypes: new List<string> { "blog-post", "page" })); // Supportiamo questi tipi

                if (response.Success)
                {
                    LogBackofficeRegistrationSuccess(_logger);
                }
                else
                {
                    LogBackofficeRegistrationRejected(_logger, response.ErrorMessage);
                    // In real world we might want to shut down or retry
                }
            }
        }
        catch (Exception ex)
        {
            LogRegistrationError(_logger, ex);
        }

        // 3. Keep the service alive
        try 
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            LogServiceStopping(_logger);
        }

        // Cleanup on shutdown
        if (_consulServiceId != null)
        {
            LogDeregisteringConsul(_logger);
            try 
            {
                await client.Agent.ServiceDeregister(_consulServiceId);
            }
            catch(Exception ex)
            {
                LogDeregistrationWarning(_logger, ex.Message);
            }
        }
    }
}


