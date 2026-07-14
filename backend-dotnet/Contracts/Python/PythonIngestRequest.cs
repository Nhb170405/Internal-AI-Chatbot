namespace backend_dotnet.Contracts.Python;

public sealed class PythonIngestRequest
{
    public Guid DocumentId { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FileReferenceType { get; set; } = "local_path";

    public string FileReferenceValue { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public string Extension { get; set; } = string.Empty;
}
