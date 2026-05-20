using Microsoft.EntityFrameworkCore;
using Pollon.Content.Api.Data;
using Pollon.Content.Api.Domain.Interfaces;
using Pollon.Publication.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Pollon.Content.Api.Infrastructure.Repositories;

public class ContentTemplateRepository(ApiDbContext dbContext) : IContentTemplateRepository
{
    public async Task<ContentTemplate?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await dbContext.ContentTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<ContentTemplate?> GetByFileNameAsync(string fileName, CancellationToken ct = default)
    {
        return await dbContext.ContentTemplates.FirstOrDefaultAsync(t => t.FileName == fileName, ct);
    }

    public async Task AddAsync(ContentTemplate template, CancellationToken ct = default)
    {
        await dbContext.ContentTemplates.AddAsync(template, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ContentTemplate template, CancellationToken ct = default)
    {
        dbContext.ContentTemplates.Update(template);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(ContentTemplate template, CancellationToken ct = default)
    {
        dbContext.ContentTemplates.Remove(template);
        await dbContext.SaveChangesAsync(ct);
    }
}
