using backend_dotnet.Infrastructure.OpenAI;

namespace backend_dotnet.Modules.Rag;

public sealed class RagPrompt
{
    // Danh sach messages se gui vao OpenAI Chat API.
    public List<OpenAIChatMessage> Messages { get; set; } = [];
}
