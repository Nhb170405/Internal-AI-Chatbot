namespace backend_dotnet.Contracts.Python;

public sealed class PythonChunkRequest
{
    public Guid DocumentId { get; set; }

    public string Text { get; set; } = string.Empty;

    public int ChunkSize { get; set; } = 1200;

    public int ChunkOverlap { get; set; } = 150;
}
