namespace backend_dotnet.Contracts.Documents;

public sealed class DocumentSearchResponse
{
    public string Query { get; set; } = string.Empty;

    public bool Success { get; set; }

    public int Count { get; set; }

    public List<DocumentSearchResultItem> Results { get; set; } = [];

    public string? ErrorMessage { get; set; }
}
