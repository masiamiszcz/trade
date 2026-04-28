using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]  // Debug endpoint - no auth required
public class FiatController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;

    public FiatController(IExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    /// <summary>
    /// DEBUG: Get latest USD to PLN exchange rate from database
    /// </summary>
    [HttpGet("debug/rate")]
    public async Task<IActionResult> GetRate()
    {
        try
        {
            var rate = await _exchangeRateService.GetUsdToPlnAsync();
            
            if (rate == null)
                return NotFound(new { message = "No USD/PLN rate found in database" });

            return Ok(new { baseCurrency = "USD", quoteCurrency = "PLN", rate = rate });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
