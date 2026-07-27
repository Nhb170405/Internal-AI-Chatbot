using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.Charts;

[ApiController]
[Authorize]
[Route("api/charts")]
public sealed class ChartFilesController : ControllerBase
{
    private readonly ChartFileService _chartFileService;

    public ChartFilesController(ChartFileService chartFileService)
    {
        _chartFileService = chartFileService;
    }

    [HttpGet("{fileName}")]
    public async Task<IActionResult> ViewChart(
        string fileName,
        CancellationToken cancellationToken)
    {
        if (_chartFileService.UsesLocalStorage)
        {
            var path = _chartFileService.GetExistingLocalPath(fileName);
            return PhysicalFile(path, "image/png");
        }

        var url = await _chartFileService.GetReadUrlAsync(fileName, cancellationToken);
        return Redirect(url);
    }
}
