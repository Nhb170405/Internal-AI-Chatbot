using backend_dotnet.Infrastructure.Persistence;
using backend_dotnet.Infrastructure.Python;
using backend_dotnet.Infrastructure.Retention;
using backend_dotnet.Infrastructure.Storage;
using backend_dotnet.Modules.Audit;
using backend_dotnet.Modules.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;


namespace backend_dotnet.Modules.BackgroundJobs;

public sealed class DeletedDocumentPurgeJobHandler
{
    private const int BatchSize = 20;

    private readonly AppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly PythonVectorClient _pythonVectorClient;
    private readonly IOptions<DocumentRetentionOptions> _retentionOptions;
    private readonly ILogger<DeletedDocumentPurgeJobHandler> _logger;
    private readonly AuditLogService _auditLogService;

    public DeletedDocumentPurgeJobHandler(
        AppDbContext db,
        IFileStorageService storage,
        PythonVectorClient pythonVectorClient,
        AuditLogService auditLogService,
        IOptions<DocumentRetentionOptions> retentionOptions,
        ILogger<DeletedDocumentPurgeJobHandler> logger
    )
    {
        _db = db;
        _storage = storage;
        _pythonVectorClient = pythonVectorClient;
        _auditLogService = auditLogService;
        _retentionOptions = retentionOptions;
        _logger = logger;
    }

    public Task PurgeExpiredDeletedDocumentsAsync()
    {
        return PurgeExpiredDeletedDocumentsAsync(CancellationToken.None);
    }

    public async Task PurgeExpiredDeletedDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var retentionDays = Math.Max(0, _retentionOptions.Value.DeletedFileRetentionDays);
        var cutoff = now.AddDays(-retentionDays);

        var documents = await _db.Documents
            .Where(document => document.Status == DocumentStatus.Deleted)
            .Where(document => document.DeletedAt != null)
            .Where(document => document.DeletedAt <= cutoff)
            .OrderBy(document => document.DeletedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var failureCount = 0;

        foreach (var document in documents)
        {
            try
            {
                await PurgeOneDocumentAsync(document, retentionDays, now, cancellationToken);
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, "Failed to purge deleted document {DocumentId}.", document.Id);
            }
        }

        if (failureCount > 0)
        {
            throw new InvalidOperationException($"{failureCount} deleted document purge item(s) failed.");
        }
    }

    private async Task PurgeOneDocumentAsync(
        Document document,
        int retentionDays,
        DateTimeOffset purgedAt,
        CancellationToken cancellationToken)
    {
        var vectorDeleteResponse = await _pythonVectorClient.DeleteDocumentVectorsAsync(document.Id, cancellationToken);

        if (!vectorDeleteResponse.Success)
        {
            throw new InvalidOperationException(
                $"Qdrant vector delete failed for document {document.Id}: {vectorDeleteResponse.ErrorMessage}");
        }

        await _storage.DeleteIfExistsAsync(document, cancellationToken);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var deletedJobLogs = await _db.DocumentProcessingJobLogs
            .Where(log => log.DocumentId == document.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var deletedJobs = await _db.DocumentProcessingJobs
            .Where(job => job.DocumentId == document.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var deletedTableProfiles = await _db.DocumentTableProfiles
            .Where(profile => profile.DocumentId == document.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var deletedChunks = await _db.DocumentChunks
            .Where(chunk => chunk.DocumentId == document.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var deletedExtractions = await _db.DocumentExtractions
            .Where(extraction => extraction.DocumentId == document.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var deletedMetadata = await _db.DocumentMetadatas
            .Where(metadata => metadata.DocumentId == document.Id)
            .ExecuteDeleteAsync(cancellationToken);

        _db.Documents.Remove(document);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "document_purge",
                ResourceType = "document",
                ResourceId = document.Id.ToString(),
                MetadataJson = JsonSerializer.Serialize(new
                {
                    originalFileName = document.OriginalFileName,
                    retentionDays,
                    deletedAt = document.DeletedAt,
                    purgedAt,
                    deletedVectors = vectorDeleteResponse.DeletedCount,
                    deletedChunks,
                    deletedExtractions,
                    deletedMetadata,
                    deletedTableProfiles,
                    deletedJobs,
                    deletedJobLogs
                })
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deleted document {DocumentId} was purged, but audit log could not be written.", document.Id);
        }
    }
}
