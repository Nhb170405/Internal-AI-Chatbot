using System.Text.Json;

namespace backend_dotnet.Contracts.Python;

public sealed class PythonDatasetAnalysisResponse
{
    public string DocumentId { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string Operation { get; set; } = string.Empty;

    public JsonElement? Result { get; set; }

    public int? RowCount { get; set; }

    public List<string> Warnings { get; set; } = [];

    public string? ErrorMessage { get; set; }
}
