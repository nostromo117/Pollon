using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Text;

namespace Pollon.Content.Api.Services;

public partial class MinioStaticStorage : IStaticStorage
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioStaticStorage> _logger;
    private const string BucketName = "pollon-static";

    public MinioStaticStorage(IMinioClient minioClient, ILogger<MinioStaticStorage> logger)
    {
        _minioClient = minioClient;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        LogInitializing(_logger);
        await EnsureBucketExistsAsync();
    }

    public async Task SaveFileAsync(string path, string content, string contentType)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            using var stream = new MemoryStream(bytes);

            LogUploadingFile(_logger, BucketName, path);

            var putArgs = new PutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(path)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putArgs);
            
            LogUploadSuccess(_logger, path);
        }
        catch (Exception ex)
        {
            LogUploadError(_logger, ex, path);
            throw;
        }
    }

    public async Task DeleteFileAsync(string path)
    {
        try
        {
            LogRemovingFile(_logger, BucketName, path);

            var removeArgs = new RemoveObjectArgs()
                .WithBucket(BucketName)
                .WithObject(path);

            await _minioClient.RemoveObjectAsync(removeArgs);
            
            LogRemoveSuccess(_logger, path);
        }
        catch (Exception ex)
        {
            LogRemoveError(_logger, ex, path);
            throw;
        }
    }

    private async Task EnsureBucketExistsAsync()
    {
        var found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(BucketName));
        if (!found)
        {
            LogCreatingBucket(_logger, BucketName);
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(BucketName));
        }

        // Always ensure the public read policy is set, especially important in dev/multi-run scenarios
        await SetupPublicReadPolicyAsync();
    }

    private async Task SetupPublicReadPolicyAsync()
    {
        var policyJson = $@"
{{
    ""Version"": ""2012-10-17"",
    ""Statement"": [
        {{
            ""Sid"": ""PublicRead"",
            ""Effect"": ""Allow"",
            ""Principal"": {{ ""AWS"": [""*""] }},
            ""Action"": [""s3:GetObject""],
            ""Resource"": [""arn:aws:s3:::{BucketName}/*""]
        }}
    ]
}}";
        await _minioClient.SetPolicyAsync(new SetPolicyArgs()
            .WithBucket(BucketName)
            .WithPolicy(policyJson));
        
        LogBucketPolicySet(_logger, BucketName);
    }
}
