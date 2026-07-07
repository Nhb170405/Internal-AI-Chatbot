using backend_dotnet.Contracts.BackgroundJobs;
using backend_dotnet.Infrastructure.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.BackgroundJobs;

[ApiController]
[Route("api/background-jobs")]
[Authorize]
public sealed class BackgroundJobsController : ControllerBase
{
    private readonly DocumentProcessingJobService _jobService;

    public BackgroundJobsController(DocumentProcessingJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DocumentProcessingJobResponse>>> List(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        // Bai tap:
        // 1. Chi admin nen xem toan bo jobs.
        // 2. Employee sau nay chi xem jobs lien quan document employee/guest neu can.
        // 3. Goi _jobService.ListAsync(status).
        var result = await _jobService.ListAsync(status, cancellationToken);
        return Ok(result);
    }

    [HttpGet("document/{documentId:guid}")]
    public async Task<ActionResult<DocumentProcessingJobResponse>> GetLatestByDocument(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        // Bai tap:
        // 1. Kiem tra user co quyen doc document nay.
        // 2. Goi _jobService.GetLatestByDocumentAsync(documentId).
        // 3. Neu null thi return 404.
        var result = await _jobService.GetLatestByDocumentAsync(documentId, cancellationToken);
        if (result == null)
        {
            throw new NotFoundApiException("job_not_found", "Processing job not found.");
        }

        return Ok(result);
    }

    [HttpGet("{jobId:guid}/logs")]
    public async Task<ActionResult<List<DocumentProcessingJobLogResponse>>> ListLogs(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        // Bai tap:
        // 1. Kiem tra quyen xem job.
        // 2. Goi _jobService.ListLogsAsync(jobId).
        var job = await _jobService.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
        {
            throw new NotFoundApiException("job_not_found", "Processing job not found.");
        }

        var result = await _jobService.ListLogsAsync(jobId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{jobId:guid}/retry")]
    public async Task<IActionResult> Retry(Guid jobId, CancellationToken cancellationToken)
    {
        // Bai tap:
        // 1. Chi admin/employee trong access scope moi retry.
        // 2. Chi retry job failed.
        // 3. Goi _jobService.RetryAsync(jobId).
        await _jobService.RetryAsync(jobId, cancellationToken);
        return Accepted();
    }
}
