using backend_dotnet.Modules.Documents;

namespace backend_dotnet.Infrastructure.Storage;

public interface IFileStorageService
{
    Task<StoredFileResult> SaveAsync(IFormFile file, Guid documentId, string extension, CancellationToken cancellationToken = default);

    Task<FileReadReference> GetReadReferenceAsync(Document document, CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(Document document, CancellationToken cancellationToken = default);
}
