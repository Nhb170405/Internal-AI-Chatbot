namespace backend_dotnet.Modules.Chat;

public sealed class ChatMessage
{
    public Guid Id { get; set; }

    public Guid ChatSessionId { get; set; }

    // TODO:
    // Gia tri hop le trong Milestone 3:
    // - user
    // - assistant
    public string Role { get; set; } = string.Empty;

    // TODO:
    // Noi dung chat duoc luu trong chat history.
    // Khac audit log: chat history duoc dung de xem lai/debug chat, nen co the luu content.
    // Sau nay can phan quyen va retention policy.
    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public ChatSession? ChatSession { get; set; }
}
