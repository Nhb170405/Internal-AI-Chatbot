namespace backend_dotnet.Contracts.Documents;

public sealed class DocumentUploadResponse
{
    public Guid Id { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Status { get; set; } = string.Empty;

    public string AccessLevel { get; set; } = string.Empty;

    public DateTimeOffset UploadedAt { get; set; }

    public Guid? ProcessingJobId { get; set; }

    public string? ProcessingJobStatus { get; set; }
}
