namespace backend_dotnet.Modules.Documents;

public sealed class FileValidationOptions
{
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;

    // Danh sach extension duoc phep upload.
    // Luu y: extension nen viet lower-case va co dau cham o dau.
    public string[] AllowedExtensions { get; set; } =
    [
        ".txt",
        ".pdf",
        ".docx",
        ".csv",
        ".xlsx",
        ".xls"
    ];

    // Danh sach extension nguy hiem nen chan ro rang.
    // Danh sach nay giup code de doc hon: neu thay .exe/.bat thi biet la bi chan co chu dich.
    public string[] BlockedExtensions { get; set; } =
    [
        ".exe",
        ".bat",
        ".cmd",
        ".ps1",
        ".js",
        ".vbs",
        ".dll",
        ".msi"
    ];

    // Content-Type do client/browser gui len, khong duoc tin tuyet doi.
    // Minh van check nhu mot lop phong ve bo sung de chan cac request qua lech.
    public string[] AllowedContentTypes { get; set; } =
    [
        "text/plain",
        "text/csv",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/octet-stream"
    ];
}
