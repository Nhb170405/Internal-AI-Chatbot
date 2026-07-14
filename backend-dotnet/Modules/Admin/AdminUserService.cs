using backend_dotnet.Contracts.Admin;
using backend_dotnet.Infrastructure.Persistence;
using backend_dotnet.Modules.Audit;
using backend_dotnet.Modules.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using backend_dotnet.Infrastructure.Errors;
using System.Text.Json;
using System.Security.Claims;

namespace backend_dotnet.Modules.Admin;

public sealed class AdminUserService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly AuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;


    public AdminUserService(
        AppDbContext db,
        IPasswordHasher<AppUser> passwordHasher,
        AuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<List<AdminUserItemResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        return _db.Users
            .AsNoTracking()
            .OrderBy(user => user.Role)
            .ThenBy(user => user.Email)
            .Select(user => new AdminUserItemResponse
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Role = user.Role,
                DepartmentId = user.DepartmentId,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminUserItemResponse> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundApiException("user_not_found", "User not found.");
        }

        return ToResponse(user);
    }

    public async Task<AdminUserItemResponse> CreateEmployeeAsync(CreateEmployeeUserRequest request, CancellationToken cancellationToken = default)
    {
        // Bai tap:
        // 1. Validate request khong null.
        // 2. Normalize email:
        //    - Trim khoang trang dau/cuoi.
        //    - Convert ve lowercase bang ToLowerInvariant().
        // 3. Validate email khong rong.
        // 4. Validate display name:
        //    - Neu rong co the dung phan truoc @ cua email lam ten tam.
        // 5. Validate password:
        //    - Khong rong.
        //    - Do dai toi thieu, vi du 8 ky tu.
        //    - Chua can password policy qua nang trong MVP.
        // 6. Kiem tra email da ton tai chua bang AnyAsync.
        //    - Neu trung, nem ConflictApiException voi code "user_email_exists".
        // 7. Tao AppUser moi:
        //    - Id = Guid.NewGuid().
        //    - Email = email da normalize.
        //    - DisplayName = display name da normalize.
        //    - Role = UserRole.Employee.
        //    - DepartmentId = request.DepartmentId.
        //    - IsActive = true.
        //    - CreatedAt/UpdatedAt = DateTimeOffset.UtcNow.
        // 8. Hash password:
        //    - Goi _passwordHasher.HashPassword(user, request.Password).
        //    - Gan ket qua vao user.PasswordHash.
        // 9. Add user vao _db.Users va SaveChangesAsync.
        // 10. Ghi audit log:
        //    - Action: "admin_user_created".
        //    - ResourceType: "user".
        //    - ResourceId: user.Id.
        //    - Metadata chi gom thong tin an toan: createdUserId, email, role.
        //    - Khong log password/plain text/token/cookie.
        // 11. Return AdminUserItemResponse.
        //
        // Luu y thiet ke:
        // - Controller da co [Authorize(Roles = UserRole.Admin)] de chan non-admin.
        // - Service van nen giu logic "chi tao employee", khong tin role tu frontend.
        // - Giai doan production co the them "owner/super_admin" de tao admin moi.

        if (request is null)
        {
            throw new ValidationApiException("invalid_request", "Create employee request is required.");
        }

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationApiException("email_required", "Email is required.");
        }
        if (!email.Contains('@'))
        {
            throw new ValidationApiException("invalid_email", "Email format is invalid.");
        }

        var displayName = (request.DisplayName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = email.Split('@')[0];
        }

        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationApiException("password_required", "Password is required.");
        }
        if (password.Length < 8)
        {
            throw new ValidationApiException("password_too_short", "Password must be at least 8 characters.");
        }

        var emailExists = await _db.Users
            .AnyAsync(user => user.Email == email, cancellationToken);

        if (emailExists)
        {
            throw new ConflictApiException("user_email_exists", "A user with this email already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            Role = UserRole.Employee,
            DepartmentId = request.DepartmentId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var actorUserIdText = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        Guid? actorUserId = Guid.TryParse(actorUserIdText, out var parsed) ? parsed : null;

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            Action = "admin_user_created",
            ActorUserId = actorUserId,
            ResourceType = "User",
            ResourceId = user.Id.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                createdUserId = user.Id,
                email = user.Email,
                role = user.Role,
                departmentId = user.DepartmentId
            })
        }, cancellationToken);

        return ToResponse(user);
    }

    private static AdminUserItemResponse ToResponse(AppUser user)
    {
        return new AdminUserItemResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            DepartmentId = user.DepartmentId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
