using backend_dotnet.Contracts.Auth;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("guest-login")]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult<CurrentUserResponse>> GuestLogin(
        [FromBody] GuestLoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.GuestLoginAsync(request, cancellationToken);
        return Ok(response);

    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult<CurrentUserResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        // TODO:
        // 1. Goi _authService.LogoutAsync().
        // 2. Return NoContent().
        await _authService.LogoutAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        // TODO:
        // 1. Goi _authService.GetCurrentUserAsync().
        // 2. Return Ok(response).
        // 3. Neu ban chon thiet ke anonymous la 401 thi return Unauthorized,
        //    nhung de frontend de dung, version dau nen return role = anonymous.
        var response = await _authService.GetCurrentUserAsync(cancellationToken);
        return Ok(response);
    }
}
