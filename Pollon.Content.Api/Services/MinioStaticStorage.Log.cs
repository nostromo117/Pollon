using Microsoft.Extensions.Logging;

namespace Pollon.Content.Api.Services;

public partial class MinioStaticStorage
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Initializing Minio Static Storage...")]
    static partial void LogInitializing(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploading file to MinIO: {Bucket}/{Path}")]
    static partial void LogUploadingFile(ILogger logger, string bucket, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully uploaded {Path} to MinIO.")]
    static partial void LogUploadSuccess(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error uploading file {Path} to MinIO")]
    static partial void LogUploadError(ILogger logger, Exception ex, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating bucket {BucketName} in MinIO")]
    static partial void LogCreatingBucket(ILogger logger, string bucketName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Bucket {BucketName} created with public read policy.")]
    static partial void LogBucketPolicySet(ILogger logger, string bucketName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Removing file from MinIO: {Bucket}/{Path}")]
    static partial void LogRemovingFile(ILogger logger, string bucket, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully removed {Path} from MinIO.")]
    static partial void LogRemoveSuccess(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error removing file {Path} from MinIO")]
    static partial void LogRemoveError(ILogger logger, Exception ex, string path);
}
