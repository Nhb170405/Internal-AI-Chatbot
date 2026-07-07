namespace backend_dotnet.Contracts.Python;

public sealed class PythonVectorSearchRequest
{
    public string Query { get; set; } = string.Empty;

    public int TopK { get; set; } = 5;

    public List<string> AllowedAccessLevels { get; set; } = [];
}
