namespace backend_dotnet.Infrastructure.Storage;

public sealed class AzureBlobStorageOptions
{
    // Azure Storage connection string.
    // Khong hard-code gia tri that vao source code.
    public string ConnectionString { get; set; } = string.Empty;

    // Container luu file raw upload.
    public string ContainerName { get; set; } = "uploaded-documents";

    // Thoi gian SAS URL con hieu luc de Python tai file ve xu ly.
    public int ReadSasMinutes { get; set; } = 15;
}
