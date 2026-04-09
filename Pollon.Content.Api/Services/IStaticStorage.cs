namespace Pollon.Content.Api.Services;

public interface IStaticStorage
{
    Task InitializeAsync();
    Task SaveFileAsync(string path, string content, string contentType);
    Task DeleteFileAsync(string path);
}
