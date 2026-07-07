using System.Text.Json;

namespace backend_dotnet.Contracts.Charts;

public sealed class ChartResponse
{
    public Guid DocumentId { get; set; }

    public bool Success { get; set; }

    public string ChartType { get; set; } = string.Empty;

    public string? ChartPath { get; set; }

    public string? ChartUrl { get; set; }

    public JsonElement? Data { get; set; }

    public List<string> Warnings { get; set; } = [];

    public string? ErrorMessage { get; set; }
}
