namespace backend_dotnet.Contracts.Documents;

public sealed class DocumentChunkResponse
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public int CharacterCount { get; set; }

    public int? StartOffset { get; set; }

    public int? EndOffset { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
