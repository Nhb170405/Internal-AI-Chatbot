namespace backend_dotnet.Contracts.Documents;

public sealed class MetadataSearchRequest
{
    public string? Query { get; set; }

    public string? ReportType { get; set; }

    public int? ReportMonth { get; set; }

    public int? ReportYear { get; set; }

    public string? Department { get; set; }

    public string? Keyword { get; set; }

    public string? Tag { get; set; }

    public int Limit { get; set; } = 20;
}
