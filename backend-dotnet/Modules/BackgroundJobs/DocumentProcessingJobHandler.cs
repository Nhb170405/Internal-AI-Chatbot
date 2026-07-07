using backend_dotnet.Modules.Datasets;
using backend_dotnet.Modules.Documents;
using backend_dotnet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Modules.BackgroundJobs;

public sealed class DocumentProcessingJobHandler
{
    private readonly AppDbContext _db;
    private readonly DocumentIngestionService _ingestionService;
    private readonly DocumentChunkingService _chunkingService;
    private readonly DocumentIndexingService _indexingService;
    private readonly DatasetProfileService _datasetProfileService;

    public DocumentProcessingJobHandler(
        AppDbContext db,
        DocumentIngestionService ingestionService,
        DocumentChunkingService chunkingService,
        DocumentIndexingService indexingService,
        DatasetProfileService datasetProfileService)
    {
        _db = db;
        _ingestionService = ingestionService;
        _chunkingService = chunkingService;
        _indexingService = indexingService;
        _datasetProfileService = datasetProfileService;
    }

    public async Task HandleAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Tu jobId lay DocumentProcessingJob trong database.
        // 2. Lay documentId tu job.
        // 3. Kiem tra document con ton tai, chua deleted.
        // 4. Set Document.Status = processing.
        // 5. Goi _ingestionService.IngestSystemAsync(documentId).
        // 6. Neu ingest fail thi throw loi an toan.
        // 7. Goi _chunkingService.ChunkSystemAsync(documentId).
        // 8. Neu chunk fail thi throw loi an toan.
        // 9. Goi _indexingService.IndexSystemAsync(documentId).
        // 10. Neu index fail thi throw loi an toan.
        // 11. Neu extension la .csv/.xlsx/.xls thi goi _datasetProfileService.ProfileSystemAsync(documentId).
        // 12. Cap nhat Document.Status cuoi cung.
        // 13. Ghi job log cho tung step.
        //
        // Luu y idempotency:
        // - Neu retry, khong duoc tao duplicate chunks/vectors/profile.
        // - Cac service con nen replace/upsert du lieu theo documentId.
        var job = await _db.DocumentProcessingJobs.FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);
        if (job == null)
        {
            throw new InvalidOperationException("Processing job not found.");
        }
        var documentId = job.DocumentId;
        var document = await _db.Documents
    .Where(document => document.Id == documentId)
    .Where(document => document.Status != DocumentStatus.Deleted)
    .FirstOrDefaultAsync(cancellationToken);
        if (document == null)
        {
            throw new InvalidOperationException("Document not found.");
        }

        var ingestResult = await _ingestionService.IngestSystemAsync(document.Id, cancellationToken);
        if (!ingestResult.Success)
        {
            await AddStepLogAsync(job, DocumentProcessingStep.Ingest, DocumentProcessingJobStatus.Failed, ingestResult.ErrorMessage, cancellationToken);
            throw new InvalidOperationException(ingestResult.ErrorMessage ?? "Document ingestion failed.");
        }
        await AddStepLogAsync(job, DocumentProcessingStep.Ingest, DocumentProcessingJobStatus.Completed, null, cancellationToken);

        var chunkResult = await _chunkingService.ChunkSystemAsync(document.Id, cancellationToken);

        if (!chunkResult.Success)
        {
            await AddStepLogAsync(job, DocumentProcessingStep.Chunk, DocumentProcessingJobStatus.Failed, chunkResult.ErrorMessage, cancellationToken);
            throw new InvalidOperationException(chunkResult.ErrorMessage ?? "Document chunking failed.");
        }
        await AddStepLogAsync(job, DocumentProcessingStep.Chunk, DocumentProcessingJobStatus.Completed, null, cancellationToken);

        var indexResult = await _indexingService.IndexSystemAsync(document.Id, cancellationToken);

        if (!indexResult.Success)
        {
            await AddStepLogAsync(job, DocumentProcessingStep.Index, DocumentProcessingJobStatus.Failed, indexResult.ErrorMessage, cancellationToken);
            throw new InvalidOperationException(indexResult.ErrorMessage ?? "Document indexing failed.");
        }
        await AddStepLogAsync(job, DocumentProcessingStep.Index, DocumentProcessingJobStatus.Completed, null, cancellationToken);

        if (IsTableFile(document.Extension))
        {
            var profileResult = await _datasetProfileService.ProfileSystemAsync(document.Id, cancellationToken);

            if (!profileResult.Success)
            {
                await AddStepLogAsync(job, DocumentProcessingStep.Profile, DocumentProcessingJobStatus.Failed, profileResult.ErrorMessage, cancellationToken);
                throw new InvalidOperationException(profileResult.ErrorMessage ?? "Dataset profile failed.");
            }
            await AddStepLogAsync(job, DocumentProcessingStep.Profile, DocumentProcessingJobStatus.Completed, null, cancellationToken);
        }
    }

    private async Task AddStepLogAsync(
        DocumentProcessingJob job,
        string step,
        string status,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var safeError = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage.Trim();

        if (safeError is { Length: > 1000 })
        {
            safeError = safeError[..1000];
        }

        _db.DocumentProcessingJobLogs.Add(new DocumentProcessingJobLog
        {
            Id = Guid.NewGuid(),
            DocumentProcessingJobId = job.Id,
            DocumentId = job.DocumentId,
            JobType = job.JobType,
            Step = step,
            Status = status,
            Attempt = job.AttemptCount,
            ErrorMessage = safeError,
            CreatedAt = now,
            StartedAt = now,
            CompletedAt = status == DocumentProcessingJobStatus.Completed || status == DocumentProcessingJobStatus.Failed
                ? now
                : null
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsTableFile(string extension)
    {
        // Bai tap:
        // 1. Normalize extension ve lowercase.
        // 2. Return true neu la .csv, .xlsx, .xls.
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        var normalized = extension.Trim().ToLowerInvariant();

        return normalized == ".csv"
            || normalized == ".xlsx"
            || normalized == ".xls";
    }
}
