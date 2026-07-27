namespace backend_dotnet.Infrastructure.Storage;

public sealed class R2StorageOptions
{
    public string ServiceUrl { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public string AccessKeyId { get; set; } = string.Empty;

    public string SecretAccessKey { get; set; } = string.Empty;

    public int PresignedUrlMinutes { get; set; } = 15;
}
