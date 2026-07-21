namespace backend_dotnet.Infrastructure.OpenAI;

// DTO trung gian cho tool_calls model tra ve.
// OpenAIClient map response JSON vao DTO nay; orchestrator dung no de chon va goi tool.
public sealed class OpenAIToolCall
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    // Arguments la JSON string do model sinh ra; luon deserialize + validate truoc khi dung.
    public string ArgumentsJson { get; set; } = "{}";
}
