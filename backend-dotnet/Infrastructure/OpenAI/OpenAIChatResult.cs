namespace backend_dotnet.Infrastructure.OpenAI;

public sealed class OpenAIChatResult
{
    // TODO:
    // Cau tra loi lay tu OpenAI response.
    public string Answer { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public int? TotalTokens { get; set; }
}
