namespace backend_dotnet.Contracts.Datasets;

public sealed class DatasetAnalysisRequest
{
    // preview, list_columns, count, sum, average, group_by, top_n
    public string Operation { get; set; } = string.Empty;

    public string? SheetName { get; set; }

    public string? ValueColumn { get; set; }

    public string? GroupByColumn { get; set; }

    public int TopN { get; set; } = 10;
}
