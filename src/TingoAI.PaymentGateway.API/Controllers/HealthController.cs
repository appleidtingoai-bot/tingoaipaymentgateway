using Microsoft.AspNetCore.Mvc;

namespace TingoAI.PaymentGateway.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint for load balancer
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            service = "TingoAI Payment Gateway"
        });
    }

    /// <summary>
    /// Readiness probe for container orchestration
    /// </summary>
    [HttpGet("ready")]
    public IActionResult Ready()
    {
        return Ok(new { status = "Ready" });
    }

    /// <summary>
    /// Liveness probe for container orchestration
    /// </summary>
    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok(new { status = "Live" });
    }
}
