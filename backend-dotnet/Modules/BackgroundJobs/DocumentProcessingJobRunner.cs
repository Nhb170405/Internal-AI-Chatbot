namespace backend_dotnet.Modules.BackgroundJobs;

public sealed class DocumentProcessingJobRunner
{
    private readonly DocumentProcessingJobService _jobService;
    private readonly DocumentProcessingJobHandler _handler;

    public DocumentProcessingJobRunner(
        DocumentProcessingJobService jobService,
        DocumentProcessingJobHandler handler)
    {
        _jobService = jobService;
        _handler = handler;
    }

    public async Task ProcessDocumentAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Day la method Hangfire se goi.
        // 2. Mark job running.
        // 3. Goi _handler.HandleAsync(jobId).
        // 4. Neu thanh cong: mark completed.
        // 5. Neu loi:
        //    - mark failed voi message an toan.
        //    - throw lai exception de Hangfire biet job failed va retry theo policy.
        //
        // Luu y:
        // - Method public thi Hangfire moi goi duoc.
        // - Khong inject HttpContext vao runner, vi background job khong co request hien tai.

        try
        {
            await _jobService.MarkRunningAsync(jobId, cancellationToken);
            await _handler.HandleAsync(jobId, cancellationToken);
            await _jobService.MarkCompletedAsync(jobId, cancellationToken);
        }
        catch (Exception e)
        {
            var safeMessage = e.Message;

            await _jobService.MarkFailedAsync(jobId, safeMessage, cancellationToken);

            throw;
        }
    }
}
