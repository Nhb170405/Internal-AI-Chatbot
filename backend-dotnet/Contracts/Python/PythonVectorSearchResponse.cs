namespace backend_dotnet.Contracts.Python;

public sealed class PythonVectorSearchResponse
{
    public bool Success { get; set; }

    public List<PythonVectorSearchHit> Hits { get; set; } = [];

    public string? ErrorMessage { get; set; }
}
