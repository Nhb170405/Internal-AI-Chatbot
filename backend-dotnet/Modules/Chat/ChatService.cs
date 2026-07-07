using System.Security.Claims;
using System.Text.Json;
using backend_dotnet.Contracts.Chat;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Infrastructure.OpenAI;
using backend_dotnet.Modules.Audit;
using backend_dotnet.Modules.Users;

namespace backend_dotnet.Modules.Chat;

public sealed class ChatService
{
    private readonly OpenAIClient _openAIClient;
    private readonly OpenAIOptions _openAIOptions;
    private readonly AuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ChatHistoryService _chatHistoryService;

    public ChatService(
        OpenAIClient openAIClient,
        OpenAIOptions openAIOptions,
        AuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor,
        ChatHistoryService chatHistoryService)
    {
        _openAIClient = openAIClient;
        _openAIOptions = openAIOptions;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
        _chatHistoryService = chatHistoryService;
    }

    public async Task<ChatMessageResponse> SendMessageAsync(ChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Lay current user tu HttpContext.User.
        // 2. Neu user chua authenticated thi throw UnauthorizedAccessException.
        //    Controller se map sang 401.
        // 3. Validate request.Message:
        //    - khong null/rong.
        //    - trim.
        //    - gioi han do dai, vi du 4000 ky tu trong Milestone 2.
        // 4. Tao messages gui len OpenAI:
        //    - system: _openAIOptions.SystemPrompt
        //    - user: message da trim
        // 5. Goi _openAIClient.SendChatAsync(...).
        // 6. Map OpenAIChatResult sang ChatMessageResponse.
        // 7. Ghi audit log action = "chat_message".
        //    MetadataJson chi nen gom:
        //    - messageLength
        //    - model
        //    - promptTokens
        //    - completionTokens
        //    - totalTokens
        //    - role
        //    Khong log full message cua user trong audit.
        // 8. Return response.

        var principal = GetCurrentPrincipal();

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Please login before chatting.");
        }

        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ValidationApiException("invalid_message", "Message is required.");
        }
        if (message.Length >= 4000)
        {
            throw new ValidationApiException("invalid_message", "Message is too long.");
        }

        var openAiMessages = new List<OpenAIChatMessage>
        {
            new OpenAIChatMessage
            {
                Role ="system",
                Content =_openAIOptions.SystemPrompt
            },
            new OpenAIChatMessage
            {
                Role = "user",
                Content = message
            }
        };

        OpenAIChatResult openAIResult;

        try
        {
            openAIResult = await _openAIClient.SendChatAsync(openAiMessages, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw new ExternalServiceApiException("ai_provider_error", "AI provider is temporarily unavailable.");
        }

        var response = new ChatMessageResponse
        {
            Answer = openAIResult.Answer,
            Model = openAIResult.Model,
            PromptTokens = openAIResult.PromptTokens,
            CompletionTokens = openAIResult.CompletionTokens,
            TotalTokens = openAIResult.TotalTokens
        };

        var role = GetRole(principal);
        await WriteChatAuditAsync(principal, role, request, response, cancellationToken);
        return response;
    }

    private ClaimsPrincipal GetCurrentPrincipal()
    {
        // TODO:
        // Lay HttpContext.User tu IHttpContextAccessor.
        // Neu HttpContext null thi throw InvalidOperationException.
        return _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedApiException("unauthorized", "Please login before chatting.");
    }

    private static string GetRole(ClaimsPrincipal principal)
    {
        // TODO:
        // Doc ClaimTypes.Role.
        // Neu khong co role thi return "anonymous".
        var role = principal.FindFirstValue(ClaimTypes.Role);

        if (role == UserRole.Admin)
        {
            return UserRole.Admin;
        }
        else if (role == UserRole.Employee)
        {
            return UserRole.Employee;
        }
        else if (role == UserRole.Guest)
        {
            return UserRole.Guest;
        }
        return "anonymous";
    }

    private static Guid? GetCurrentUserId(ClaimsPrincipal principal)
    {
        // TODO:
        // Doc ClaimTypes.NameIdentifier cho employee/admin.
        // Neu parse Guid fail thi return null.
        return TryGetGuidClaim(principal, ClaimTypes.NameIdentifier);
    }

    private static Guid? GetCurrentGuestSessionId(ClaimsPrincipal principal)
    {
        // TODO:
        // Doc claim "guest_session_id" cho guest.
        // Neu parse Guid fail thi return null.
        return TryGetGuidClaim(principal, "guest_session_id");
    }

    private Task WriteChatAuditAsync(ClaimsPrincipal principal, string role, ChatMessageRequest request, ChatMessageResponse response, CancellationToken cancellationToken)
    {
        // TODO:
        // Tao AuditLogEntry:
        // - ActorUserId neu co.
        // - ActorGuestSessionId neu co.
        // - Action = "chat_message".
        // - ResourceType = "Chat".
        // - MetadataJson = JSON an toan.
        //
        // Goi _auditLogService.LogAsync(...).
        //
        // Goi y metadata:
        // JsonSerializer.Serialize(new
        // { 
        //     role,
        //     messageLength = request.Message.Length,
        //     model = response.Model,
        //     response.TotalTokens
        // })
        var auditLogEntry = new AuditLogEntry
        {
            ActorUserId = GetCurrentUserId(principal),
            ActorGuestSessionId = GetCurrentGuestSessionId(principal),
            Action = "chat_message",
            ResourceType = "Chat",
            MetadataJson = JsonSerializer.Serialize(new
            {
                role,
                messageLength = request.Message?.Trim().Length,
                model = response.Model,
                response.TotalTokens
            })
        };

        return _auditLogService.LogAsync(auditLogEntry, cancellationToken);
    }

    public async Task<ChatMessageResponse> SendSessionMessageAsync(Guid sessionId, SendSessionMessageRequest request, CancellationToken cancellationToken = default)
    {
        var principal = GetCurrentPrincipal();

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Please login before chatting.");
        }

        if (request is null)
        {
            throw new ValidationApiException("invalid_message", "Message request is required.");
        }

        var message = request.Message?.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ValidationApiException("invalid_message", "Message is required.");
        }

        if (message.Length >= 4000)
        {
            throw new ValidationApiException("invalid_message", "Message is too long.");
        }

        var session = await _chatHistoryService.GetOwnedSessionAsync(sessionId, cancellationToken);
        await _chatHistoryService.AddMessageAsync(sessionId, ChatMessageRole.User, message, cancellationToken);
        var recentMessages = await _chatHistoryService.GetRecentMessagesAsync(sessionId, take: 10, cancellationToken);
        var openAIChatMessage = new List<OpenAIChatMessage>
        {
            new OpenAIChatMessage
            {
                Role = ChatMessageRole.System,
                Content = _openAIOptions.SystemPrompt
            }
        };

        openAIChatMessage.AddRange(recentMessages.Select(chatMessage => new OpenAIChatMessage
        {
            Role = chatMessage.Role,
            Content = chatMessage.Content
        }));

        OpenAIChatResult openAIChatResult;

        try
        {
            openAIChatResult = await _openAIClient.SendChatAsync(openAIChatMessage, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw new ExternalServiceApiException("ai_provider_error", "AI provider is temporarily unavailable.");
        }

        var chatMessageResponse = new ChatMessageResponse
        {
            Answer = openAIChatResult.Answer,
            Model = openAIChatResult.Model,
            PromptTokens = openAIChatResult.PromptTokens,
            CompletionTokens = openAIChatResult.CompletionTokens,
            TotalTokens = openAIChatResult.TotalTokens
        };

        await _chatHistoryService.AddMessageAsync(sessionId, ChatMessageRole.Assistant, chatMessageResponse.Answer, cancellationToken);
        await _chatHistoryService.AddTokenUsageAsync(session, chatMessageResponse, cancellationToken);
        await _chatHistoryService.UpdateSessionTimestampAsync(session, cancellationToken);

        var role = GetRole(principal);
        await WriteChatAuditAsync(
            principal,
            role,
            new ChatMessageRequest { Message = message },
            chatMessageResponse,
            cancellationToken);

        return chatMessageResponse;

    }
    /////////////////////////////////////////////////////////////////////
    /// Helper
    ////////////////////////////////////////////////////////////////////
    private static Guid? TryGetGuidClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirstValue(claimType);

        return Guid.TryParse(value, out var id)
            ? id
            : null;
    }
}
