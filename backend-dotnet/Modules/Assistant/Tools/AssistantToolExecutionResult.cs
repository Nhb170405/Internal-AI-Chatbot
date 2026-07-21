using System.Text.Json;

namespace backend_dotnet.Modules.Assistant.Tools;

// Ket qua noi bo cua mot tool. Orchestrator se serialize Output va gui lai OpenAI
// trong message role="tool" kem dung ToolCallId.
public sealed class AssistantToolExecutionResult
{
    public bool Success { get; init; }

    public JsonElement Output { get; init; }

    public static AssistantToolExecutionResult Ok<T>(T value) => new()
    {
        Success = true,
        Output = JsonSerializer.SerializeToElement(value)
    };
}
