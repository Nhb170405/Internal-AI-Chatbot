using backend_dotnet.Contracts.Auth;
using backend_dotnet.Contracts.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.Documents;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentMetadataController : ControllerBase
{
    private readonly DocumentMetadataService _metadataService;
    private readonly DocumentMetadataRoutingService _routingService;

    public DocumentMetadataController(
        DocumentMetadataService metadataService,
        DocumentMetadataRoutingService routingService)
    {
        _metadataService = metadataService;
        _routingService = routingService;
    }

    [HttpGet("{documentId:guid}/metadata")]
    public async Task<ActionResult<DocumentMetadataResponse>> GetMetadata(
    Guid documentId,
    CancellationToken cancellationToken)
    {
        var response = await _metadataService.GetAsync(documentId, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{documentId:guid}/metadata")]
    public async Task<ActionResult<DocumentMetadataResponse>> UpdateMetadata(
    Guid documentId,
    UpdateDocumentMetadataRequest request,
    CancellationToken cancellationToken)
    {
        var response = await _metadataService.UpdateAsync(documentId, request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("metadata/search")]
    public async Task<ActionResult<MetadataSearchResponse>> SearchMetadata(
        [FromQuery] MetadataSearchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _routingService.SearchAsync(request, cancellationToken);
        return Ok(response);
    }
}
