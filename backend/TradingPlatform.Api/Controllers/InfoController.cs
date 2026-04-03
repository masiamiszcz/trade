using Microsoft.AspNetCore.Mvc;

namespace TradingPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InfoController : ControllerBase
{
    [HttpGet]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            message = "Trading Platform API",
            status = "API is working",
            endpoints = new[]
            {
                "/api/health",
                "/api/market",
                "/api/market/{symbol}"
            },
            timestamp = DateTime.UtcNow
        });
    }
}