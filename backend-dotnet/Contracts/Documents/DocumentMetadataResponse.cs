namespace backend_dotnet.Contracts.Documents;

public sealed class DocumentMetadataResponse
{
    public Guid DocumentId { get; set; }

    // Cac field ky thuat lay tu bang Documents de frontend hien thi chung.
    public string OriginalFileName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string AccessLevel { get; set; } = string.Empty;

    // Cac field nghiep vu lay tu bang DocumentMetadata.
    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? ReportType { get; set; }

    public DateOnly? ReportDate { get; set; }

    public int? ReportMonth { get; set; }

    public int? ReportYear { get; set; }

    public string? Department { get; set; }

    public string? SourceSystem { get; set; }

    public string? Language { get; set; }

    public List<string> Keywords { get; set; } = [];

    public List<string> Tags { get; set; } = [];

    public List<string> DetectedColumns { get; set; } = [];

    public List<string> SheetNames { get; set; } = [];

    public DateTimeOffset? MetadataCreatedAt { get; set; }

    public DateTimeOffset? MetadataUpdatedAt { get; set; }
}
