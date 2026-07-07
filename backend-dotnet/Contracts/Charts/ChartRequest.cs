namespace backend_dotnet.Contracts.Charts;

public sealed class ChartRequest
{
    // bar, line, pie trong version dau.
    public string ChartType { get; set; } = string.Empty;

    // Cac field duoc truyen cho DatasetAnalysisService truoc.
    // Chart khong tu tinh toan, ma dung result cua analysis.
    public string Operation { get; set; } = string.Empty;

    public string? SheetName { get; set; }

    public string? ValueColumn { get; set; }

    public string? GroupByColumn { get; set; }

    public int TopN { get; set; } = 10;

    // Thong tin render chart.
    public string? Title { get; set; }

    public string? XField { get; set; }

    public string? YField { get; set; }
}
