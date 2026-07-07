using backend_dotnet.Contracts.Admin;
using backend_dotnet.Modules.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.Admin;

[ApiController]
[Authorize(Roles = UserRole.Admin)]
[Route("api/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly AdminUserService _userService;

    public AdminUsersController(AdminUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminUserItemResponse>>> List(CancellationToken cancellationToken)
    {
        var users = await _userService.ListAsync(cancellationToken);
        return Ok(users);
    }
}
