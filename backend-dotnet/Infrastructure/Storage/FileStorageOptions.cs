namespace backend_dotnet.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    // Provider quyet dinh implementation nao duoc dang ky trong DI.
    // Development: "local"
    // Production: "r2"
    public string Provider { get; set; } = FileStorageProvider.Local;
}
