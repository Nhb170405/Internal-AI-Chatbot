using System.Text.Json;

namespace backend_dotnet.Contracts.Python;

public sealed class PythonIngestResponse
{
    public Guid DocumentId { get; set; }

    public bool Success { get; set; }

    public string ParserName { get; set; } = string.Empty;

    public string ExtractedText { get; set; } = string.Empty;

    public int CharacterCount { get; set; }

    public int? PageCount { get; set; }

    public JsonElement Metadata { get; set; }

    public List<string> Warnings { get; set; } = [];

    public string? ErrorMessage { get; set; }
}
