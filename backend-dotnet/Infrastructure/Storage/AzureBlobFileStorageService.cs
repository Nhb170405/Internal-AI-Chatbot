using backend_dotnet.Modules.Documents;
using Microsoft.Extensions.Options;
using Azure.Storage.Blobs;
using backend_dotnet.Infrastructure.Errors;
using Azure;
using Azure.Storage.Sas;


namespace backend_dotnet.Infrastructure.Storage;

public sealed class AzureBlobFileStorageService : IFileStorageService
{
    private readonly AzureBlobStorageOptions _options;

    public AzureBlobFileStorageService(IOptions<AzureBlobStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<StoredFileResult> SaveAsync(IFormFile file, Guid documentId, string extension, CancellationToken cancellationToken = default)
    {
        // Bai tap sau khi cai Azure.Storage.Blobs:
        // 1. Validate _options.ConnectionString va _options.ContainerName khong rong.
        // 2. Tao storedFileName = documentId + extension lower-case.
        // 3. Tao blobName, vi du: $"documents/{documentId}/{storedFileName}".
        // 4. Tao BlobContainerClient tu connection string va container name.
        // 5. CreateIfNotExistsAsync cho container.
        // 6. Lay BlobClient theo blobName.
        // 7. Mo file.OpenReadStream() va upload len blob.
        // 8. Return StoredFileResult:
        //    - StorageProvider = FileStorageProvider.AzureBlob
        //    - StoredFileName = storedFileName
        //    - StorageKey = blobName
        //    - StoragePath = blob.Uri.ToString()

        if (file is null)
        {
            throw new ValidationApiException("invalid_file", "Missing file.");
        }

        if (file.Length <= 0)
        {
            throw new ValidationApiException("invalid_file", "File is empty.");
        }

        if (string.IsNullOrWhiteSpace(_options.ConnectionString) || string.IsNullOrWhiteSpace(_options.ContainerName))
        {
            throw new ExternalServiceApiException("azure_blob_config_missing", "Azure Blob storage configuration is missing.");
        }

        var container = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
        var normalizedExtension = extension.Trim().ToLowerInvariant();
        if (!normalizedExtension.StartsWith("."))
        {
            normalizedExtension = "." + normalizedExtension;
        }
        var storedFileName = documentId + normalizedExtension.ToLowerInvariant();
        var blobName = $"documents/{documentId}/{storedFileName}";

        try
        {
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blob = container.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();

            await blob.UploadAsync(stream, overwrite: false, cancellationToken);

            return new StoredFileResult
            {
                StorageProvider = FileStorageProvider.AzureBlob,
                StoredFileName = storedFileName,
                StorageKey = blobName,
                StoragePath = blob.Uri.ToString()
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            throw new ConflictApiException(
                "azure_blob_already_exists",
                "A file with the same storage key already exists.");
        }
        catch (RequestFailedException)
        {
            throw new ExternalServiceApiException(
                "azure_blob_upload_failed",
                "Failed to upload file to Azure Blob Storage.");
        }
    }

    public async Task<FileReadReference> GetReadReferenceAsync(Document document, CancellationToken cancellationToken = default)
    {
        // Bai tap sau khi cai Azure.Storage.Blobs:
        // 1. Doc document.StorageKey de tim blob.
        // 2. Tao BlobClient.
        // 3. Tao SAS URL read-only, het han sau _options.ReadSasMinutes phut.
        // 4. Return FileReadReference:
        //    - ReferenceType = FileReadReferenceType.SasUrl
        //    - Value = sasUrl
        //
        // Luu y:
        // - Khong luu SAS URL vao database vi no co han va la secret tam thoi.
        // - Chi tao SAS URL khi background job/Python can doc file.
        if (document is null)
        {
            throw new ValidationApiException("invalid_document", "Document is missing.");
        }
        if (string.IsNullOrWhiteSpace(document.StorageKey))
        {
            throw new ValidationApiException(
                "invalid_storage_key",
                "Document storage key is missing.");
        }
        if (string.IsNullOrWhiteSpace(_options.ConnectionString) ||
            string.IsNullOrWhiteSpace(_options.ContainerName))
        {
            throw new ExternalServiceApiException(
                "azure_blob_config_missing",
                "Azure Blob storage configuration is missing.");
        }
        var container = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
        var blob = container.GetBlobClient(document.StorageKey);

        try
        {
            var exists = await blob.ExistsAsync(cancellationToken);

            if (!exists.Value)
            {
                throw new NotFoundApiException(
                    "azure_blob_not_found",
                    "Document file was not found in Azure Blob Storage.");
            }

            if (!blob.CanGenerateSasUri)
            {
                throw new ExternalServiceApiException(
                    "azure_blob_sas_unavailable",
                    "Cannot generate Azure Blob SAS URL.");
            }

            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.ReadSasMinutes);

            var sasUri = blob.GenerateSasUri(
                BlobSasPermissions.Read,
                expiresAt);

            return new FileReadReference
            {
                ReferenceType = FileReadReferenceType.SasUrl,
                Value = sasUri.ToString()
            };
        }
        catch (ApiException)
        {
            throw;
        }
        catch (RequestFailedException)
        {
            throw new ExternalServiceApiException(
                "azure_blob_read_reference_failed",
                "Failed to create Azure Blob read reference.");
        }
    }

    public async Task DeleteIfExistsAsync(Document document, CancellationToken cancellationToken = default)
    {
        // Bai tap sau:
        // 1. Doc document.StorageKey.
        // 2. Tim blob tu BlobContainerClient.
        // 3. Goi DeleteIfExistsAsync.
        //
        // Ham nay se dung cho purge hard delete sau retention window.
        if (document is null)
        {
            throw new ValidationApiException("invalid_document", "Document is missing.");
        }
        if (string.IsNullOrWhiteSpace(document.StorageKey))
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(_options.ConnectionString) ||
            string.IsNullOrWhiteSpace(_options.ContainerName))
        {
            throw new ExternalServiceApiException(
                "azure_blob_config_missing",
                "Azure Blob storage configuration is missing.");
        }
        var container = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
        var blob = container.GetBlobClient(document.StorageKey);

        try
        {
            await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException)
        {
            throw new ExternalServiceApiException(
                "azure_blob_delete_failed",
                "Failed to delete file from Azure Blob Storage.");
        }
    }
}
