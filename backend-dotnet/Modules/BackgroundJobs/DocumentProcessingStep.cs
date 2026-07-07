namespace backend_dotnet.Modules.BackgroundJobs;

public static class DocumentProcessingStep
{
    public const string Enqueue = "enqueue";
    public const string Ingest = "ingest";
    public const string Chunk = "chunk";
    public const string Index = "index";
    public const string Profile = "profile";
    public const string Complete = "complete";
    public const string Fail = "fail";
    public const string Retry = "retry";
    public const string Running = "running";
}
