namespace backend_dotnet.Contracts.Chat;

public sealed class ChatMessageRequest
{
    // TODO:
    // Noi dung user gui len chatbot.
    //
    // Validation nen co trong ChatService hoac controller:
    // - Khong null/rong.
    // - Trim khoang trang.
    // - Gioi han do dai de tranh ton token qua muc.
    public string Message { get; set; } = string.Empty;
}
