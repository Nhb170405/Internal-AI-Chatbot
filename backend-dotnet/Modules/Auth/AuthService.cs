using backend_dotnet.Contracts.Auth;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Modules.Users;
using backend_dotnet.Modules.Sessions;
using backend_dotnet.Modules.Audit;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace backend_dotnet.Modules.Auth;

public sealed class AuthService
{
    // TODO: Sau nay inject cac dependency can thiet:
    // - UserService: tim employee/admin.
    // - SessionService: tao guest session.
    // - AuditLogService: ghi login/logout.
    // - IHttpContextAccessor: tao/xoa cookie auth.
    // - IPasswordHasher hoac PasswordHasher<AppUser>: verify password.
    private readonly UserService _userService;
    private readonly SessionService _sessionService;
    private readonly AuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public AuthService(UserService userService, SessionService sessionService, AuditLogService auditLogService, IHttpContextAccessor httpContextAccessor, IPasswordHasher<AppUser> passwordHasher)
    {
        _userService = userService;
        _sessionService = sessionService;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
        _passwordHasher = passwordHasher;
    }


    // Dang nhap dang guest
    public async Task<CurrentUserResponse> GuestLoginAsync(
        GuestLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Goi SessionService.CreateGuestSessionAsync(request.DisplayName).
        // 2. Tao claims:
        //    - role = guest
        //    - guest_session_id = guestSession.Id
        //    - display_name = guestSession.DisplayName
        // 3. SignInAsync bang cookie authentication.
        // 4. Ghi audit action = guest_login.
        // 5. Return CurrentUserResponse role = guest.

        if (request is null)
        {
            throw new ValidationApiException("invalid_request", "Guest login request is required.");
        }

        var guestSession = await _sessionService.CreateGuestSessionAsync(request.DisplayName, cancellationToken);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role,UserRole.Guest),
            new Claim("guest_session_id",guestSession.Id.ToString()),
            new Claim("display_name", guestSession.DisplayName)
        };

        await SignInWithClaimsAsync(claims, guestSession.ExpiresAt);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorGuestSessionId = guestSession.Id,
            Action = "guest_login",
            ResourceType = "Auth",
            ResourceId = guestSession.Id.ToString(),
            MetadataJson = JsonSerializer.Serialize(new { displayNameProvided = !string.IsNullOrWhiteSpace(request.DisplayName) }),
            IpAddress = GetIpAddress()
        }, cancellationToken);
        return new CurrentUserResponse
        {
            GuestSessionId = guestSession.Id,
            DisplayName = guestSession.DisplayName,
            Role = UserRole.Guest,
            ExpiresAt = guestSession.ExpiresAt
        };
    }


    // Dang nhap dang Admin/Employee
    public async Task<CurrentUserResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Validate email/password khong rong.
        // 2. Goi UserService.FindByEmailAsync(request.Email).
        // 3. Neu khong thay user hoac user inactive: tra loi loi dang nhap that bai.
        // 4. Verify password bang password hash.
        // 5. Tao claims:
        //    - user_id
        //    - email
        //    - display_name
        //    - role = employee/admin
        //    - department_id neu co
        // 6. SignInAsync bang cookie authentication.
        // 7. Ghi audit employee_login_success/admin_login_success.
        // 8. Return CurrentUserResponse.
        //
        // Luu y:
        // - Khong bao loi qua chi tiet neu sai email/password.
        // - Khong chap nhan role tu request body.

        if (request is null)
        {
            throw new ValidationApiException("invalid_request", "Login request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            await LogLoginFailedAsync(request.Email, "Missing Email or Password", cancellationToken);

            throw InvalidCredentials();
        }

        var user = await _userService.FindByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            await LogLoginFailedAsync(request.Email, "InvalidCredentials", cancellationToken);

            throw InvalidCredentials();
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            await LogLoginFailedAsync(
                request.Email,
                "InvalidCredentials",
                cancellationToken);

            throw InvalidCredentials();
        }

        if (!IsInternalRole(user.Role))
        {
            await LogLoginFailedAsync(
                request.Email,
                "InvalidCredentials",
                cancellationToken);

            throw InvalidCredentials();
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            await LogLoginFailedAsync(request.Email, "Invalidcredentials", cancellationToken);
            throw InvalidCredentials();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email,user.Email),
            new Claim(ClaimTypes.Name,user.DisplayName),
            new Claim(ClaimTypes.Role,user.Role)
        };

        if (user.DepartmentId is not null)
        {
            claims.Add(new Claim("department_id", user.DepartmentId.Value.ToString()));
        }

        var expiresUtc = DateTimeOffset.UtcNow.AddHours(8);

        await SignInWithClaimsAsync(claims, expiresUtc);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = user.Id,
            Action = user.Role == UserRole.Admin
                ? "admin_login_success"
                : "employee_login_success",
            ResourceType = "Auth",
            ResourceId = user.Id.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                email = MaskEmail(user.Email)
            }),
            IpAddress = GetIpAddress()
        }, cancellationToken);

        return new CurrentUserResponse
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            DepartmentId = user.DepartmentId
        };
    }


    // Dang xuat
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Doc current user/guest tu HttpContext.User.
        // 2. SignOutAsync cookie authentication.
        // 3. Ghi audit action = logout.
        // 4. Khong can return data phuc tap, controller co the tra 204 NoContent.
        var httpContext = GetHttpContext();
        var principal = httpContext.User;

        var actorUserId = TryGetGuidClaim(principal, ClaimTypes.NameIdentifier);

        var actorSessionId = TryGetGuidClaim(principal, "guest_session_id");

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = actorUserId,
            ActorGuestSessionId = actorSessionId,
            Action = "logout",
            ResourceType = "Auth",
            IpAddress = GetIpAddress()
        }, cancellationToken);
    }


    // Hoi backend hien tai toi la ai
    public async Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Doc HttpContext.User.
        // 2. Neu chua authenticated: return role = anonymous.
        // 3. Neu role = guest:
        //    - doc guest_session_id tu claims.
        //    - kiem tra guest session con han.
        //    - return CurrentUserResponse role = guest.
        // 4. Neu role = employee/admin:
        //    - doc user_id tu claims.
        //    - optional: reload user tu SQL Server de biet user con active khong.
        //    - return CurrentUserResponse.
        var httpContext = GetHttpContext();
        var principal = httpContext.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            return AnonymousUser();
        }

        var role = principal.FindFirstValue(ClaimTypes.Role);

        if (role == UserRole.Guest)
        {
            var guestSessionId = TryGetGuidClaim(
                principal,
                "guest_session_id");

            if (guestSessionId is null)
            {
                return AnonymousUser();
            }

            var isActive = await _sessionService.IsGuestSessionActiveAsync(
                guestSessionId.Value,
                cancellationToken);

            if (!isActive)
            {
                return AnonymousUser();
            }

            var displayName = principal.FindFirstValue("display_name")
                ?? "Guest";

            return new CurrentUserResponse
            {
                GuestSessionId = guestSessionId,
                DisplayName = displayName,
                Role = UserRole.Guest
            };
        }

        if (role == UserRole.Employee || role == UserRole.Admin)
        {
            var userId = TryGetGuidClaim(principal, ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                return AnonymousUser();
            }

            var user = await _userService.GetByIdAsync(userId.Value, cancellationToken);

            if (user is null)
            {
                return AnonymousUser();
            }

            return new CurrentUserResponse
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Role = user.Role,
                DepartmentId = user.DepartmentId
            };
        }

        return AnonymousUser();
    }

    public bool IsInternalRole(string role)
    {
        // TODO:
        // 1. Return true neu role la employee hoac admin.
        // 2. Ham nho nay giup AuthService/Policy doc de hon.
        return role is UserRole.Employee or UserRole.Admin;
    }


    ///////////////////////////////////////////////////////
    ///  Ham phu helper
    //////////////////////////////////////////////////////

    // Lay HttpContext
    private HttpContext GetHttpContext()
    {
        return _httpContextAccessor.HttpContext
            ?? throw new UnauthorizedApiException("unauthorized", "Http context is not available.");
    }

    private static UnauthorizedApiException InvalidCredentials()
    {
        return new UnauthorizedApiException("invalid_credentials", "Email or password is incorrect.");
    }

    // Lay IP Address
    private string? GetIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }

    // Doc claim dang Guid
    private static Guid? TryGetGuidClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirstValue(claimType);

        return Guid.TryParse(value, out var id)
            ? id
            : null;
    }

    // Mask Email
    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "unknown";
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var parts = normalizedEmail.Split('@');

        if (parts.Length != 2)
        {
            return "***";
        }

        var localPart = parts[0];
        var domain = parts[1];

        if (localPart.Length <= 1)
        {
            return $"*@{domain}";
        }

        return $"{localPart[0]}***@{domain}";
    }

    // Sign in bang cookie 
    private async Task SignInWithClaimsAsync(List<Claim> claims, DateTimeOffset expiresUtc)
    {
        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = expiresUtc,
            AllowRefresh = true
        };

        await GetHttpContext().SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);
    }

    // Log login fail
    private async Task LogLoginFailedAsync(string? email, string reason, CancellationToken cancellationToken)
    {
        await _auditLogService.LogAsync(new AuditLogEntry
        {
            Action = "login_failed",
            ResourceType = "Auth",
            MetadataJson = JsonSerializer.Serialize(new
            {
                email = MaskEmail(email),
                reason
            }),
            IpAddress = GetIpAddress()
        }, cancellationToken);
    }


    private static CurrentUserResponse AnonymousUser()
    {
        return new CurrentUserResponse
        {
            Role = "anonymous",
            DisplayName = "anonymous"
        };
    }
}
