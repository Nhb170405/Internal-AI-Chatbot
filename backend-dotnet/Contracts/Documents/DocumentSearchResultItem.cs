namespace backend_dotnet.Contracts.Documents;

public sealed class DocumentSearchResultItem
{
    public double Score { get; set; }

    public Guid DocumentId { get; set; }

    public Guid ChunkId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;
}
