using backend_dotnet.Contracts.Auth;
using backend_dotnet.Contracts.Datasets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.Datasets;

[ApiController]
[Authorize]
[Route("api/documents/{documentId:guid}/dataset")]
public sealed class DatasetsController : ControllerBase
{
    private readonly DatasetProfileService _profileService;
    private readonly DatasetAnalysisService _analysisService;

    public DatasetsController(
        DatasetProfileService profileService,
        DatasetAnalysisService analysisService)
    {
        _profileService = profileService;
        _analysisService = analysisService;
    }

    [HttpPost("profile")]
    public async Task<ActionResult<DatasetProfileResponse>> Profile(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var response = await _profileService.ProfileAsync(documentId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("profile")]
    public async Task<ActionResult<List<DatasetTableProfileResponse>>> ListProfiles(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var response = await _profileService.ListProfilesAsync(documentId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<DatasetAnalysisResponse>> Analyze(
        Guid documentId,
        DatasetAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _analysisService.AnalyzeAsync(documentId, request, cancellationToken);
        return Ok(response);
    }
}

