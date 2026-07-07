namespace backend_dotnet.Modules.Documents;

public sealed class DeletedDocumentPurgeJob
{
    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Skeleton cho Milestone 12/13, chua implement o Milestone 4.
        //
        // Muc tieu:
        // 1. Doc DocumentRetentionOptions.DeletedFileRetentionDays.
        // 2. Tim Documents co Status = DocumentStatus.Deleted.
        // 3. Chi xu ly document co DeletedAt < now - retentionDays.
        // 4. Xoa file vat ly trong StoragePath neu ton tai.
        // 5. Sau nay xoa/inactive DocumentChunks.
        // 6. Sau nay xoa/inactive vectors trong Qdrant theo documentId.
        // 7. Ghi audit/system log action = "document_purge".
        //
        // Luu y:
        // - Job phai idempotent: chay lai khong gay loi nghiem trong.
        // - Khong hard delete metadata SQL neu van can audit trail.
        // - Neu file da mat san thi ghi log va tiep tuc.
        return Task.CompletedTask;
    }
}
