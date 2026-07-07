namespace backend_dotnet.Contracts.Datasets;

public sealed class DatasetTableProfileResponse
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public string SheetName { get; set; } = string.Empty;

    public int TableIndex { get; set; }

    public int RowCount { get; set; }

    public int ColumnCount { get; set; }

    public List<DatasetColumnProfileResponse> Columns { get; set; } = [];

    public List<Dictionary<string, object?>> SampleRows { get; set; } = [];

    public List<string> Warnings { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
