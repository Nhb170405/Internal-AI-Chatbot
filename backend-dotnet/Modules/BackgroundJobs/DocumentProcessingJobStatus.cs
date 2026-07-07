namespace backend_dotnet.Modules.BackgroundJobs;

public static class DocumentProcessingJobStatus
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
}
