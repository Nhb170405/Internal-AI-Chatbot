namespace backend_dotnet.Modules.Users;

public sealed class AppUser
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty; // lưu lowercase email đã được trim để dễ tìm kiếm và tránh trùng lặp do khác biệt về chữ hoa chữ thường hoặc khoảng trắng.

    public string DisplayName { get; set; } = string.Empty;

    // TODO: Chi nhan employee/admin trong bang Users.
    // Guest nen nam o GuestSessions de tranh tao user rac.
    public string Role { get; set; } = UserRole.Employee;

    public Guid? DepartmentId { get; set; }

    // TODO: Luu password da hash, khong bao gio luu plain text.
    public string? PasswordHash { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
