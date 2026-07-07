namespace backend_dotnet.Contracts.Chat;

public sealed class ChatMessageResponse
{
    // TODO:
    // Cau tra loi cuoi cung tra ve frontend.
    public string Answer { get; set; } = string.Empty;

    // TODO:
    // Model OpenAI da su dung, vi du: gpt-4.1-mini.
    public string Model { get; set; } = string.Empty;

    // TODO:
    // Token user/system prompt gui len OpenAI.
    public int? PromptTokens { get; set; }

    // TODO:
    // Token OpenAI sinh ra trong cau tra loi.
    public int? CompletionTokens { get; set; }

    // TODO:
    // Tong token = prompt + completion neu OpenAI response co usage.
    public int? TotalTokens { get; set; }
}
