namespace backend_dotnet.Contracts.Chat;

public sealed class ChatMessageItemResponse
{
    public Guid Id { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
