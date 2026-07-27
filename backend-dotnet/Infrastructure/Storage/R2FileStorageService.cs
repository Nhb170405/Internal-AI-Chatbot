using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Modules.Documents;

namespace backend_dotnet.Infrastructure.Storage;

public sealed class R2FileStorageService : IFileStorageService
{
    private readonly R2ObjectStorageService _storage;

    public R2FileStorageService(R2ObjectStorageService storage)
    {
        _storage = storage;
    }

    public async Task<StoredFileResult> SaveAsync(
        IFormFile file,
        Guid documentId,
        string extension,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0)
        {
            throw new ValidationApiException("invalid_file", "Missing or empty file.");
        }

        var normalizedExtension = extension.Trim().ToLowerInvariant();
        if (!normalizedExtension.StartsWith('.'))
        {
            normalizedExtension = "." + normalizedExtension;
        }

        var storedFileName = $"{documentId}{normalizedExtension}";
        var key = $"documents/{documentId}/{storedFileName}";

        await using var stream = file.OpenReadStream();
        await _storage.PutAsync(
            key,
            stream,
            string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            cancellationToken);

        return new StoredFileResult
        {
            StorageProvider = FileStorageProvider.R2,
            StoredFileName = storedFileName,
            StorageKey = key,
            StoragePath = $"r2://{key}"
        };
    }

    public async Task<FileReadReference> GetReadReferenceAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        if (document is null || string.IsNullOrWhiteSpace(document.StorageKey))
        {
            throw new ValidationApiException(
                "invalid_storage_key",
                "Document storage key is missing.");
        }

        if (!await _storage.ExistsAsync(document.StorageKey, cancellationToken))
        {
            throw new NotFoundApiException(
                "r2_object_not_found",
                "Document file was not found in Cloudflare R2.");
        }

        return new FileReadReference
        {
            ReferenceType = FileReadReferenceType.PresignedUrl,
            Value = _storage.CreatePresignedReadUrl(document.StorageKey)
        };
    }

    public Task DeleteIfExistsAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        if (document is null || string.IsNullOrWhiteSpace(document.StorageKey))
        {
            return Task.CompletedTask;
        }

        return _storage.DeleteIfExistsAsync(document.StorageKey, cancellationToken);
    }
}
