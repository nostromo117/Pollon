using Pollon.Publication.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Pollon.Content.Api.Domain.Interfaces;

public interface IContentTemplateRepository
{
    Task<ContentTemplate?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ContentTemplate?> GetByFileNameAsync(string fileName, CancellationToken ct = default);
    Task AddAsync(ContentTemplate template, CancellationToken ct = default);
    Task UpdateAsync(ContentTemplate template, CancellationToken ct = default);
    Task DeleteAsync(ContentTemplate template, CancellationToken ct = default);
}
