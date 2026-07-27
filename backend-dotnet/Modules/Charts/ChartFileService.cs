using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace backend_dotnet.Modules.Charts;

public sealed class ChartFileService
{
    private const int MaxChartBytes = 10 * 1024 * 1024;
    private readonly IServiceProvider _services;
    private readonly string _provider;
    private readonly string _localRootPath;

    public ChartFileService(
        IServiceProvider services,
        IConfiguration configuration,
        IOptions<ChartStorageOptions> options,
        IWebHostEnvironment environment)
    {
        _services = services;
        _provider = configuration.GetValue<string>("FileStorage:Provider")?
            .Trim()
            .ToLowerInvariant() ?? FileStorageProvider.Local;

        var configuredRoot = options.Value.RootPath;
        _localRootPath = Path.GetFullPath(Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(environment.ContentRootPath, configuredRoot));
    }

    public bool UsesLocalStorage => _provider == FileStorageProvider.Local;

    public async Task<string> SaveChartAsync(
        string chartContentBase64,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chartContentBase64))
        {
            throw new ValidationApiException(
                "invalid_chart_content",
                "Python chart response did not contain image data.");
        }

        byte[] content;
        try
        {
            content = Convert.FromBase64String(chartContentBase64);
        }
        catch (FormatException)
        {
            throw new ValidationApiException(
                "invalid_chart_content",
                "Python chart response contained invalid image data.");
        }

        if (content.Length == 0 || content.Length > MaxChartBytes)
        {
            throw new ValidationApiException(
                "invalid_chart_content",
                "Generated chart size is invalid.");
        }

        var fileName = $"chart_{Guid.NewGuid():N}.png";
        if (UsesLocalStorage)
        {
            Directory.CreateDirectory(_localRootPath);
            var fullPath = Path.Combine(_localRootPath, fileName);
            await File.WriteAllBytesAsync(fullPath, content, cancellationToken);
        }
        else
        {
            EnsureR2Provider();
            var key = $"charts/{fileName}";
            await using var stream = new MemoryStream(content, writable: false);
            await GetR2Storage().PutAsync(key, stream, "image/png", cancellationToken);
        }

        return fileName;
    }

    public string GetExistingLocalPath(string fileName)
    {
        ValidateFileName(fileName);
        var fullPath = Path.GetFullPath(Path.Combine(_localRootPath, fileName));
        if (!fullPath.StartsWith(_localRootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationApiException("invalid_chart_file", "Chart file path is not valid.");
        }

        if (!File.Exists(fullPath))
        {
            throw new NotFoundApiException("chart_not_found", "Chart file not found.");
        }

        return fullPath;
    }

    public async Task<string> GetReadUrlAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ValidateFileName(fileName);
        EnsureR2Provider();

        var key = $"charts/{fileName}";
        var storage = GetR2Storage();
        if (!await storage.ExistsAsync(key, cancellationToken))
        {
            throw new NotFoundApiException("chart_not_found", "Chart file not found.");
        }

        return storage.CreatePresignedReadUrl(key);
    }

    private R2ObjectStorageService GetR2Storage() =>
        _services.GetRequiredService<R2ObjectStorageService>();

    private void EnsureR2Provider()
    {
        if (_provider != FileStorageProvider.R2)
        {
            throw new InvalidOperationException($"Unsupported chart storage provider '{_provider}'.");
        }
    }

    private static void ValidateFileName(string fileName)
    {
        if (!IsAllowedFileName(fileName))
        {
            throw new ValidationApiException("invalid_chart_file", "Chart file name is not valid.");
        }
    }

    private static bool IsAllowedFileName(string fileName) =>
        fileName == Path.GetFileName(fileName) &&
        fileName.StartsWith("chart_", StringComparison.Ordinal) &&
        string.Equals(Path.GetExtension(fileName), ".png", StringComparison.OrdinalIgnoreCase);
}
