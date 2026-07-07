namespace backend_dotnet.Modules.Sessions;

public sealed class GuestSession
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = "Guest";

    // TODO: Chuoi random de dai dien cho guest session neu can doi chieu.
    // Khong dung thong tin de doan nhu email/IP.
    public string SessionKey { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public bool IsActive { get; set; } = true;
}
