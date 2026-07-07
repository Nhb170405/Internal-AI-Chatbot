namespace backend_dotnet.Infrastructure.OpenAI;

public sealed class OpenAIOptions
{
    // TODO:
    // Base URL cua OpenAI API.
    // Gia tri mac dinh hop ly: https://api.openai.com/v1
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    // TODO:
    // API key khong duoc hard-code trong C#.
    // Nen lay tu appsettings.Development.json, user secrets, hoac environment variable.
    public string ApiKey { get; set; } = string.Empty;

    // TODO:
    // Model chat mac dinh.
    // Vi du: gpt-4.1-mini.
    public string ChatModel { get; set; } = string.Empty;

    // TODO:
    // System prompt ngan gon cho Milestone 2.
    // Chua dua RAG/tool rules dai vao day.
    public string SystemPrompt { get; set; } = "You are a helpful internal company assistant. Answer clearly and concisely.";
}
