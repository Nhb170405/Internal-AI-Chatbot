namespace backend_dotnet.Infrastructure.Storage;

public sealed class FileReadReference
{
    // Cach doc file:
    // - "local_path": Python co the doc truc tiep tu duong dan local khi chay cung may dev.
    // - "presigned_url": Python can download a file through a short-lived R2/S3 URL.
    public string ReferenceType { get; init; } = string.Empty;

    // Gia tri de dua sang Python:
    // - Neu ReferenceType = "local_path" thi Value la full path.
    // - Neu ReferenceType = "presigned_url" thi Value la URL co han doc tam thoi.
    public string Value { get; init; } = string.Empty;
}
