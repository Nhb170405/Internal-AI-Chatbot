using System.Text.Json;

namespace backend_dotnet.Contracts.Python;

public sealed class PythonChartRenderRequest
{
    public string ChartType { get; set; } = string.Empty;

    public string? Title { get; set; }

    // Data lay tu DatasetAnalysisResponse.Result.
    // Milestone 12 khong gui filePath sang chart renderer.
    public JsonElement? Data { get; set; }

    public string? XField { get; set; }

    public string? YField { get; set; }
}
