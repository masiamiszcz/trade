using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MarketController(IMarketDataService marketDataService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var assets = await marketDataService.GetAllAsync(cancellationToken);
        return Ok(assets);
    }

    [HttpGet("{symbol}")]
    public async Task<IActionResult> GetBySymbol(string symbol, CancellationToken cancellationToken)
    {
        var asset = await marketDataService.GetBySymbolAsync(symbol, cancellationToken);

        return asset is null
            ? NotFound(new { message = $"Asset '{symbol}' not found." })
            : Ok(asset);
    }
}