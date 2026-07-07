using System.Text.Json;

namespace backend_dotnet.Contracts.Python;

public sealed class PythonChartRenderResponse
{
    public bool Success { get; set; }

    public string ChartType { get; set; } = string.Empty;

    public string? ChartPath { get; set; }

    public JsonElement? Data { get; set; }

    public List<string> Warnings { get; set; } = [];

    public string? ErrorMessage { get; set; }
}
