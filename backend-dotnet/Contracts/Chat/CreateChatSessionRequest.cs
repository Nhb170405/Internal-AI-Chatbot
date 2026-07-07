namespace backend_dotnet.Contracts.Chat;

public sealed class CreateChatSessionRequest
{
    // TODO:
    // Optional title do frontend gui len.
    // Neu null/rong thi ChatHistoryService dat mac dinh "New chat".
    public string? Title { get; set; }
}
