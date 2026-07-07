namespace backend_dotnet.Modules.BackgroundJobs;

public sealed class DocumentProcessingJobLog
{
    public Guid Id { get; set; }
    public Guid DocumentProcessingJobId { get; set; }
    public Guid DocumentId { get; set; }

    public string JobType { get; set; } = DocumentProcessingJobType.DocumentProcess;
    public string Step { get; set; } = DocumentProcessingStep.Enqueue;
    public string Status { get; set; } = DocumentProcessingJobStatus.Queued;

    public int Attempt { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
