using System.Security.Cryptography;
using backend_dotnet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Modules.Sessions;

public sealed class SessionService
{
    private readonly AppDbContext _db;
    public SessionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GuestSession> CreateGuestSessionAsync(string? displayName, CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Neu displayName rong thi dat mac dinh la "Guest".
        // 2. Tao GuestSession.Id = Guid moi.
        // 3. Tao SessionKey bang random an toan.
        // 4. Dat ExpiresAt, vi du hien tai + 8 gio.
        // 5. Luu vao SQL Server.
        // 6. Return GuestSession da tao.
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Guest";
        }

        var guestSession = new GuestSession
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName,

            // 3. Tao SessionKey bang random an toan.
            SessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_'),

            ExpiresAt = DateTimeOffset.UtcNow.AddHours(8),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.GuestSessions.Add(guestSession);
        await _db.SaveChangesAsync(cancellationToken);
        return guestSession;
    }

    public async Task<bool> IsGuestSessionActiveAsync(Guid guestSessionId, CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Tim GuestSession trong SQL Server.
        // 2. Neu khong ton tai thi return false.
        // 3. Neu ExpiresAt < thoi gian hien tai thi return false.
        // 4. Nguoc lai return true.
        if (guestSessionId == Guid.Empty)
        {
            return false;
        }

        var guestSession = await _db.GuestSessions.AsNoTracking().FirstOrDefaultAsync(gs => gs.Id == guestSessionId, cancellationToken);
        if (guestSession == null)
        {
            return false;
        }

        if (guestSession.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        if (guestSession.IsActive == false)
        {
            return false;
        }

        return true;
    }
}
