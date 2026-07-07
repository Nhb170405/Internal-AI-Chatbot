namespace backend_dotnet.Contracts.Auth;

public sealed class GuestLoginRequest
{
    // TODO: Ten hien thi ma guest nhap tren man hinh login.
    // Co the null hoac rong, khi do AuthService se tu dat ten mac dinh.
    public string? DisplayName { get; set; }
}
