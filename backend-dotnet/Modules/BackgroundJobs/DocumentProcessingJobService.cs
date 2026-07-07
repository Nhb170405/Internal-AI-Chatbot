using backend_dotnet.Contracts.BackgroundJobs;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using backend_dotnet.Modules.Documents;

namespace backend_dotnet.Modules.BackgroundJobs;

public sealed class DocumentProcessingJobService
{
    private readonly AppDbContext _db;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public DocumentProcessingJobService(AppDbContext db, IBackgroundJobClient backgroundJobClient)
    {
        _db = db;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<EnqueueDocumentProcessingResponse> EnqueueDocumentProcessingAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Kiem tra document co ton tai va chua bi deleted khong.
        // 2. Tao DocumentProcessingJob voi Status = queued.
        // 3. Luu job vao database.
        // 4. Sau khi cai Hangfire:
        //    - goi backgroundJobClient.Enqueue<DocumentProcessingJobRunner>(
        //        runner => runner.ProcessDocumentAsync(job.Id, CancellationToken.None)
        //      )
        //    - luu HangfireJobId vao job.
        // 5. Ghi log step enqueue.
        // 6. Return EnqueueDocumentProcessingResponse.
        //
        // Luu y:
        // - Khong luu file content/API key/cookie/token vao job.
        // - Ham nay chi tao job, khong xu ly ingest/chunk/index ngay.

        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document == null)
        {
            throw new NotFoundApiException("document_not_found", "Document not found.");
        }

        if (document.Status == DocumentStatus.Deleted)
        {
            throw new ConflictApiException("invalid_document_state", "Deleted document cannot be processed.");
        }

        var activeJob = await _db.DocumentProcessingJobs
                    .Where(job => job.DocumentId == documentId)
                    .Where(job =>
                        job.Status == DocumentProcessingJobStatus.Queued ||
                        job.Status == DocumentProcessingJobStatus.Running)
                    .OrderByDescending(job => job.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

        if (activeJob != null)
        {
            return new EnqueueDocumentProcessingResponse(
                activeJob.Id,
                activeJob.DocumentId,
                activeJob.HangfireJobId,
                activeJob.Status
            );
        }
        var now = DateTimeOffset.UtcNow;
        var jobId = Guid.NewGuid();

        var job = new DocumentProcessingJob
        {
            Id = jobId,
            DocumentId = document.Id,
            JobType = DocumentProcessingJobType.DocumentProcess,
            Status = DocumentProcessingJobStatus.Queued,
            AttemptCount = 0,
            MaxAttempts = 3,
            CreatedAt = now,
            UpdatedAt = now
        };

        var log = new DocumentProcessingJobLog
        {
            Id = Guid.NewGuid(),
            DocumentProcessingJobId = job.Id,
            DocumentId = document.Id,
            JobType = job.JobType,
            Step = DocumentProcessingStep.Enqueue,
            Status = DocumentProcessingJobStatus.Completed,
            Attempt = 0,
            CreatedAt = now,
            StartedAt = now,
            CompletedAt = now
        };

        _db.DocumentProcessingJobs.Add(job);
        _db.DocumentProcessingJobLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        var hangfireJobId = _backgroundJobClient.Enqueue<DocumentProcessingJobRunner>(runner => runner.ProcessDocumentAsync(job.Id, CancellationToken.None));

        job.HangfireJobId = hangfireJobId;
        job.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new EnqueueDocumentProcessingResponse(
            job.Id,
            job.DocumentId,
            job.HangfireJobId,
            job.Status
        );
    }

    public async Task<DocumentProcessingJobResponse?> GetLatestByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Query job moi nhat theo DocumentId.
        // 2. Order by CreatedAt descending.
        // 3. Map sang DocumentProcessingJobResponse.
        // 4. Neu khong co job thi return null.
        return await _db.DocumentProcessingJobs
            .Where(job => job.DocumentId == documentId)
            .OrderByDescending(job => job.CreatedAt)
            .Select(job => new DocumentProcessingJobResponse(
                job.Id,
                job.DocumentId,
                job.HangfireJobId,
                job.JobType,
                job.Status,
                job.AttemptCount,
                job.MaxAttempts,
                job.LastError,
                job.CreatedAt,
                job.UpdatedAt,
                job.StartedAt,
                job.CompletedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DocumentProcessingJobResponse?> GetByIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        return await _db.DocumentProcessingJobs
            .Where(job => job.Id == jobId)
            .Select(job => new DocumentProcessingJobResponse(
                job.Id,
                job.DocumentId,
                job.HangfireJobId,
                job.JobType,
                job.Status,
                job.AttemptCount,
                job.MaxAttempts,
                job.LastError,
                job.CreatedAt,
                job.UpdatedAt,
                job.StartedAt,
                job.CompletedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<DocumentProcessingJobResponse>> ListAsync(string? status, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Tao query tu _db.DocumentProcessingJobs.
        // 2. Neu status co gia tri thi filter theo status.
        // 3. Order by CreatedAt descending.
        // 4. Map sang response.
        // 5. Gioi han so dong neu can, vi admin page khong nen load vo han.
        var query = _db.DocumentProcessingJobs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(job => job.Status == status);
        }

        return await query
            .OrderByDescending(job => job.CreatedAt)
            .Take(100)
            .Select(job => new DocumentProcessingJobResponse(
                job.Id,
                job.DocumentId,
                job.HangfireJobId,
                job.JobType,
                job.Status,
                job.AttemptCount,
                job.MaxAttempts,
                job.LastError,
                job.CreatedAt,
                job.UpdatedAt,
                job.StartedAt,
                job.CompletedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DocumentProcessingJobLogResponse>> ListLogsAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Query logs theo DocumentProcessingJobId.
        // 2. Order by CreatedAt ascending de xem flow theo thoi gian.
        // 3. Map sang DocumentProcessingJobLogResponse.
        return await _db.DocumentProcessingJobLogs
            .Where(log => log.DocumentProcessingJobId == jobId)
            .OrderBy(log => log.CreatedAt)
            .Select(log => new DocumentProcessingJobLogResponse(
                log.Id,
                log.DocumentProcessingJobId,
                log.DocumentId,
                log.JobType,
                log.Step,
                log.Status,
                log.Attempt,
                log.ErrorMessage,
                log.CreatedAt,
                log.StartedAt,
                log.CompletedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task RetryAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Tim job failed.
        // 2. Kiem tra quyen user o Controller hoac service truoc khi retry.
        // 3. Reset status ve queued/running tuy cach dung Hangfire.
        // 4. Sau khi cai Hangfire, enqueue lai runner voi job.Id.
        // 5. Ghi log retry.
        var job = await _db.DocumentProcessingJobs.FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);

        if (job == null)
        {
            throw new NotFoundApiException("job_not_found", "Processing job not found.");
        }

        if (job.Status != DocumentProcessingJobStatus.Failed)
        {
            throw new ConflictApiException("invalid_job_state", "Only failed jobs can be retried.");
        }

        if (job.AttemptCount >= job.MaxAttempts)
        {
            throw new ConflictApiException("invalid_job_state", "Processing job reached max attempts.");
        }

        var now = DateTimeOffset.UtcNow;

        job.Status = DocumentProcessingJobStatus.Queued;
        job.LastError = null;
        job.StartedAt = null;
        job.CompletedAt = null;
        job.UpdatedAt = now;

        var log = new DocumentProcessingJobLog
        {
            Id = Guid.NewGuid(),
            DocumentProcessingJobId = job.Id,
            DocumentId = job.DocumentId,
            JobType = job.JobType,
            Step = DocumentProcessingStep.Retry,
            Status = DocumentProcessingJobStatus.Queued,
            Attempt = job.AttemptCount,
            CreatedAt = now,
            StartedAt = now,
            CompletedAt = now
        };

        var hangfireJobId = _backgroundJobClient.Enqueue<DocumentProcessingJobRunner>(runner => runner.ProcessDocumentAsync(job.Id, CancellationToken.None));
        job.HangfireJobId = hangfireJobId;

        _db.DocumentProcessingJobLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkRunningAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Set Status = running.
        // 2. Tang AttemptCount.
        // 3. Set StartedAt neu chua co.
        // 4. UpdatedAt = now.
        // 5. SaveChangesAsync.
        var job = await _db.DocumentProcessingJobs.FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);

        if (job == null)
        {
            throw new NotFoundApiException("job_not_found", "Processing job not found.");
        }

        var now = DateTimeOffset.UtcNow;
        job.Status = DocumentProcessingJobStatus.Running;
        job.AttemptCount += 1;
        job.StartedAt = now;
        job.CompletedAt = null;
        job.LastError = null;
        job.UpdatedAt = now;

        var log = new DocumentProcessingJobLog
        {
            Id = Guid.NewGuid(),
            DocumentProcessingJobId = job.Id,
            DocumentId = job.DocumentId,
            JobType = job.JobType,
            Step = DocumentProcessingStep.Running,
            Status = DocumentProcessingJobStatus.Running,
            Attempt = job.AttemptCount,
            CreatedAt = now,
            StartedAt = now
        };

        _db.DocumentProcessingJobLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkCompletedAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Set Status = completed.
        // 2. Set CompletedAt va UpdatedAt.
        // 3. Clear LastError neu can.
        // 4. SaveChangesAsync.
        var job = await _db.DocumentProcessingJobs.FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);

        if (job == null)
        {
            throw new NotFoundApiException("job_not_found", "Processing job not found.");
        }

        var now = DateTimeOffset.UtcNow;
        job.Status = DocumentProcessingJobStatus.Completed;
        job.CompletedAt = now;
        job.LastError = null;
        job.UpdatedAt = now;

        var log = new DocumentProcessingJobLog
        {
            Id = Guid.NewGuid(),
            DocumentProcessingJobId = job.Id,
            DocumentId = job.DocumentId,
            JobType = job.JobType,
            Step = DocumentProcessingStep.Complete,
            Status = DocumentProcessingJobStatus.Completed,
            Attempt = job.AttemptCount,
            CompletedAt = now,
            StartedAt = now,
            CreatedAt = now,
        };

        _db.DocumentProcessingJobLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid jobId, string safeErrorMessage, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Set Status = failed.
        // 2. Set LastError bang message an toan.
        // 3. Set CompletedAt va UpdatedAt.
        // 4. Khong luu stack trace dai hoac secret vao LastError.
        // 5. SaveChangesAsync.
        var job = await _db.DocumentProcessingJobs.FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);

        if (job == null)
        {
            throw new NotFoundApiException("job_not_found", "Processing job not found.");
        }

        var safeMessage = string.IsNullOrWhiteSpace(safeErrorMessage) ? "Processing job failed." : safeErrorMessage.Trim();

        if (safeMessage.Length > 1000)
        {
            safeMessage = safeMessage[..1000];
        }

        var now = DateTimeOffset.UtcNow;
        job.Status = DocumentProcessingJobStatus.Failed;
        job.CompletedAt = now;
        job.LastError = safeMessage;
        job.UpdatedAt = now;

        var log = new DocumentProcessingJobLog
        {
            Id = Guid.NewGuid(),
            DocumentProcessingJobId = job.Id,
            DocumentId = job.DocumentId,
            JobType = job.JobType,
            Step = DocumentProcessingStep.Fail,
            Status = DocumentProcessingJobStatus.Failed,
            Attempt = job.AttemptCount,
            CompletedAt = now,
            CreatedAt = now,
            ErrorMessage = safeMessage,
            StartedAt = now
        };

        _db.DocumentProcessingJobLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
