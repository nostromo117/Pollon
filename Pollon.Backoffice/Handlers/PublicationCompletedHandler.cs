using Marten;
using Microsoft.Extensions.Logging;
using Pollon.Contracts.Events;
using Pollon.Contracts.Messages;
using Pollon.Publication.Models;
using Wolverine;

namespace Pollon.Backoffice.Handlers;

public partial class PublicationCompletedHandler
{
    public async Task Handle(
        PublicationCompleted message, 
        IDocumentSession session,
        IMessageBus bus,
        ILogger<PublicationCompletedHandler> logger)
    {
        LogFinalizingInDb(logger, message.Id);

        var item = await session.LoadAsync<ContentItem>(message.Id);
        if (item == null)
        {
            LogItemNotFound(logger, message.Id);
            return;
        }

        item.Status = "Published";
        item.PublishedAt = DateTime.UtcNow;
        item.Warnings = message.Warnings;
        item.UpdatedAt = DateTime.UtcNow;

        session.Store(item);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new ContentPublishedEvent(item.Id));

        LogOfficiallyPublished(logger, item.Id, item.Warnings.Count);
    }
}
