namespace backend_dotnet.Contracts.Documents;

public sealed class UpdateDocumentMetadataRequest
{
    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? ReportType { get; set; }

    public DateOnly? ReportDate { get; set; }

    public int? ReportMonth { get; set; }

    public int? ReportYear { get; set; }

    public string? Department { get; set; }

    public string? SourceSystem { get; set; }

    public string? Language { get; set; }

    public List<string>? Keywords { get; set; }

    public List<string>? Tags { get; set; }
}
