namespace backend_dotnet.Contracts.Assistant;

public sealed class AssistantCitationResponse
{
    public Guid DocumentId { get; set; }

    public Guid ChunkId { get; set; }

    public int ChunkIndex { get; set; }

    public double Score { get; set; }

    public string Snippet { get; set; } = string.Empty;

    public int? PageNumber { get; set; }
}
