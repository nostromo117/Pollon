using Pollon.Backoffice.Models;
using Pollon.Backoffice.Repositories;
using Pollon.Backoffice.Services;

namespace Pollon.Media.Api.Services;

public class DatabaseMediaStorageService(IRepository<MediaAsset> repository) : IMediaStorageService
{
    public async Task<MediaAsset> SaveFileAsync(string fileName, Stream content, string mimeType, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        var asset = new MediaAsset
        {
            Id = Guid.NewGuid().ToString(),
            FileName = fileName,
            MimeType = mimeType,
            SizeInBytes = bytes.Length,
            Data = bytes
            // Url will be assigned by the endpoint or dynamically.
        };

        asset.Url = $"/api/media/{asset.Id}";

        await repository.CreateAsync(asset);
        return asset;
    }

    public async Task<MediaAsset?> GetFileAsync(string id, CancellationToken cancellationToken = default)
    {
        return await repository.GetByIdAsync(id);
    }
}
