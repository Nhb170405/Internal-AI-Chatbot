namespace backend_dotnet.Contracts.BackgroundJobs;

public sealed record DocumentProcessingJobLogResponse(
    Guid Id,
    Guid DocumentProcessingJobId,
    Guid DocumentId,
    string JobType,
    string Step,
    string Status,
    int Attempt,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt
);
