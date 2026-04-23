using Marten;
using Microsoft.Extensions.Logging;
using Pollon.Contracts.Events;
using Pollon.Contracts.Messages;
using Pollon.Publication.Models;
using Wolverine;
using Pollon.Backoffice.Repositories;

namespace Pollon.Backoffice.Handlers;

public partial class PublicationCompletedHandler
{
    public async Task Handle(
        PublicationCompleted message, 
        IRepository<ContentItem> repository,
        IMessageBus bus,
        ILogger<PublicationCompletedHandler> logger)
    {
        LogFinalizingInDb(logger, message.Id);

        var item = await repository.GetByIdAsync(message.Id);
        if (item == null)
        {
            LogItemNotFound(logger, message.Id);
            return;
        }

        item.Status = "Published";
        item.PublishedAt = DateTime.UtcNow;
        item.Warnings = message.Warnings;
        item.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(item.Id, item);

        await bus.PublishAsync(new ContentPublishedEvent(item.Id));

        LogOfficiallyPublished(logger, item.Id, item.Warnings.Count);
    }
}
