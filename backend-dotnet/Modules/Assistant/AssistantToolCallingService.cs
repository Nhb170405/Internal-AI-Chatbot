using System.Text.Json;
using backend_dotnet.Infrastructure.OpenAI;
using backend_dotnet.Modules.Assistant.Tools;

namespace backend_dotnet.Modules.Assistant;

public sealed class AssistantToolCallingService
{
    private readonly OpenAIClient _openAIClient;
    private readonly AssistantToolExecutor _toolExecutor;

    public AssistantToolCallingService(
        OpenAIClient openAIClient,
        AssistantToolExecutor toolExecutor)
    {
        _openAIClient = openAIClient;
        _toolExecutor = toolExecutor;
    }

    public async Task<OpenAIChatResult> SendAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new ArgumentException("User message is required.", nameof(userMessage));
        }

        var messages = new List<OpenAIChatMessage>
        {
            new()
            {
                Role = "system",
                Content =
                    "You are an internal company assistant. " +
                    "Use an available tool when the user asks about internal documents."
            },
            new()
            {
                Role = "user",
                Content = userMessage.Trim()
            }
        };

        const int maxToolSteps = 5;

        for (var step = 0; step < maxToolSteps; step++)
        {
            var result = await _openAIClient.SendChatAsync(
                messages,
                _toolExecutor.Definitions,
                cancellationToken);

            if (!result.HasToolCalls)
            {
                return result;
            }

            // Protocol yeu cau gui lai assistant message chua tool_calls truoc
            // cac message role="tool" tuong ung.
            messages.Add(new OpenAIChatMessage
            {
                Role = "assistant",
                Content = string.IsNullOrWhiteSpace(result.Answer)
                    ? null
                    : result.Answer,
                ToolCalls = result.ToolCalls
            });

            // Mot response co the yeu cau nhieu tool. Moi tool_call_id phai co
            // dung mot tool-result message truoc lan goi model tiep theo.
            foreach (var toolCall in result.ToolCalls)
            {
                var toolResult = await _toolExecutor.ExecuteAsync(
                    toolCall.Name,
                    toolCall.ArgumentsJson,
                    cancellationToken);

                messages.Add(new OpenAIChatMessage
                {
                    Role = "tool",
                    ToolCallId = toolCall.Id,
                    Content = JsonSerializer.Serialize(toolResult)
                });
            }
        }

        throw new InvalidOperationException(
            $"Assistant exceeded the maximum of {maxToolSteps} tool-calling steps.");
    }
}
