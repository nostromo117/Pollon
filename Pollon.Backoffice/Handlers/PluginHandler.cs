using Marten;
using Microsoft.Extensions.Logging;
using Pollon.Contracts.Models;
using Pollon.Contracts.Messages;

namespace Pollon.Backoffice.Handlers;

public partial class PluginHandler
{
    public async Task Handle(RegisterPlugin message, IDocumentSession session, ILogger<PluginHandler> logger)
    {
        LogRegisteringPlugin(logger, message.Name, message.Id, message.ConsulServiceId);

        var plugin = await session.LoadAsync<PluginInfo>(message.Id) ?? new PluginInfo { Id = message.Id };
        
        plugin.Name = message.Name;
        plugin.ConsulServiceId = message.ConsulServiceId;
        plugin.HeartbeatUrl = message.HeartbeatUrl;
        plugin.Version = message.Version;
        plugin.Description = message.Description;
        plugin.LastSeen = DateTime.UtcNow;

        session.Store(plugin);
        await session.SaveChangesAsync();
    }
}

