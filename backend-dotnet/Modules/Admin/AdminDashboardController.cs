using backend_dotnet.Contracts.Admin;
using backend_dotnet.Modules.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.Admin;

[ApiController]
[Authorize(Roles = UserRole.Admin)]
[Route("api/admin")]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly AdminDashboardService _dashboardService;

    public AdminDashboardController(AdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AdminOverviewResponse>> Overview(CancellationToken cancellationToken)
    {
        var response = await _dashboardService.GetOverviewAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("usage")]
    public async Task<ActionResult<AdminUsageResponse>> Usage(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var response = await _dashboardService.GetUsageAsync(from, to, cancellationToken);
        return Ok(response);
    }
}
