namespace backend_dotnet.Contracts.Rag;

public sealed class RagChatResponse
{
    public string Answer { get; set; } = string.Empty;

    public List<CitationDto> Citations { get; set; } = [];

    public string Model { get; set; } = string.Empty;

    // OpenAI tra token usage, minh chi luu/count token.
    // Chua tinh thanh tien trong Milestone 8.
    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public int? TotalTokens { get; set; }
}
