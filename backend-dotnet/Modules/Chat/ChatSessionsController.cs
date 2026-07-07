using backend_dotnet.Contracts.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.Chat;

[ApiController]
[Authorize]
[Route("api/chat/sessions")]
public sealed class ChatSessionsController : ControllerBase
{
    private readonly ChatHistoryService _chatHistoryService;
    private readonly ChatService _chatService;

    public ChatSessionsController(ChatHistoryService chatHistoryService, ChatService chatService)
    {
        _chatHistoryService = chatHistoryService;
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<ActionResult<ChatSessionResponse>> CreateSession([FromBody] CreateChatSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await _chatHistoryService.CreateSessionAsync(request, cancellationToken);
        return Ok(session);
    }

    [HttpGet]
    public async Task<ActionResult<List<ChatSessionResponse>>> ListSessions(
        CancellationToken cancellationToken)
    {
        // TODO:
        // 1. Goi _chatHistoryService.ListSessionsAsync.
        // 2. Return Ok(listSession).
        var listSession = await _chatHistoryService.ListSessionsAsync(cancellationToken);
        return Ok(listSession);
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<ChatSessionDetailResponse>> GetSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var sessionDetail = await _chatHistoryService.GetSessionDetailAsync(sessionId, cancellationToken);
        return Ok(sessionDetail);
    }
    [HttpGet("{sessionId:guid}/messages")]
    public async Task<ActionResult<List<ChatMessageItemResponse>>> GetMessages(Guid sessionId, CancellationToken cancellationToken)
    {
        var messages = await _chatHistoryService.GetSessionMessagesAsync(sessionId, cancellationToken);
        return Ok(messages);
    }

    [HttpPost("{sessionId:guid}/messages")]
    [EnableRateLimiting("chat")]
    public async Task<ActionResult<ChatMessageResponse>> SendMessage(Guid sessionId, [FromBody] SendSessionMessageRequest request, CancellationToken cancellationToken)
    {
        var messageResponse = await _chatService.SendSessionMessageAsync(sessionId, request, cancellationToken);
        return Ok(messageResponse);
    }
}
