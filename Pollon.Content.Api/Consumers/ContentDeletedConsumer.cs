using Wolverine;
using Microsoft.EntityFrameworkCore;
using Pollon.Content.Api.Data;
using Pollon.Contracts.Events;
using Pollon.Content.Api.Services;

namespace Pollon.Content.Api.Consumers;

public partial class ContentDeletedConsumer
{
    private readonly ApiDbContext _dbContext;
    private readonly IStaticStorage _staticStorage;
    private readonly ILogger<ContentDeletedConsumer> _logger;

    public ContentDeletedConsumer(ApiDbContext dbContext, IStaticStorage staticStorage, ILogger<ContentDeletedConsumer> logger)
    {
        _dbContext = dbContext;
        _staticStorage = staticStorage;
        _logger = logger;
    }

    public async Task Handle(ContentDeletedEvent message)
    {
        LogReceivedEvent(_logger, message.ContentItemId);

        var existingContent = await _dbContext.PublishedContents
            .FirstOrDefaultAsync(c => c.Id == message.ContentItemId);

        if (existingContent != null)
        {
            // Atomicity Guarantee: If it was published statically, attempt MinIO deletion FIRST.
            if (existingContent.PublishMode == "Static" || existingContent.PublishMode == "Both")
            {
                var fileName = string.IsNullOrWhiteSpace(existingContent.Slug)
                               ? $"{existingContent.Id}.html"
                               : $"{existingContent.Slug}.html";

                try
                {
                    await _staticStorage.DeleteFileAsync(fileName);
                    LogDeletedStaticFile(_logger, fileName);
                }
                catch (Exception ex)
                {
                    LogDeleteStaticFileError(_logger, ex, fileName);
                    // Crucial: Bubble up exception! Wolverine will consider message failed, 
                    // preventing SaveChangesAsync() and ensuring PostgreSQL delete rolls back.
                    throw;
                }
            }

            _dbContext.PublishedContents.Remove(existingContent);
            await _dbContext.SaveChangesAsync();
            LogDeletedSuccess(_logger, message.ContentItemId);
        }
    }
}
