namespace backend_dotnet.Contracts.Datasets;

public sealed class DatasetProfileResponse
{
    public Guid DocumentId { get; set; }

    public bool Success { get; set; }

    public int TableCount { get; set; }

    public List<DatasetTableProfileResponse> Profiles { get; set; } = [];

    public List<string> Warnings { get; set; } = [];

    public string? ErrorMessage { get; set; }
}
