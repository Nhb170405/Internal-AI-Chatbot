namespace backend_dotnet.Contracts.Python;

public sealed class PythonVectorIndexRequest
{
    public Guid DocumentId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string AccessLevel { get; set; } = string.Empty;

    public string DocumentStatus { get; set; } = string.Empty;

    public List<PythonVectorChunkInput> Chunks { get; set; } = [];
}
