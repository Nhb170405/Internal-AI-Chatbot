using backend_dotnet.Contracts.Assistant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.Assistant;

[ApiController]
[Authorize]
[Route("api/assistant")]
public sealed class AssistantController : ControllerBase
{
    private readonly AssistantService _assistantService;

    public AssistantController(AssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    [HttpPost("chat")]
    [EnableRateLimiting("chat")]
    public async Task<ActionResult<AssistantChatResponse>> Chat(
        AssistantChatRequest request,
        CancellationToken cancellationToken)
    {
        // Controller khong routing va khong map loi thu cong.
        // AssistantService/tool service se nem ApiException, middleware format response.
        var response = await _assistantService.SendAsync(request, cancellationToken);
        return Ok(response);
    }
}
