using Wolverine;
using Microsoft.EntityFrameworkCore;
using Pollon.Content.Api.Data;
using Pollon.Contracts.Events;

namespace Pollon.Content.Api.Consumers;

public class ContentDeletedConsumer
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
        _logger.LogInformation("Received ContentDeletedEvent for ContentItemId {ContentItemId}", message.ContentItemId);

        var existingContent = await _dbContext.PublishedContents
            .FirstOrDefaultAsync(c => c.Id == message.ContentItemId);

        if (existingContent != null)
        {
            _dbContext.PublishedContents.Remove(existingContent);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Deleted ContentItemId {ContentItemId} from SQL Server", message.ContentItemId);
        }
    }
}
