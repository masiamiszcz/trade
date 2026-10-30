using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Api.Models;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CfdController : ControllerBase
{
    private readonly ICfdService _cfdService;

    public CfdController(ICfdService cfdService)
    {
        _cfdService = cfdService;
    }

    [HttpGet("instruments")]
    public async Task<ActionResult<IEnumerable<CfdInstrumentSummaryDto>>> GetAvailableCfdInstruments(
        CancellationToken cancellationToken)
    {
        var instruments = await _cfdService.GetAvailableCfdInstrumentsAsync(cancellationToken);
        return Ok(instruments);
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
            var candles = await _cfdService.GetChartCandlesAsync(
                symbol,
                request.RangeMinutes,
                request.IntervalMinutes,
                request.To,
                cancellationToken);

            var candleList = candles.ToList();
            if (candleList.Count == 0)
            {
                return NotFound(new { message = $"No chart history found for CFD symbol '{symbol}' in the requested range." });
            }

            return Ok(candleList);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"CFD symbol '{symbol}' not available." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
