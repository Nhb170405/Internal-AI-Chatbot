namespace backend_dotnet.Contracts.Admin;

public sealed class CreateEmployeeUserRequest
{
    // Email dung de dang nhap.
    // Khi xu ly trong service can trim + lowercase de tranh trung tai khoan do khac chu hoa/chu thuong.
    public string Email { get; set; } = string.Empty;

    // Ten hien thi trong giao dien admin/chat.
    // Khong nen lay email lam display name neu nguoi dung da nhap ten rieng.
    public string DisplayName { get; set; } = string.Empty;

    // Mat khau tam thoi do admin cap cho employee.
    // Service phai hash password truoc khi luu, khong bao gio luu plain text.
    public string Password { get; set; } = string.Empty;

    // Giai doan MVP chua bat buoc phong ban.
    // Sau nay neu lam permission theo phong ban thi co the gan DepartmentId tai day.
    public Guid? DepartmentId { get; set; }
}
