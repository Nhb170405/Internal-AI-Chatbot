using System.Text.Json;
using backend_dotnet.Contracts.Rag;
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

    public async Task<AssistantToolCallingResult> SendAsync(
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
                    "For exact CSV or Excel calculations, always use analyze_dataset. " +
                    "Never calculate totals, averages, counts, groupings, previews, or top-N values " +
                    "from document-search snippets or sample rows. " +
                    "Use search_internal_documents only for unstructured document questions."
            },
            new()
            {
                Role = "user",
                Content = userMessage.Trim()
            }
        };

        const int maxToolSteps = 5;
        var citations = new Dictionary<Guid, CitationDto>();

        for (var step = 0; step < maxToolSteps; step++)
        {
            var result = await _openAIClient.SendChatAsync(
                messages,
                _toolExecutor.Definitions,
                cancellationToken);

            if (!result.HasToolCalls)
            {
                return new AssistantToolCallingResult
                {
                    ChatResult = result,
                    Citations = citations.Values.ToList()
                };
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

                CollectCitations(toolCall.Name, toolResult, citations);

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

    private static void CollectCitations(
        string toolName,
        AssistantToolExecutionResult toolResult,
        IDictionary<Guid, CitationDto> citations)
    {
        if (!toolResult.Success ||
            toolName != "search_internal_documents" ||
            toolResult.Output.ValueKind != JsonValueKind.Object ||
            !toolResult.Output.TryGetProperty("Citations", out var citationJson) ||
            citationJson.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var toolCitations = citationJson.Deserialize<List<CitationDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        foreach (var citation in toolCitations)
        {
            citations[citation.ChunkId] = citation;
        }
    }
}
