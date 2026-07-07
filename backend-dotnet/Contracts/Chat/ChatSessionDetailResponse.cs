namespace backend_dotnet.Contracts.Chat;

public sealed class ChatSessionDetailResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<ChatMessageItemResponse> Messages { get; set; } = new();
}
