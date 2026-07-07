namespace backend_dotnet.Modules.Documents;

public sealed class DocumentChunk
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public int CharacterCount { get; set; }

    public int? StartOffset { get; set; }

    public int? EndOffset { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Document? Document { get; set; }
}
