namespace backend_dotnet.Contracts.BackgroundJobs;

public sealed record DocumentProcessingJobResponse(
    Guid Id,
    Guid DocumentId,
    string? HangfireJobId,
    string JobType,
    string Status,
    int AttemptCount,
    int MaxAttempts,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt
);
