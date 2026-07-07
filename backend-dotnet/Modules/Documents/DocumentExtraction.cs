namespace backend_dotnet.Modules.Documents;

public sealed class DocumentExtraction
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    // Text da duoc Python parser extract tu file raw.
    // Milestone 6 se doc field nay de chunk.
    public string ExtractedText { get; set; } = string.Empty;

    public string ParserName { get; set; } = string.Empty;

    public int CharacterCount { get; set; }

    public int? PageCount { get; set; }

    // JSON metadata tu parser, vi du sheetCount/pageCount/strategy/warnings.
    public string? MetadataJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Document? Document { get; set; }
}
