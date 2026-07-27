using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using backend_dotnet.Infrastructure.Errors;
using Microsoft.Extensions.Options;

namespace backend_dotnet.Infrastructure.Storage;

public sealed class R2ObjectStorageService : IDisposable
{
    private readonly R2StorageOptions _options;
    private readonly AmazonS3Client _client;

    public R2ObjectStorageService(IOptions<R2StorageOptions> options)
    {
        _options = options.Value;
        ValidateOptions(_options);

        var credentials = new BasicAWSCredentials(
            _options.AccessKeyId,
            _options.SecretAccessKey);

        _client = new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl,
            ForcePathStyle = true
        });

        Amazon.AWSConfigsS3.UseSignatureVersion4 = true;
    }

    public async Task PutAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false,
                // R2 does not support the streaming SigV4/checksum behavior
                // enabled by recent AWSSDK.S3 versions.
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true
            }, cancellationToken);
        }
        catch (AmazonS3Exception)
        {
            throw new ExternalServiceApiException(
                "r2_upload_failed",
                "Failed to upload object to Cloudflare R2.");
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(_options.BucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (AmazonS3Exception)
        {
            throw new ExternalServiceApiException(
                "r2_metadata_failed",
                "Failed to read object metadata from Cloudflare R2.");
        }
    }

    public string CreatePresignedReadUrl(string key)
    {
        if (_options.PresignedUrlMinutes <= 0)
        {
            throw new ExternalServiceApiException(
                "r2_config_invalid",
                "R2 presigned URL lifetime must be greater than zero.");
        }

        return _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_options.PresignedUrlMinutes)
        });
    }

    public async Task DeleteIfExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.DeleteObjectAsync(_options.BucketName, key, cancellationToken);
        }
        catch (AmazonS3Exception)
        {
            throw new ExternalServiceApiException(
                "r2_delete_failed",
                "Failed to delete object from Cloudflare R2.");
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private static void ValidateOptions(R2StorageOptions options)
    {
        if (!Uri.TryCreate(options.ServiceUrl, UriKind.Absolute, out var serviceUri) ||
            serviceUri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(options.BucketName) ||
            string.IsNullOrWhiteSpace(options.AccessKeyId) ||
            string.IsNullOrWhiteSpace(options.SecretAccessKey))
        {
            throw new ExternalServiceApiException(
                "r2_config_missing",
                "Cloudflare R2 storage configuration is missing or invalid.");
        }
    }
}
