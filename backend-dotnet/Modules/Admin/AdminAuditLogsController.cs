using backend_dotnet.Contracts.Admin;
using backend_dotnet.Modules.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.Admin;

[ApiController]
[Authorize(Roles = UserRole.Admin)]
[Route("api/admin/audit-logs")]
public sealed class AdminAuditLogsController : ControllerBase
{
    private readonly AdminAuditLogService _auditLogService;

    public AdminAuditLogsController(AdminAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<AdminAuditLogsResponse>> List(
        [FromQuery] string? action,
        [FromQuery] string? resourceType,
        [FromQuery] string? actorId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        var response = await _auditLogService.ListAsync(
            action,
            resourceType,
            actorId,
            from,
            to,
            page,
            pageSize,
            cancellationToken);

        return Ok(response);
    }
}
