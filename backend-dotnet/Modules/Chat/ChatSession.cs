namespace backend_dotnet.Modules.Chat;

public sealed class ChatSession
{
    public Guid Id { get; set; }

    // TODO:
    // UserId co gia tri khi session thuoc employee/admin.
    // GuestSessionId co gia tri khi session thuoc guest.
    // Chi mot trong hai field nay nen co gia tri.
    public Guid? UserId { get; set; }

    public Guid? GuestSessionId { get; set; }

    // TODO:
    // Title co the do user dat, hoac lay tu message dau tien sau nay.
    public string Title { get; set; } = "New chat";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    // TODO:
    // Navigation properties cho EF Core.
    public List<ChatMessage> Messages { get; set; } = new();

    public List<TokenUsage> TokenUsages { get; set; } = new();
}
