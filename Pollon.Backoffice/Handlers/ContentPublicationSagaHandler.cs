using Marten;
using Microsoft.Extensions.Logging;
using Pollon.Contracts.Events;
using Pollon.Contracts.Messages;
using Pollon.Contracts.Models;
using Pollon.Publication.Models;
using Wolverine;
using Pollon.Backoffice.Repositories;
using System.Text.Json;

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
        IRepository<ContentItem> itemRepo,
        IRepository<ContentType> typeRepo,
        IRepository<PluginInfo> pluginRepo,
        IMessageBus bus,
        ILogger<ContentPublicationSaga> logger)
    {
        LogStartingSaga(logger, command.Id, command.ContentType);

        this.Id = command.Id;
        this.ContentType = command.ContentType;

        // 1. Fetch data using repositories
        var item = await itemRepo.GetByIdAsync(command.Id);
        var contentType = await typeRepo.GetByIdAsync(command.ContentType);
        var targetPlugins = await pluginRepo.Query()
            .Where(x => x.Status == "Online" && x.EnabledContentTypes.Contains(command.ContentType))
            .ToListAsync();

        // 2. Optimized Payload Preparation: Direct mapping from schema (excluding Images)
        var contentJson = "{}";
        if (item != null && contentType != null)
        {
            var filteredData = new Dictionary<string, object>();
            foreach (var field in contentType.Fields)
            {
                if (field.FieldType != ContentFieldType.Image && item.Data.TryGetValue(field.Name, out var value))
                {
                    filteredData[field.Name] = value;
                }
            }
            contentJson = JsonSerializer.Serialize(filteredData);
        }

        var messages = new List<object>();

        if (targetPlugins.Any())
        {
            foreach (var plugin in targetPlugins)
            {
                this.PendingPlugins.Add(plugin.Id);
            }
            
            LogWaitingForPlugins(logger, targetPlugins.Count, string.Join(", ", this.PendingPlugins));

            // Request validation from all selected plugins with the filtered JSON payload
            messages.Add(new PluginValidationRequest(command.Id, contentJson));
            
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
