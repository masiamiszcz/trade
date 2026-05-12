using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Api.Models;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var candles = await _cryptoService.GetCandlesBySymbolAsync(symbol, cancellationToken);
            return Ok(candles);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Crypto symbol '{symbol}' not available." });
        }
    }

    [HttpPost("{symbol}/chart")]
    public async Task<ActionResult<IEnumerable<CandleDto>>> GetChartCandles(
        string symbol,
        [FromBody] ChartRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        if (request.RangeMinutes <= 0)
        {
            return BadRequest(new { message = "RangeMinutes must be greater than zero." });
        }

        if (request.IntervalMinutes.HasValue && request.IntervalMinutes.Value <= 0)
        {
            return BadRequest(new { message = "IntervalMinutes must be greater than zero when specified." });
        }

        try
        {
            var candles = await _cryptoService.GetChartCandlesAsync(
                symbol,
                request.RangeMinutes,
                request.IntervalMinutes,
                request.To,
                cancellationToken);

            return Ok(candles);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Crypto symbol '{symbol}' not available." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
