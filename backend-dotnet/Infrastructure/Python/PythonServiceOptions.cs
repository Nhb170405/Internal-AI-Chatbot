namespace backend_dotnet.Infrastructure.Python;

public sealed class PythonServiceOptions
{
    // BaseUrl cua ai-service-python, vi du: http://localhost:8000
    public string BaseUrl { get; set; } = string.Empty;

    // Timeout cho request ingestion. Milestone 5 xu ly sync nen can timeout ro rang.
    public int TimeoutSeconds { get; set; } = 60;
}
