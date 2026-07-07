namespace backend_dotnet.Contracts.Documents;

public sealed class DocumentListItemResponse
{
    public Guid Id { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Status { get; set; } = string.Empty;

    public string AccessLevel { get; set; } = string.Empty;

    public bool HasTableProfile { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
