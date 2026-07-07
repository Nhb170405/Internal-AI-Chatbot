using backend_dotnet.Contracts.Admin;
using backend_dotnet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Modules.Admin;

public sealed class AdminUserService
{
    private readonly AppDbContext _db;

    public AdminUserService(AppDbContext db)
    {
        _db = db;
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
}
