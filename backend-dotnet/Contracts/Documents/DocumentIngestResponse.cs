namespace backend_dotnet.Contracts.Documents;

public sealed class DocumentIngestResponse
{
    public Guid DocumentId { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string ParserName { get; set; } = string.Empty;

    public int CharacterCount { get; set; }

    public int? PageCount { get; set; }

    public string? ErrorMessage { get; set; }
}
