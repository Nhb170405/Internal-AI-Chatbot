namespace backend_dotnet.Contracts.Python;

public sealed class PythonDatasetAnalysisRequest
{
    public string DocumentId { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public string? SheetName { get; set; }

    public string? ValueColumn { get; set; }

    public string? GroupByColumn { get; set; }

    public int TopN { get; set; } = 10;
}
