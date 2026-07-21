using backend_dotnet.Infrastructure.Errors;

namespace backend_dotnet.Modules.Assistant.Tools;

// Registry + dispatcher cho tat ca tool da dang ky trong DI.
// Khong bao gio dung reflection de goi ten ham do model gui len.
public sealed class AssistantToolExecutor
{
    private readonly IReadOnlyDictionary<string, IAssistantTool> _tools;

    public AssistantToolExecutor(IEnumerable<IAssistantTool> tools)
    {
        _tools = tools.ToDictionary(
            tool => tool.Definition.Name,
            StringComparer.Ordinal);
    }

    public IReadOnlyList<AssistantToolDefinition> Definitions =>
        _tools.Values.Select(tool => tool.Definition).ToList();

    public Task<AssistantToolExecutionResult> ExecuteAsync(
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            throw new ValidationApiException(
                "unknown_assistant_tool",
                $"Assistant requested an unsupported tool: {toolName}.");
        }

        return tool.ExecuteAsync(argumentsJson, cancellationToken);
    }
}
