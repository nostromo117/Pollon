using Pollon.Backoffice.Models;

namespace Pollon.Backoffice.Services;

public interface IMediaStorageService
{
    /// <summary>
    /// Saves a file stream and returns the persisted MediaAsset model containing the access URL.
    /// </summary>
    Task<MediaAsset> SaveFileAsync(string fileName, Stream content, string mimeType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a MediaAsset model with its binary data by ID.
    /// </summary>
    Task<MediaAsset?> GetFileAsync(string id, CancellationToken cancellationToken = default);
}
