namespace Pollon.Backoffice.Repositories;

public interface IRepository<T>
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> GetByIdAsync(string id);
    Task CreateAsync(T item);
    Task UpdateAsync(string id, T item);
    Task DeleteAsync(string id);
}
