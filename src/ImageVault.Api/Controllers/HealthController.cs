using Microsoft.AspNetCore.Mvc;

namespace ImageVault.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>Liveness cho Docker healthcheck.</summary>
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", utc = DateTime.UtcNow });
}
