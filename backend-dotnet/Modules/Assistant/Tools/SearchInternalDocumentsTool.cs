using System.Text.Json;
using backend_dotnet.Contracts.Rag;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Modules.Rag;

namespace backend_dotnet.Modules.Assistant.Tools;

// Tool dau tien nen trien khai vi no read-only va tai su dung toan bo auth/RAG flow hien co.
public sealed class SearchInternalDocumentsTool : IAssistantTool
{
    private readonly RagService _ragService;

    public SearchInternalDocumentsTool(RagService ragService)
    {
        _ragService = ragService;
    }

    public AssistantToolDefinition Definition { get; } = new()
    {
        Name = "search_internal_documents",
        Description = "Search unstructured internal documents such as PDF, DOCX and TXT. Do not use this tool for exact calculations over CSV or Excel datasets. Use analyze_dataset for sums, averages, counts, grouping and top-N queries.",
        Parameters = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                question = new
                {
                    type = "string",
                    description = "The user's question about internal documents."
                },
                topK = new
                {
                    type = new[] { "integer", "null" },
                    description = "Number of relevant chunks. Use 8-10 for exhaustive list, summary, achievement, policy, requirement, or 'all' questions.",
                    minimum = 1,
                    maximum = 10
                }
            },
            required = new[] { "question", "topK" },
            additionalProperties = false
        })
    };

    public async Task<AssistantToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        SearchInternalDocumentsArguments? arguments;

        try
        {
            arguments = JsonSerializer.Deserialize<SearchInternalDocumentsArguments>(
                argumentsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            throw new ValidationApiException(
                "invalid_tool_arguments",
                "search_internal_documents received invalid JSON arguments.");
        }

        if (string.IsNullOrWhiteSpace(arguments?.Question))
        {
            throw new ValidationApiException(
                "invalid_tool_arguments",
                "search_internal_documents requires a question.");
        }

        // TODO: Neu sau nay tool nhan documentIds/filter, validate quyen doc tai day
        // hoac trong RagService. Khong tin tuong ID/filter do model tu tao.
        var response = await _ragService.SendAsync(new RagChatRequest
        {
            Question = arguments.Question.Trim(),
            TopK = Math.Clamp(arguments.TopK ?? 10, 1, 10)
        }, cancellationToken);

        return AssistantToolExecutionResult.Ok(new
        {
            response.Answer,
            response.Citations,
            response.Model,
            response.PromptTokens,
            response.CompletionTokens,
            response.TotalTokens
        });
    }

    private sealed class SearchInternalDocumentsArguments
    {
        public string Question { get; set; } = string.Empty;

        public int? TopK { get; set; }
    }
}
