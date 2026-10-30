using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Controllers;

[ApiController]
[Route("api/cfd/views")]
[Authorize]
public sealed class CfdChartViewsController : ControllerBase
{
    private readonly ICfdChartViewService _viewService;

    public CfdChartViewsController(ICfdChartViewService viewService)
    {
        _viewService = viewService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CfdChartViewDto>>> GetForCurrentUser(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _viewService.GetForUserAsync(userId, cancellationToken));
    }

    [HttpGet("{viewId:guid}")]
    public async Task<ActionResult<CfdChartViewDto>> GetById(
        Guid viewId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var view = await _viewService.GetByIdForUserAsync(userId, viewId, cancellationToken);
        return view is null ? NotFound() : Ok(view);
    }

    [HttpPost]
    public async Task<ActionResult<CfdChartViewDto>> Create(
        [FromBody] SaveCfdChartViewRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            var view = await _viewService.CreateAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { viewId = view.Id }, view);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{viewId:guid}")]
    public async Task<IActionResult> Delete(Guid viewId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await _viewService.DeleteAsync(userId, viewId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("oid")?.Value;
        return Guid.TryParse(claim, out userId);
    }
}
