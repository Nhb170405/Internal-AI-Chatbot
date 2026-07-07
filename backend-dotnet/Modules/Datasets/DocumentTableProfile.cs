namespace backend_dotnet.Modules.Datasets;

public sealed class DocumentTableProfile
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public string SheetName { get; set; } = string.Empty;

    public int TableIndex { get; set; }

    public int RowCount { get; set; }

    public int ColumnCount { get; set; }

    // JSON array chua danh sach cot + type do Python pandas suy luan.
    public string ColumnsJson { get; set; } = "[]";

    // JSON array chua vai dong mau. Khong luu toan bo dataset o milestone nay.
    public string SampleRowsJson { get; set; } = "[]";

    // JSON array chua canh bao ve header/cot/du lieu.
    public string WarningsJson { get; set; } = "[]";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
