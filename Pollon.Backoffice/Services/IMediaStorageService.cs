using Pollon.Publication.Models;

namespace Pollon.Backoffice.Services;

public interface IMediaStorageService
{
    /// <summary>
    /// Saves a file stream and returns the persisted MediaAsset model containing the access URL.
    /// </summary>
    Task<MediaAsset> SaveFileAsync(string fileName, Stream content, string mimeType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves multiple file streams and returns the persisted MediaAsset models.
    /// </summary>
    Task<List<MediaAsset>> SaveFilesAsync(IEnumerable<(string fileName, Stream content, string mimeType)> files, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a MediaAsset model with its binary data by ID.
    /// </summary>
    Task<MediaAsset?> GetFileAsync(string id, CancellationToken cancellationToken = default);
}
