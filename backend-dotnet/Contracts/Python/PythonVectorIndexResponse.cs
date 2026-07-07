namespace backend_dotnet.Contracts.Python;

public sealed class PythonVectorIndexResponse
{
    public Guid DocumentId { get; set; }

    public bool Success { get; set; }

    public string CollectionName { get; set; } = string.Empty;

    public int IndexedCount { get; set; }

    public string? ErrorMessage { get; set; }
}
