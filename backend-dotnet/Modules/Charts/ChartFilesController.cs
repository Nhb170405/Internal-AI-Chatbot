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
    public IActionResult ViewChart(string fileName)
    {
        var path = _chartFileService.GetExistingChartPath(fileName);
        return PhysicalFile(path, "image/png");
    }
}
