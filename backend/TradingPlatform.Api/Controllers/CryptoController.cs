using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class CryptoController : ControllerBase
{
    private readonly ICryptoService _cryptoService;

    public CryptoController(ICryptoService cryptoService)
    {
        _cryptoService = cryptoService;
    }

    [HttpGet("cryptoinstruments")]
    public async Task<ActionResult<IEnumerable<InstrumentDto>>> GetAvailableCryptoInstruments(CancellationToken cancellationToken)
    {
        var instruments = await _cryptoService.GetAvailableCryptoInstrumentsAsync(cancellationToken);
        var cryptoInstruments = instruments
            .Where(i => string.Equals(i.Type, InstrumentType.Crypto.ToString(), StringComparison.OrdinalIgnoreCase));

        return Ok(cryptoInstruments);
    }

    [HttpGet("{symbol}/candles")]
    public async Task<ActionResult<IEnumerable<CandleDto>>> GetCandlesBySymbol(
        string symbol,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var candles = await _cryptoService.GetCandlesBySymbolAsync(symbol, limit, cancellationToken);
            return Ok(candles);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Crypto symbol '{symbol}' not available." });
        }
    }
}
