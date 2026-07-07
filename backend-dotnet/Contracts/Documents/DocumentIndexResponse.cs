namespace backend_dotnet.Contracts.Documents;

public sealed class DocumentIndexResponse
{
    public Guid DocumentId { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool Success { get; set; }

    public int IndexedCount { get; set; }

    public string CollectionName { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}
