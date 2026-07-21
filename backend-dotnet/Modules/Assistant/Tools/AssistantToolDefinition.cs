using System.Text.Json;

namespace backend_dotnet.Modules.Assistant.Tools;

// Mo ta mot function tool theo JSON schema ma OpenAI Chat Completions chap nhan.
// Lop nay chi la metadata; code thuc thi nam trong IAssistantTool.ExecuteAsync.
public sealed class AssistantToolDefinition
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public JsonElement Parameters { get; init; }

    // Strict=true giup model tao arguments dung schema hon.
    public bool Strict { get; init; } = true;
}
