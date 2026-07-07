using System.Security.Claims;
using backend_dotnet.Contracts.Chat;
using backend_dotnet.Infrastructure.Errors;
using backend_dotnet.Infrastructure.Persistence;
using backend_dotnet.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Modules.Chat;

public sealed class ChatHistoryService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChatHistoryService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ChatSessionResponse> CreateSessionAsync(CreateChatSessionRequest request, CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Lay current principal.
        // 2. Neu chua authenticated thi throw UnauthorizedAccessException.
        // 3. Xac dinh owner:
        //    - role guest -> GuestSessionId tu claim "guest_session_id".
        //    - employee/admin -> UserId tu ClaimTypes.NameIdentifier.
        // 4. Tao ChatSession:
        //    - Id moi.
        //    - Title = request.Title neu co, nguoc lai "New chat".
        //    - Owner theo user/guest.
        //    - CreatedAt/UpdatedAt = now.
        // 5. Luu DB.
        // 6. Return ChatSessionResponse.
        var principal = GetCurrentPrincipal();

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Please login before chatting.");
        }

        var owner = GetOwner(principal);

        var chatSession = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = owner.UserId,
            GuestSessionId = owner.GuestSessionId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Hoi thoai moi" : request.Title.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.ChatSessions.Add(chatSession);

        await _db.SaveChangesAsync(cancellationToken);

        return new ChatSessionResponse
        {
            Id = chatSession.Id,
            Title = chatSession.Title,
            CreatedAt = chatSession.CreatedAt,
            UpdatedAt = chatSession.UpdatedAt
        }
        ;
    }

    public async Task<List<ChatSessionResponse>> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Lay current owner.
        // 2. Query ChatSessions thuoc owner do.
        // 3. Sort UpdatedAt descending.
        // 4. Map sang ChatSessionResponse.
        var principal = GetCurrentPrincipal();

        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedApiException("unauthorized", "Please login before chatting.");
        }

        var owner = GetOwner(principal);
        var query = _db.ChatSessions.AsQueryable();

        if (owner.GuestSessionId.HasValue)
        {
            query = query.Where(session => session.GuestSessionId == owner.GuestSessionId.Value);
        }
        else if (owner.UserId.HasValue)
        {
            query = query.Where(session => session.UserId == owner.UserId.Value);
        }
        else
        {
            throw new UnauthorizedApiException("unauthorized", "Invalid session owner.");
        }

        return await query.OrderByDescending(session => session.UpdatedAt).Select(session => new ChatSessionResponse
        {
            Id = session.Id,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        }).ToListAsync(cancellationToken);

    }

    public async Task<ChatSessionDetailResponse> GetSessionDetailAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Tim session theo Id va owner hien tai.
        // 2. Neu khong tim thay thi throw KeyNotFoundException.
        // 3. Load messages theo CreatedAt ascending.
        // 4. Map sang ChatSessionDetailResponse.
        var chatSession = await GetOwnedSessionAsync(sessionId, cancellationToken);
        var chatMessageItemResponse = await GetSessionMessagesAsync(sessionId, cancellationToken);
        return new ChatSessionDetailResponse
        {
            Id = chatSession.Id,
            Title = chatSession.Title,
            CreatedAt = chatSession.CreatedAt,
            UpdatedAt = chatSession.UpdatedAt,
            Messages = chatMessageItemResponse
        };

    }

    public async Task<List<ChatMessageItemResponse>> GetSessionMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Check session thuoc owner hien tai.
        // 2. Neu khong thuoc owner thi throw KeyNotFoundException.
        // 3. Query messages theo CreatedAt ascending.
        // 4. Map sang ChatMessageItemResponse.
        await GetOwnedSessionAsync(sessionId, cancellationToken);

        var messages = await _db.ChatMessages
        .Where(message => message.ChatSessionId == sessionId)
        .OrderBy(message => message.CreatedAt)
        .Select(message => new ChatMessageItemResponse
        {
            Id = message.Id,
            Role = message.Role,
            Content = message.Content,
            CreatedAt = message.CreatedAt
        })
        .ToListAsync(cancellationToken);

        return messages;
    }

    public async Task<ChatSession> GetOwnedSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        // TODO:
        // Ham nay cho ChatService dung khi gui message vao session.
        // 1. Lay owner hien tai.
        // 2. Tim ChatSession co Id = sessionId va owner match.
        // 3. Neu khong thay thi throw KeyNotFoundException.
        // 4. Return entity ChatSession.
        var principal = GetCurrentPrincipal();
        var owner = GetOwner(principal);
        var query = _db.ChatSessions.AsQueryable();

        if (owner.UserId.HasValue)
        {
            query = query.Where(session => session.Id == sessionId && session.UserId == owner.UserId.Value);
        }
        else if (owner.GuestSessionId.HasValue)
        {
            query = query.Where(session => session.Id == sessionId && session.GuestSessionId == owner.GuestSessionId.Value);
        }
        else
        {
            throw new UnauthorizedApiException("unauthorized", "Invalid session owner.");
        }

        var chatSession = await query.FirstOrDefaultAsync(cancellationToken);

        if (chatSession is null)
        {
            throw new NotFoundApiException("chat_session_not_found", "Chat session not found.");
        }

        return chatSession;
    }

    public async Task<ChatMessage> AddMessageAsync(Guid sessionId, string role, string content, CancellationToken cancellationToken = default)
    {
        // TODO: luu 1 message vao DB
        // 1. Validate role/content.
        // 2. Tao ChatMessage.
        // 3. Add vao DB.
        // 4. SaveChangesAsync.
        // 5. Return ChatMessage.

        if (role != ChatMessageRole.User && role != ChatMessageRole.Assistant)
        {
            throw new ValidationApiException("invalid_chat_message_role", "Invalid chat message role.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ValidationApiException("invalid_message", "Message content is required.");
        }

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = sessionId,
            Role = role,
            Content = content.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task AddTokenUsageAsync(ChatSession session, ChatMessageResponse response, CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Tao TokenUsage tu response token usage.
        // 2. Copy owner tu ChatSession.
        // 3. Luu DB.
        if (session == null)
        {
            throw new ValidationApiException("invalid_chat_session", "Chat session is required.");
        }

        if (response == null)
        {
            throw new ValidationApiException("invalid_chat_response", "Chat response is required.");
        }

        if (string.IsNullOrWhiteSpace(response.Model))
        {
            throw new ExternalServiceApiException("ai_provider_error", "AI provider response model is missing.");
        }

        var tokenUsage = new TokenUsage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = session.Id,
            UserId = session.UserId,
            GuestSessionId = session.GuestSessionId,
            Model = response.Model,
            PromptTokens = response.PromptTokens,
            CompletionTokens = response.CompletionTokens,
            TotalTokens = response.TotalTokens,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.TokenUsages.Add(tokenUsage);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSessionTimestampAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Set session.UpdatedAt = DateTimeOffset.UtcNow.
        // 2. SaveChangesAsync.

        if (session == null)
        {
            throw new ValidationApiException("invalid_chat_session", "Chat session is required.");
        }

        session.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ChatMessage>> GetRecentMessagesAsync(Guid sessionId, int take, CancellationToken cancellationToken = default)
    {
        // TODO:
        // Dung de dua mot so message gan nhat vao prompt OpenAI.
        // 1. Query messages theo sessionId.
        // 2. Lay take message moi nhat.
        // 3. Sort lai CreatedAt ascending truoc khi return.
        if (take <= 0)
        {
            return new List<ChatMessage>();
        }

        var messages = await _db.ChatMessages
            .Where(message => message.ChatSessionId == sessionId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return messages
            .OrderBy(message => message.CreatedAt)
            .ToList();
    }

    private ClaimsPrincipal GetCurrentPrincipal()
    {
        // TODO:
        // Lay HttpContext.User.
        // Neu HttpContext null thi throw InvalidOperationException.
        return _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedApiException("unauthorized", "Please login before chatting.");
    }

    private static Guid? TryGetGuidClaim(ClaimsPrincipal principal, string claimType)
    {
        // TODO:
        // Doc claim va parse Guid.
        // Neu khong parse duoc thi return null.
        var value = principal.FindFirstValue(claimType);

        return Guid.TryParse(value, out var id) ? id : null;
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

    private static (Guid? UserId, Guid? GuestSessionId) GetOwner(ClaimsPrincipal principal)
    {
        // TODO:
        // 1. Neu role = guest:
        //    - lay guest_session_id.
        //    - return (null, guestSessionId).
        // 2. Neu role = employee/admin:
        //    - lay ClaimTypes.NameIdentifier.
        //    - return (userId, null).
        // 3. Neu khong hop le thi throw UnauthorizedAccessException.
        var role = GetRole(principal);
        if (role == UserRole.Guest)
        {
            Guid? guestSessionid = TryGetGuidClaim(principal, "guest_session_id");
            return guestSessionid == null
                ? throw new UnauthorizedApiException("unauthorized", "Invalid guest session.")
                : (null, guestSessionid);
        }
        if (role == UserRole.Admin || role == UserRole.Employee)
        {
            Guid? UserId = TryGetGuidClaim(principal, ClaimTypes.NameIdentifier);
            return UserId == null
                ? throw new UnauthorizedApiException("unauthorized", "Invalid user session.")
                : (UserId, null);
        }
        throw new UnauthorizedApiException("unauthorized", "Please login before chatting.");
    }
}
