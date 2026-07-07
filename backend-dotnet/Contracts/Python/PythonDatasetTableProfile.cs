using System.Text.Json;

namespace backend_dotnet.Contracts.Python;

public sealed class PythonDatasetTableProfile
{
    public string SheetName { get; set; } = string.Empty;

    public int TableIndex { get; set; }

    public int RowCount { get; set; }

    public int ColumnCount { get; set; }

    public List<PythonDatasetColumnProfile> Columns { get; set; } = [];

    public List<Dictionary<string, JsonElement>> SampleRows { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}
