namespace backend_dotnet.Contracts.BackgroundJobs;

public sealed record EnqueueDocumentProcessingResponse(
    Guid JobId,
    Guid DocumentId,
    string? HangfireJobId,
    string Status
);
