using System.Text.Json;
using backend_dotnet.Contracts.Assistant;
using backend_dotnet.Contracts.Chat;
using backend_dotnet.Contracts.Rag;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Modules.Audit;
using backend_dotnet.Modules.Chat;
using backend_dotnet.Modules.Rag;
using Microsoft.Extensions.Options;

namespace backend_dotnet.Modules.Assistant;

public sealed class AssistantService
{
    private readonly AssistantRouter _router;
    private readonly ChatService _chatService;
    private readonly RagService _ragService;
    private readonly AssistantDatasetProfileHandler _datasetProfileHandler;
    private readonly AuditLogService _auditLogService;
    private readonly AssistantToolCallingService _toolCallingService;
    private readonly AssistantOptions _assistantOptions;

    public AssistantService(
        AssistantRouter router,
        ChatService chatService,
        RagService ragService,
        AssistantDatasetProfileHandler datasetProfileHandler,
        AuditLogService auditLogService,
        AssistantToolCallingService toolCallingService,
        IOptions<AssistantOptions> assistantOptions)
    {
        _router = router;
        _chatService = chatService;
        _ragService = ragService;
        _datasetProfileHandler = datasetProfileHandler;
        _auditLogService = auditLogService;
        _toolCallingService = toolCallingService;
        _assistantOptions = assistantOptions.Value;
    }

    public async Task<AssistantChatResponse> SendAsync(AssistantChatRequest request, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 13:
        // 1. Validate request khong null.
        // 2. Validate Message khong rong va gioi han do dai.
        // 3. Goi _router.Decide(message).
        // 4. Ghi audit log assistant_route_decided.
        // 5. Switch theo decision.Route:
        //    - Chitchat: goi ChatService.SendMessageAsync.
        //    - Rag: goi RagService.SendAsync.
        //    - DatasetProfile: tam thoi tra response huong user sang Dataset page.
        //    - DatasetAnalyze: tam thoi tra response huong user sang Dataset page.
        //    - Chart: tam thoi tra response huong user sang Charts page.
        //    - Unsupported: tra loi an toan.
        // 6. Map response ve AssistantChatResponse.
        //
        // Luu y:
        // - AssistantService khong tu goi OpenAI truc tiep.
        // - No dieu phoi cac service da co.
        // - Khong log full message vao audit.

        if (request is null)
        {
            throw new ValidationApiException("invalid_assistant_request", "Assistant request is required.");
        }

        var message = request.Message?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ValidationApiException("invalid_assistant_request", "Message is required.");
        }

        if (message.Length > 4000)
        {
            throw new ValidationApiException("invalid_assistant_request", "Message is too long.");
        }

        if (_assistantOptions.ToolCallingEnabled)
        {
            return await HandleToolCallingAsync(
                message,
                cancellationToken);
        }

        var topK = request.TopK <= 0 ? 3 : Math.Min(request.TopK, 10);
        var decision = _router.Decide(message);

        await LogRouteDecisionAsync(decision, message.Length, topK, cancellationToken);

        return decision.Route switch
        {
            AssistantRoute.Chitchat => await HandleChitchatAsync(message, cancellationToken),
            AssistantRoute.Rag => await HandleRagAsync(message, topK, cancellationToken),
            AssistantRoute.DatasetProfile => await _datasetProfileHandler.HandleAsync(
                message,
                decision.DocumentHint,
                cancellationToken),
            AssistantRoute.DatasetAnalyze => BuildNeedsActionResponse(
                AssistantRoute.DatasetAnalyze,
                "Cau hoi nay phu hop voi tinh nang phan tich du lieu bang. Hay mo trang Datasets de chon file, cot va phep tinh.",
                "open_datasets_page",
                decision.DocumentHint),
            AssistantRoute.Chart => BuildNeedsActionResponse(
                AssistantRoute.Chart,
                "Cau hoi nay phu hop voi tinh nang tao bieu do. Hay mo trang Charts de chon file va cau hinh bieu do.",
                "open_charts_page",
                decision.DocumentHint),
            _ => new AssistantChatResponse
            {
                Route = AssistantRoute.Unsupported,
                Answer = "Toi chua hieu can xu ly cau hoi nay bang cong cu nao. Ban hay noi ro hon hoac thu cau hoi khac.",
                Warnings = ["unsupported_route"]
            }
        };
    }

    private async Task<AssistantChatResponse> HandleChitchatAsync(string message, CancellationToken cancellationToken)
    {
        var chatResponse = await _chatService.SendMessageAsync(new ChatMessageRequest
        {
            Message = message
        }, cancellationToken);

        return new AssistantChatResponse
        {
            Route = AssistantRoute.Chitchat,
            Answer = chatResponse.Answer,
            Model = chatResponse.Model,
            PromptTokens = chatResponse.PromptTokens,
            CompletionTokens = chatResponse.CompletionTokens,
            TotalTokens = chatResponse.TotalTokens
        };
    }

    private async Task<AssistantChatResponse> HandleRagAsync(string message, int topK, CancellationToken cancellationToken)
    {
        var ragResponse = await _ragService.SendAsync(new RagChatRequest
        {
            Question = message,
            TopK = topK
        }, cancellationToken);

        return new AssistantChatResponse
        {
            Route = AssistantRoute.Rag,
            Answer = ragResponse.Answer,
            Model = ragResponse.Model,
            PromptTokens = ragResponse.PromptTokens,
            CompletionTokens = ragResponse.CompletionTokens,
            TotalTokens = ragResponse.TotalTokens,
            Citations = ragResponse.Citations.Select(citation => new AssistantCitationResponse
            {
                DocumentId = citation.DocumentId,
                ChunkId = citation.ChunkId,
                ChunkIndex = citation.ChunkIndex,
                Score = citation.Score,
                Snippet = citation.Snippet,
                PageNumber = citation.PageNumber
            }).ToList()
        };
    }

    private static AssistantChatResponse BuildNeedsActionResponse(
        string route,
        string answer,
        string suggestedAction,
        string? documentHint)
    {
        return new AssistantChatResponse
        {
            Route = route,
            Answer = answer,
            NeedsUserAction = true,
            SuggestedAction = suggestedAction,
            Data = JsonSerializer.SerializeToElement(new
            {
                documentHint
            })
        };
    }

    private async Task LogRouteDecisionAsync(
        AssistantRouteDecision decision,
        int messageLength,
        int topK,
        CancellationToken cancellationToken)
    {
        await _auditLogService.LogAsync(new AuditLogEntry
        {
            Action = "assistant_route_decided",
            ResourceType = "Assistant",
            MetadataJson = JsonSerializer.Serialize(new
            {
                route = decision.Route,
                decision.Confidence,
                decision.Reason,
                decision.DocumentHint,
                messageLength,
                topK
            })
        }, cancellationToken);
    }

    private async Task<AssistantChatResponse> HandleToolCallingAsync(
    string message,
    CancellationToken cancellationToken)
    {
        var result = await _toolCallingService.SendAsync(
            message,
            cancellationToken);

        return new AssistantChatResponse
        {
            Route = AssistantRoute.ToolCalling,
            Answer = result.ChatResult.Answer,
            Model = result.ChatResult.Model,
            PromptTokens = result.ChatResult.PromptTokens,
            CompletionTokens = result.ChatResult.CompletionTokens,
            TotalTokens = result.ChatResult.TotalTokens,
            Citations = result.Citations.Select(citation => new AssistantCitationResponse
            {
                DocumentId = citation.DocumentId,
                ChunkId = citation.ChunkId,
                ChunkIndex = citation.ChunkIndex,
                Score = citation.Score,
                Snippet = citation.Snippet,
                PageNumber = citation.PageNumber
            }).ToList()
        };
    }
}
