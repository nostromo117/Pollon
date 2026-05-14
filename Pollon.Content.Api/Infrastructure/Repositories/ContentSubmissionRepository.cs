using Pollon.Content.Api.Data;
using Pollon.Content.Api.Domain.Interfaces;
using Pollon.Publication.Models;

namespace Pollon.Content.Api.Infrastructure.Repositories;

public class ContentSubmissionRepository(ApiDbContext dbContext) : IContentSubmissionRepository
{
    public async Task AddAsync(ContentSubmission submission, CancellationToken ct = default)
    {
        dbContext.ContentSubmissions.Add(submission);
        await dbContext.SaveChangesAsync(ct);
    }
}
