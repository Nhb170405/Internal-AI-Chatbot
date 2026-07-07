namespace backend_dotnet.Contracts.Python;

public sealed class PythonDatasetProfileResponse
{
    public string DocumentId { get; set; } = string.Empty;

    public bool Success { get; set; }

    public List<PythonDatasetTableProfile> Profiles { get; set; } = [];

    public List<string> Warnings { get; set; } = [];

    public string? ErrorMessage { get; set; }
}
