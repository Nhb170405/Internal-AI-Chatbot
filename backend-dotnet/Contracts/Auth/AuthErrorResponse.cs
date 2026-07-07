namespace backend_dotnet.Contracts.Auth;

public sealed class AuthErrorResponse
{
    // TODO: Ma loi ngan gon cho frontend xu ly.
    // Vi du: invalid_credentials, inactive_user, unauthorized.
    public string Code { get; set; } = string.Empty;

    // TODO: Thong bao an toan cho nguoi dung.
    // Khong nen noi ro "email dung nhung password sai" vi de lo thong tin account.
    public string Message { get; set; } = string.Empty;
}
