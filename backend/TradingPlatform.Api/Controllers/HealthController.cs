using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var healthStatus = await _healthService.GetHealthStatusAsync();

        return healthStatus.IsReady
            ? Ok(healthStatus)
            : StatusCode(503, healthStatus);
    }
}