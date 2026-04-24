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

    public IQueryable<T> Query()
    {
        return _session.Query<T>();
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        return await _session.LoadAsync<T>(id);
    }

    // Checks for existence without loading the document. 
    // We use Expressions because T doesn't have a common interface with an Id property, 
    // but Marten uses 'Id' as the primary key by convention.
    public async Task<bool> ExistsAsync(string id)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
        var property = System.Linq.Expressions.Expression.Property(parameter, "Id");
        var constant = System.Linq.Expressions.Expression.Constant(id);
        var equal = System.Linq.Expressions.Expression.Equal(property, constant);
        var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(equal, parameter);

        return await _session.Query<T>().AnyAsync(lambda);
    }

    public async Task CreateAsync(T item)
    {
        _session.Store(item);
        await _session.SaveChangesAsync();
    }

    public async Task UpdateAsync(string id, T item)
    {
        _session.Store(item);
        await _session.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        _session.Delete<T>(id);
        await _session.SaveChangesAsync();
    }
}
