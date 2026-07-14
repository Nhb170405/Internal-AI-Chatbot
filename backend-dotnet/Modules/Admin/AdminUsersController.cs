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

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<AdminUserItemResponse>> GetById(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Controller chi map HTTP GET /api/admin/users/{userId} vao service.
        // Neu user khong ton tai, service se nem NotFoundApiException va middleware tra 404 JSON chuan.
        var user = await _userService.GetByIdAsync(userId, cancellationToken);
        return Ok(user);
    }

    [HttpPost("employees")]
    public async Task<ActionResult<AdminUserItemResponse>> CreateEmployee(
        [FromBody] CreateEmployeeUserRequest request,
        CancellationToken cancellationToken)
    {
        // Bai tap:
        // 1. Controller chi nhan HTTP request.
        // 2. Goi AdminUserService.CreateEmployeeAsync de xu ly nghiep vu.
        // 3. Khong hash password trong controller.
        // 4. Khong check trung email trong controller.
        // 5. Khong try/catch tai day, vi GlobalExceptionMiddleware da chuyen ApiException thanh JSON loi chuan.
        // 6. Neu tao thanh cong, tra 201 Created hoac 200 OK deu duoc trong MVP.
        //    O day dung 201 Created vi day la thao tac tao resource moi.

        var createdUser = await _userService.CreateEmployeeAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { userId = createdUser.Id },
            createdUser);
    }
}
