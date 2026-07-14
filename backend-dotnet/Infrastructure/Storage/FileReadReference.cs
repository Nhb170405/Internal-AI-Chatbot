namespace backend_dotnet.Infrastructure.Storage;

public sealed class FileReadReference
{
    // Cach doc file:
    // - "local_path": Python co the doc truc tiep tu duong dan local khi chay cung may dev.
    // - "sas_url": Python can download file bang URL ngan han tren Azure Blob.
    public string ReferenceType { get; init; } = string.Empty;

    // Gia tri de dua sang Python:
    // - Neu ReferenceType = "local_path" thi Value la full path.
    // - Neu ReferenceType = "sas_url" thi Value la URL co han doc tam thoi.
    public string Value { get; init; } = string.Empty;
}
