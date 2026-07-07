using backend_dotnet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Modules.Users;

public sealed class UserService
{
    // TODO: Sau khi co AppDbContext, inject vao constructor.
    private readonly AppDbContext _db;
    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // TODO:    
        // 1. Trim email.
        // 2. Chuyen ve lowercase/normalized email.
        // 3. Tim user trong SQL Server theo email.
        // 4. Chi tra ve user active.
        // 5. Neu khong thay thi return null.

        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult<AppUser?>(null);
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        return _db.Users
            .Where(u => u.Email == normalizedEmail && u.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Tim user theo Id.
        // 2. Neu user inactive thi nen return null.
        // 3. Ham nay duoc AuthService dung cho /api/auth/me.

        return _db.Users
            .Where(u => u.Id == userId && u.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
