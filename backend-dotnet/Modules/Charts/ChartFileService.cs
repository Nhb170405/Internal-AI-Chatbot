using backend_dotnet.Infrastructure.Errors;
using Microsoft.Extensions.Options;

namespace backend_dotnet.Modules.Charts;

public sealed class ChartFileService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png"
    };

    private readonly string _rootPath;

    public ChartFileService(IOptions<ChartStorageOptions> options, IWebHostEnvironment environment)
    {
        var configuredRoot = options.Value.RootPath;
        _rootPath = Path.GetFullPath(Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(environment.ContentRootPath, configuredRoot));
    }

    public string? CreateChartUrl(string? chartPath)
    {
        if (string.IsNullOrWhiteSpace(chartPath))
        {
            return null;
        }

        var fileName = Path.GetFileName(chartPath);
        if (!IsAllowedFileName(fileName))
        {
            return null;
        }

        return $"/api/charts/{Uri.EscapeDataString(fileName)}";
    }

    public string GetExistingChartPath(string fileName)
    {
        if (!IsAllowedFileName(fileName))
        {
            throw new ValidationApiException("invalid_chart_file", "Chart file name is not valid.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, fileName));
        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationApiException("invalid_chart_file", "Chart file path is not valid.");
        }

        if (!File.Exists(fullPath))
        {
            throw new NotFoundApiException("chart_not_found", "Chart file not found.");
        }

        return fullPath;
    }

    private static bool IsAllowedFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        if (fileName != Path.GetFileName(fileName))
        {
            return false;
        }

        return AllowedExtensions.Contains(Path.GetExtension(fileName));
    }
}
