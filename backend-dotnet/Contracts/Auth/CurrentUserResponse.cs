namespace backend_dotnet.Contracts.Auth;

public sealed class CurrentUserResponse
{
    // TODO: UserId chi co khi la employee/admin.
    public Guid? UserId { get; set; }

    // TODO: GuestSessionId chi co khi la guest.
    public Guid? GuestSessionId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }

    // TODO: Gia tri hop le: anonymous, guest, employee, admin.
    public string Role { get; set; } = "anonymous";

    public Guid? DepartmentId { get; set; }

    // TODO: Dung cho guest session hoac cookie expiration neu can hien thi tren UI.
    public DateTimeOffset? ExpiresAt { get; set; }
}
