using Wolverine;
using Microsoft.EntityFrameworkCore;
using Pollon.Content.Api.Data;
using Pollon.Contracts.Events;

namespace Pollon.Content.Api.Consumers;

public partial class ContentDeletedConsumer
{
    private readonly ApiDbContext _dbContext;
    private readonly ILogger<ContentDeletedConsumer> _logger;

    public ContentDeletedConsumer(ApiDbContext dbContext, ILogger<ContentDeletedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(ContentDeletedEvent message)
    {
        LogReceivedEvent(_logger, message.ContentItemId);

        var existingContent = await _dbContext.PublishedContents
            .FirstOrDefaultAsync(c => c.Id == message.ContentItemId);

        if (existingContent != null)
        {
            _dbContext.PublishedContents.Remove(existingContent);
            await _dbContext.SaveChangesAsync();
            LogDeletedSuccess(_logger, message.ContentItemId);
        }
    }
}
