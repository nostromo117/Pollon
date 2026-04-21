using Marten;
using Microsoft.Extensions.Logging;
using Pollon.Contracts.Events;
using Pollon.Contracts.Messages;
using Pollon.Contracts.Models;
using Pollon.Publication.Models;
using Wolverine;

namespace Pollon.Backoffice.Handlers;

public partial class ContentPublicationSaga : Saga
{
    // State properties (persisted by Marten)
    public string? Id { get; set; }
    public HashSet<string> PendingPlugins { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? ContentType { get; set; }

    // 1. START the Saga
    public async Task<object[]> Start(
        StartContentPublication command, 
        IDocumentSession session,
        IMessageBus bus,
        ILogger<ContentPublicationSaga> logger)
    {
        LogStartingSaga(logger, command.Id, command.ContentType);

        this.Id = command.Id;
        this.ContentType = command.ContentType;

        // Find plugins that support this content type and are online
        var plugins = await session.Query<PluginInfo>()
            .Where(x => x.Status == "Online")
            .ToListAsync();
            
        var targetPlugins = plugins
            .Where(p => p.SupportedContentTypes.Contains(command.ContentType, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var messages = new List<object>();

        if (targetPlugins.Any())
        {
            foreach (var plugin in targetPlugins)
            {
                this.PendingPlugins.Add(plugin.Id);
            }
            
            LogWaitingForPlugins(logger, targetPlugins.Count, string.Join(", ", this.PendingPlugins));

            // Request validation from all selected plugins
            messages.Add(new PluginValidationRequest(command.Id));
            
            // Schedule timeout in 20 seconds using the bus
            await bus.ScheduleAsync(new PublicationTimeout(command.Id), TimeSpan.FromSeconds(20));
        }
        else
        {
            LogNoPluginsFound(logger, command.ContentType);
            messages.Add(new PluginValidationResponse(command.Id, "System", true));
        }

        return messages.ToArray();
    }

    // 2. Handle Plugin Responses
    public async Task<object[]> Handle(
        PluginValidationResponse response, 
        ILogger<ContentPublicationSaga> logger)
    {
        if (response.PluginId != "System")
        {
             LogReceivedResponse(logger, response.PluginId, response.Id, response.Success);
                
             this.PendingPlugins.Remove(response.PluginId);
             
             if (!response.Success || !string.IsNullOrEmpty(response.Warning))
             {
                 var msg = !string.IsNullOrEmpty(response.Warning) 
                    ? $"Plugin {response.PluginId}: {response.Warning}"
                    : $"Plugin {response.PluginId} validation failed.";
                 this.Warnings.Add(msg);
             }
        }

        if (this.PendingPlugins.Count == 0)
        {
            return await FinalizePublication(logger);
        }

        return Array.Empty<object>();
    }

    // 3. Handle Timeout
    public async Task<object[]> Handle(
        PublicationTimeout timeout, 
        ILogger<ContentPublicationSaga> logger)
    {
        if (this.PendingPlugins.Count > 0)
        {
            LogPublicationTimeout(logger, this.Id, this.PendingPlugins.Count, string.Join(", ", this.PendingPlugins));

            foreach (var pluginId in this.PendingPlugins)
            {
                this.Warnings.Add($"Plugin {pluginId} did not respond in time (Timeout).");
            }

            this.PendingPlugins.Clear();
            return await FinalizePublication(logger);
        }

        return Array.Empty<object>();
    }

    private async Task<object[]> FinalizePublication(ILogger logger)
    {
        LogFinalizingPublication(logger, this.Id, this.Warnings.Count);

        this.MarkCompleted();
        
        return new object[] { new PublicationCompleted(this.Id!, this.Warnings) };
    }
}

public record PublicationCompleted(string Id, List<string> Warnings);
