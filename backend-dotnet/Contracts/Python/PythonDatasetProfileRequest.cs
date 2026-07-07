namespace backend_dotnet.Contracts.Python;

public sealed class PythonDatasetProfileRequest
{
    public string DocumentId { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;
}
