namespace backend_dotnet.Modules.BackgroundJobs;

public sealed class DocumentProcessingJob
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }

    // Sau khi cai Hangfire, field nay luu id cua job trong Hangfire.
    // Vi du: backgroundJobClient.Enqueue(...) tra ve string jobId.
    public string? HangfireJobId { get; set; }

    public string JobType { get; set; } = DocumentProcessingJobType.DocumentProcess;
    public string Status { get; set; } = DocumentProcessingJobStatus.Queued;

    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 3;

    public string? LastError { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
