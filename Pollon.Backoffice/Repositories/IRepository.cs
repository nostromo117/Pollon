namespace Pollon.Backoffice.Repositories;

public interface IRepository<T>
{
    Task<IEnumerable<T>> GetAllAsync();
    IQueryable<T> Query();
    Task<T?> GetByIdAsync(string id);
    Task<bool> ExistsAsync(string id);
    Task CreateAsync(T item);
    Task UpdateAsync(string id, T item);
    Task DeleteAsync(string id);
}
