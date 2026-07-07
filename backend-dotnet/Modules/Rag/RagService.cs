using System.Security.Claims;
using System.Text.Json;
using backend_dotnet.Contracts.Rag;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Infrastructure.OpenAI;
using backend_dotnet.Modules.Audit;
using backend_dotnet.Modules.Documents;
using backend_dotnet.Modules.Users;

namespace backend_dotnet.Modules.Rag;

public sealed class RagService
{
    private readonly DocumentIndexingService _documentIndexingService;
    private readonly OpenAIClient _openAIClient;
    private readonly PromptBuilder _promptBuilder;
    private readonly AuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RagService(
        DocumentIndexingService documentIndexingService,
        OpenAIClient openAIClient,
        PromptBuilder promptBuilder,
        AuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor)
    {
        _documentIndexingService = documentIndexingService;
        _openAIClient = openAIClient;
        _promptBuilder = promptBuilder;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<RagChatResponse> SendAsync(RagChatRequest request, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 8:
        // 1. Lay principal tu HttpContext.
        // 2. Check authenticated.
        // 3. Validate request.Question:
        //    - khong rong.
        //    - gioi han do dai, vi du 4000 ky tu.
        // 4. Validate TopK:
        //    - neu <= 0 thi dung default 5.
        //    - gioi han toi da 10 hoac 20 de tranh ton token.
        // 5. Goi _documentIndexingService.SearchAsync(question, topK).
        //    - Ham nay da tu check role va filter accessLevel.
        // 6. Neu search success=false hoac khong co result:
        //    - Return answer an toan: "Khong tim thay thong tin trong tai lieu noi bo."
        //    - Citations rong.
        //    - Token usage null/0 vi chua goi OpenAI Chat.
        // 7. Goi _promptBuilder.Build(question, searchResults).
        // 8. Goi _openAIClient.SendChatAsync(prompt.Messages).
        // 9. Tao citations tu searchResults:
        //    - moi result thanh CitationDto.
        //    - snippet cat ngan.
        // 10. Ghi audit log:
        //    - action = "rag_chat_message".
        //    - khong log full question.
        //    - khong log full chunks.
        //    - log questionLength, topK, citationCount, model, token usage.
        // 11. Return RagChatResponse:
        //    - Answer.
        //    - Citations.
        //    - Model.
        //    - PromptTokens.
        //    - CompletionTokens.
        //    - TotalTokens.
        //
        // Luu y:
        // - Milestone 8 chi count token, chua tinh tien.
        // - Citation co the nhieu document, khong ep ve mot source duy nhat.
        // - Neu OpenAI loi, controller se map InvalidOperationException sang 502.

        var principal = GetCurrentPrincipal();

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Unauthorized.");
        }

        if (request is null)
        {
            throw new ValidationApiException("invalid_rag_request", "RAG request is required.");
        }

        var topK = request.TopK <= 0 ? 5 : request.TopK;

        if (topK > 10)
        {
            throw new ValidationApiException("invalid_rag_request", "TopK must be less than or equal to 10.");
        }

        var question = request.Question?.Trim();

        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ValidationApiException("invalid_rag_request", "Question is required.");
        }

        if (question.Length > 4000)
        {
            throw new ValidationApiException("invalid_rag_request", "Question is too long.");
        }

        var searchResponse = await _documentIndexingService.SearchAsync(question, topK, cancellationToken);

        if (!searchResponse.Success || searchResponse.Results.Count == 0)
        {
            return new RagChatResponse
            {
                Answer = "Tôi không tìm thấy thông tin này trong tài liệu nội bộ.",
                Citations = [],
                Model = string.Empty,
                PromptTokens = 0,
                CompletionTokens = 0,
                TotalTokens = 0
            };
        }

        var prompt = _promptBuilder.Build(question, searchResponse.Results);
        OpenAIChatResult openAIResult;

        try
        {
            openAIResult = await _openAIClient.SendChatAsync(prompt.Messages, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw new ExternalServiceApiException("openai_service_error", "OpenAI chat service error.");
        }

        var citations = BuildCitations(searchResponse.Results);

        var role = GetRole(principal);
        var userId = GetCurrentUserId(principal);
        var guestSessionId = GetCurrentGuestSessionId(principal);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            ActorUserId = userId,
            ActorGuestSessionId = guestSessionId,
            Action = "rag_chat_message",
            ResourceType = "RagChat",
            MetadataJson = JsonSerializer.Serialize(new
            {
                role,
                questionLength = question.Length,
                topK,
                citationCount = citations.Count,
                model = openAIResult.Model,
                promptTokens = openAIResult.PromptTokens,
                completionTokens = openAIResult.CompletionTokens,
                totalTokens = openAIResult.TotalTokens
            })
        }, cancellationToken);

        return new RagChatResponse
        {
            Answer = openAIResult.Answer,
            Citations = citations,
            Model = openAIResult.Model,
            PromptTokens = openAIResult.PromptTokens,
            CompletionTokens = openAIResult.CompletionTokens,
            TotalTokens = openAIResult.TotalTokens
        };

    }

    private ClaimsPrincipal GetCurrentPrincipal()
    {
        return _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedApiException("unauthorized", "Unauthorized.");
    }

    private static string GetRole(ClaimsPrincipal principal)
    {
        var role = principal.FindFirstValue(ClaimTypes.Role);

        if (role == UserRole.Admin)
        {
            return UserRole.Admin;
        }

        if (role == UserRole.Employee)
        {
            return UserRole.Employee;
        }

        if (role == UserRole.Guest)
        {
            return UserRole.Guest;
        }

        return "anonymous";
    }

    private static Guid? GetCurrentUserId(ClaimsPrincipal principal)
    {
        return TryGetGuidClaim(principal, ClaimTypes.NameIdentifier);
    }

    private static Guid? GetCurrentGuestSessionId(ClaimsPrincipal principal)
    {
        return TryGetGuidClaim(principal, "guest_session_id");
    }

    private static Guid? TryGetGuidClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirstValue(claimType);

        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static List<CitationDto> BuildCitations(IReadOnlyList<Contracts.Documents.DocumentSearchResultItem> results)
    {
        // Bai tap:
        // 1. Map tung search result sang CitationDto.
        // 2. Citation phai giu chunk-level tracking:
        //    - DocumentId.
        //    - ChunkId.
        //    - ChunkIndex.
        //    - Score.
        //    - Snippet.
        // 3. PageNumber de null trong Milestone 8 neu chua co metadata.
        var citations = new List<CitationDto>();

        if (results == null)
        {
            throw new ValidationApiException("invalid_rag_context", "Search results are required.");
        }

        foreach (var result in results)
        {
            var citation = new CitationDto
            {
                DocumentId = result.DocumentId,
                ChunkId = result.ChunkId,
                ChunkIndex = result.ChunkIndex,
                Score = result.Score,
                Snippet = BuildSnippet(result.Content),
                PageNumber = null
            };

            citations.Add(citation);
        }

        return citations;
    }

    private static string BuildSnippet(string content, int maxLength = 300)
    {
        // Bai tap:
        // 1. Trim content.
        // 2. Neu content ngan hon maxLength thi return.
        // 3. Neu dai hon thi cat va them "...".
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var cleanedContent = content.Trim();

        if (cleanedContent.Length <= maxLength)
        {
            return cleanedContent;
        }

        return cleanedContent[..maxLength] + "...";
    }
}
