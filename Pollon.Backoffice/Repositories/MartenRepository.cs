using Marten;

namespace Pollon.Backoffice.Repositories;

public class MartenRepository<T> : IRepository<T> where T : class
{
    private readonly IDocumentSession _session;

    public MartenRepository(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _session.Query<T>().ToListAsync();
    }

    public async Task<T> GetByIdAsync(string id)
    {
        return await _session.LoadAsync<T>(id);
    }

    public async Task CreateAsync(T item)
    {
        _session.Store(item);
        await _session.SaveChangesAsync();
    }

    public async Task UpdateAsync(string id, T item)
    {
        var existing = await _session.LoadAsync<T>(id);
        if (existing != null)
        {
            _session.Store(item);
            await _session.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(string id)
    {
        _session.Delete<T>(id);
        await _session.SaveChangesAsync();
    }
}
