namespace backend_dotnet.Modules.BackgroundJobs;

public sealed class DeletedDocumentPurgeJobHandler
{
    public Task PurgeExpiredDeletedDocumentsAsync(CancellationToken cancellationToken = default)
    {
        // Bai tap sau:
        // 1. Doc retention config.
        // 2. Query documents Status = deleted va DeletedAt da qua retention.
        // 3. Xoa file vat ly neu con ton tai.
        // 4. Xoa/inactive chunks.
        // 5. Xoa/inactive Qdrant vectors.
        // 6. Ghi audit document_purge.
        //
        // Milestone 15 co the lam sau khi document_process job da on.
        return Task.CompletedTask;
    }
}
