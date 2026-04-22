namespace TradingPlatform.Api.Controllers;

// InstrumentsController
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;



[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class InstrumentsController : ControllerBase
{
    private readonly IInstrumentService _instrumentService;

    public InstrumentsController(IInstrumentService instrumentService)
    {
        _instrumentService = instrumentService;
    }

    /// <summary>
    /// Get all instruments (including blocked ones)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<InstrumentDto>>> GetAll(CancellationToken cancellationToken)
    {
        var instruments = await _instrumentService.GetAllAsync(page: 1, pageSize: 50, cancellationToken: cancellationToken);
        return Ok(instruments);
    }

    /// <summary>
    /// Get only active and not blocked instruments
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<InstrumentDto>>> GetAllActive(CancellationToken cancellationToken)
    {
        var instruments = await _instrumentService.GetAllActiveAsync(cancellationToken);
        return Ok(instruments);
    }

    /// <summary>
    /// Get instrument by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<InstrumentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var instrument = await _instrumentService.GetByIdAsync(id, cancellationToken);
        return Ok(instrument);
    }

    /// <summary>
    /// Get instrument by symbol
    /// </summary>
    [HttpGet("symbol/{symbol}")]
    [AllowAnonymous]
    public async Task<ActionResult<InstrumentDto>> GetBySymbol(string symbol, CancellationToken cancellationToken)
    {
        var instrument = await _instrumentService.GetBySymbolAsync(symbol, cancellationToken);
        return Ok(instrument);
    }

    /// <summary>
    /// Create new instrument (ADMIN only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InstrumentDto>> Create([FromBody] CreateInstrumentRequest request, CancellationToken cancellationToken)
    {
        var adminId = Guid.Parse(User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found in token"));
        var instrument = await _instrumentService.CreateAsync(request, adminId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = instrument.Id }, instrument);
    }

    /// <summary>
    /// Update instrument (ADMIN only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InstrumentDto>> Update(Guid id, [FromBody] UpdateInstrumentRequest request, CancellationToken cancellationToken)
    {
        var adminId = Guid.Parse(User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found in token"));
        var instrument = await _instrumentService.UpdateAsync(id, request, adminId, cancellationToken);
        return Ok(instrument);
    }

    /// <summary>
    /// Block instrument - users cannot buy/sell (ADMIN only)
    /// </summary>
    [HttpPatch("{id}/block")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InstrumentDto>> Block(Guid id, CancellationToken cancellationToken)
    {
        var adminId = Guid.Parse(User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("Unable to extract admin ID from token"));
        var instrument = await _instrumentService.BlockAsync(id, adminId, cancellationToken);
        return Ok(instrument);
    }

    /// <summary>
    /// Unblock instrument - users can buy/sell again (ADMIN only)
    /// </summary>
    [HttpPatch("{id}/unblock")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InstrumentDto>> Unblock(Guid id, CancellationToken cancellationToken)
    {
        var adminId = Guid.Parse(User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("Unable to extract admin ID from token"));
        var instrument = await _instrumentService.UnblockAsync(id, adminId, cancellationToken);
        return Ok(instrument);
    }

    /// <summary>
    /// Delete instrument (ADMIN only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var adminId = Guid.Parse(User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("Unable to extract admin ID from token"));
        await _instrumentService.DeleteAsync(id, adminId, cancellationToken);
        return NoContent();
    }
}
