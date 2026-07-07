using backend_dotnet.Contracts.Charts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.Charts;

[ApiController]
[Authorize]
[Route("api/documents/{documentId:guid}/dataset/chart")]
public sealed class ChartsController : ControllerBase
{
    private readonly ChartService _chartService;

    public ChartsController(ChartService chartService)
    {
        _chartService = chartService;
    }

    [HttpPost]
    public async Task<ActionResult<ChartResponse>> CreateChart(
        Guid documentId,
        ChartRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _chartService.CreateChartAsync(documentId, request, cancellationToken);
        return Ok(response);
    }
}
