using Microsoft.AspNetCore.Mvc;

namespace backend_dotnet.Modules.Health;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok", service = "backend-dotnet-api", timestamp = DateTime.UtcNow });
    }
}
