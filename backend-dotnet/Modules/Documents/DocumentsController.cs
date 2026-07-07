using backend_dotnet.Contracts.Documents;
using backend_dotnet.Modules.BackgroundJobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.Documents;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly DocumentService _documentService;
    private readonly DocumentIngestionService _documentIngestionService;
    private readonly DocumentChunkingService _documentChunkingService;
    private readonly DocumentIndexingService _documentIndexingService;
    private readonly DocumentProcessingJobService _documentProcessingJobService;

    public DocumentsController(
        DocumentService documentService,
        DocumentIngestionService documentIngestionService,
        DocumentChunkingService documentChunkingService,
        DocumentIndexingService documentIndexingService,
        DocumentProcessingJobService documentProcessingJobService)
    {
        _documentService = documentService;
        _documentIngestionService = documentIngestionService;
        _documentChunkingService = documentChunkingService;
        _documentIndexingService = documentIndexingService;
        _documentProcessingJobService = documentProcessingJobService;
    }

    [HttpPost("upload")]
    [EnableRateLimiting("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<ActionResult<DocumentUploadResponse>> Upload(
        [FromForm] DocumentUploadRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _documentService.UploadAsync(request.File, request.AccessLevel, cancellationToken);
        var job = await _documentProcessingJobService.EnqueueDocumentProcessingAsync(response.Id, cancellationToken);

        response.ProcessingJobId = job.JobId;
        response.ProcessingJobStatus = job.Status;

        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<List<DocumentListItemResponse>>> List(CancellationToken cancellationToken)
    {
        var documents = await _documentService.ListAsync(cancellationToken);
        return Ok(documents);
    }

    [HttpGet("{documentId:guid}")]
    public async Task<ActionResult<DocumentResponse>> GetById(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _documentService.GetByIdAsync(documentId, cancellationToken);
        return Ok(document);
    }

    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> Delete(Guid documentId, CancellationToken cancellationToken)
    {
        await _documentService.DeleteAsync(documentId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{documentId:guid}/restore")]
    public async Task<ActionResult<DocumentResponse>> Restore(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _documentService.RestoreAsync(documentId, cancellationToken);
        return Ok(document);
    }

    [HttpPost("{documentId:guid}/ingest")]
    public async Task<ActionResult<DocumentIngestResponse>> Ingest(Guid documentId, CancellationToken cancellationToken)
    {
        var response = await _documentIngestionService.IngestAsync(documentId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{documentId:guid}/chunk")]
    public async Task<ActionResult<DocumentChunkingResponse>> Chunk(Guid documentId, CancellationToken cancellationToken)
    {
        var response = await _documentChunkingService.ChunkAsync(documentId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{documentId:guid}/chunks")]
    public async Task<ActionResult<List<DocumentChunkResponse>>> ListChunks(Guid documentId, CancellationToken cancellationToken)
    {
        var chunks = await _documentChunkingService.ListChunksAsync(documentId, cancellationToken);
        return Ok(chunks);
    }

    [HttpPost("{documentId:guid}/index")]
    public async Task<ActionResult<DocumentIndexResponse>> Index(Guid documentId, CancellationToken cancellationToken)
    {
        var response = await _documentIndexingService.IndexAsync(documentId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("search")]
    public async Task<ActionResult<DocumentSearchResponse>> Search(
        [FromQuery] string query,
        [FromQuery] int topK = 2,
        CancellationToken cancellationToken = default)
    {
        var response = await _documentIndexingService.SearchAsync(query, topK, cancellationToken);
        return Ok(response);
    }
}
