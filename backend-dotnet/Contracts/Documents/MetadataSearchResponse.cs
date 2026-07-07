namespace backend_dotnet.Contracts.Documents;

public sealed class MetadataSearchResponse
{
    public bool Success { get; set; }

    public int Count { get; set; }

    public List<DocumentMetadataResponse> Results { get; set; } = [];

    public string? ErrorMessage { get; set; }
}
