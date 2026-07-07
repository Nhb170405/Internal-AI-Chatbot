namespace backend_dotnet.Contracts.Auth;

public sealed class LoginRequest
{
    // TODO: Email cua employee/admin.
    // Can validate: bat buoc nhap, dung format email, trim khoang trang.
    public string Email { get; set; } = string.Empty;

    // TODO: Mat khau dang nhap.
    // Tuyet doi khong log field nay ra console, file log, audit log.
    public string Password { get; set; } = string.Empty;
}
