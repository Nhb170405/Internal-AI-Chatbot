namespace backend_dotnet.Infrastructure.Storage;

public sealed class StoredFileResult
{
    // Ten file do backend tu tao, vi du: "{documentId}.pdf".
    // Khong dung ten file user upload de lam stored file name.
    public string StoredFileName { get; init; } = string.Empty;

    // Noi file duoc luu: "local" hoac "azure_blob".
    // Sau nay dung field nay de debug va de biet storage implementation nao da tao file.
    public string StorageProvider { get; init; } = string.Empty;

    // Dinh danh on dinh cua file trong storage.
    // Local: co the la relative path hoac stored file name.
    // Azure Blob: nen la blob name, vi du "documents/{documentId}/{documentId}.pdf".
    public string StorageKey { get; init; } = string.Empty;

    // Thong tin tham khao noi bo.
    // Local: full physical path.
    // Azure: blob uri khong kem SAS token.
    // Khong expose tuy tien field nay ra frontend.
    public string StoragePath { get; init; } = string.Empty;
}
