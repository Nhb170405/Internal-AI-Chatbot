namespace backend_dotnet.Modules.Assistant.Tools;

// Moi tool gom hai phan:
// 1. Definition de gui cho model.
// 2. ExecuteAsync de backend thuc thi sau khi model yeu cau goi tool.
public interface IAssistantTool
{
    AssistantToolDefinition Definition { get; }

    Task<AssistantToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default);
}
