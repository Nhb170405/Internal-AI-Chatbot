namespace backend_dotnet.Infrastructure.Storage;

public sealed class LocalFileStorageOptions
{
    // Thu muc goc de luu file upload trong moi truong local/dev.
    // Nen cau hinh trong appsettings.Development.json sau, vi du:
    // "LocalFileStorage": { "RootPath": "storage/uploads" }
    public string RootPath { get; set; } = "storage/uploads";

    // Gioi han file upload trong Milestone 4. Gia tri mac dinh: 20 MB.
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;

    // Danh sach extension cho phep. Nen dung lower-case khi so sanh.
    public string[] AllowedExtensions { get; set; } = [".pdf", ".docx", ".xlsx", ".csv", ".txt"];
}
