using Microsoft.Extensions.Logging;
using Pollon.Contracts.Messages;
using Wolverine;

namespace Pollon.Plugin.Example.Handlers;

public partial class SagaParticipantHandler
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SagaParticipantHandler> _logger;
    private readonly string _pluginId;

    public SagaParticipantHandler(IConfiguration configuration, ILogger<SagaParticipantHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _pluginId = _configuration["Plugin:Id"] ?? "plugin-example-01";
    }

    public async Task<PluginValidationResponse> Handle(PluginValidationRequest request)
    {
        LogReceivedValidationRequest(_logger, request.Id);
        _logger.LogInformation("Plugin received JSON payload: {Json}", request.ContentJson);

        // Simulate some processing (e.g. SEO check, image analysis, etc.)
        await Task.Delay(2000);

        bool isSuccess = true;
        string? warning = null;

        // Example: logic to add a warning for certain items
        if (request.Id.Contains("42"))
        {
            warning = "This content contains life, the universe, and everything. Use with caution.";
        }

        LogSendingValidationResult(_logger, request.Id, isSuccess);

        return new PluginValidationResponse(request.Id, _pluginId, isSuccess, warning);
    }
}
