using backend_dotnet.Contracts.Rag;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.Rag;

[ApiController]
[Authorize]
[Route("api/rag")]
public sealed class RagController : ControllerBase
{
    private readonly RagService _ragService;

    public RagController(RagService ragService)
    {
        _ragService = ragService;
    }

    [HttpPost("chat")]
    [EnableRateLimiting("chat")]
    public async Task<ActionResult<RagChatResponse>> Chat(RagChatRequest request, CancellationToken cancellationToken)
    {
        // Controller chi nhan HTTP request va tra HTTP response.
        // Loi nghiep vu/AI service se duoc RagService nem ApiException va GlobalExceptionMiddleware format.
        var response = await _ragService.SendAsync(request, cancellationToken);
        return Ok(response);
    }
}
