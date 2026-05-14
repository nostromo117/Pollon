using Pollon.Publication.Models;

namespace Pollon.Content.Api.Domain.Interfaces;

public interface IContentSubmissionRepository
{
    Task AddAsync(ContentSubmission submission, CancellationToken ct = default);
}
