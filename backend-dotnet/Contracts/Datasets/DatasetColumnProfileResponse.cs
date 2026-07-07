namespace backend_dotnet.Contracts.Datasets;

public sealed class DatasetColumnProfileResponse
{
    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public int NonNullCount { get; set; }

    public int NullCount { get; set; }
}
