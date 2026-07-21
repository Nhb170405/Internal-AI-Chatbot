namespace backend_dotnet.Infrastructure.OpenAI;

public sealed class OpenAIChatMessage
{
    // TODO:
    // Role hop le cho chat co ban:
    // - system
    // - user
    // - assistant
    public string Role { get; set; } = string.Empty;

    // TODO:
    // Noi dung message gui len OpenAI.
    public string? Content { get; set; } = string.Empty;

    public string? ToolCallId { get; set; }

    public IReadOnlyList<OpenAIToolCall>? ToolCalls { get; set; }
}
