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

    /// <summary>
    /// Get exchange rate for a currency pair (e.g., usdpln, eurpln, gbppln)
    /// </summary>
    [HttpGet("rate/{symbol}")]
    public async Task<IActionResult> GetExchangeRate(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length != 6)
            return BadRequest(new { message = "Symbol must be 6 characters (e.g., usdpln)" });

        var from = symbol.Substring(0, 3).ToUpper();
        var to = symbol.Substring(3, 3).ToUpper();

        try
        {
            var rate = await _exchangeRateService.GetRateAsync(from, to);
            
            if (rate == null)
                return NotFound(new { message = $"No {from}/{to} rate found in database" });

            return Ok(new { baseCurrency = from, quoteCurrency = to, rate = rate });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
