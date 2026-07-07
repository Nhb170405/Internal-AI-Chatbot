namespace backend_dotnet.Contracts.Documents;

public sealed class DocumentChunkingResponse
{
    public Guid DocumentId { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool Success { get; set; }

    public int ChunkCount { get; set; }

    public string? ErrorMessage { get; set; }
}
