namespace TradingPlatform.Api.Controllers;

// InstrumentsController
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;


[ApiController]
[Route("api/instruments")]
[Authorize]
public sealed class InstrumentsController : ControllerBase
{
    private readonly IInstrumentService _instrumentService;

    public InstrumentsController(IInstrumentService instrumentService)
    {
        _instrumentService = instrumentService;
    }

    // ===== READ =====

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _instrumentService.GetAllAsync(ct));

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive(CancellationToken ct)
        => Ok(await _instrumentService.GetAllActiveAsync(ct));

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _instrumentService.GetByIdAsync(id, ct));

    // ===== REQUESTS (NO APPROVAL HERE) =====

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateInstrumentRequest request, CancellationToken ct)
    {
        var adminId = GetUserId();
        await _instrumentService.RequestCreateAsync(request, adminId, ct);
        return Accepted();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RequestUpdate(Guid id, UpdateInstrumentRequest request, CancellationToken ct)
    {
        var adminId = GetUserId();
        await _instrumentService.RequestUpdateAsync(id, request, adminId, ct);
        return Accepted();
    }

    [HttpPatch("{id}/block")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RequestBlock(Guid id, AdminRequestReasonRequest req, CancellationToken ct)
    {
        var adminId = GetUserId();
        await _instrumentService.RequestBlockAsync(id, req.Reason, adminId, ct);
        return Accepted();
    }

    [HttpPatch("{id}/unblock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RequestUnblock(Guid id, AdminRequestReasonRequest req, CancellationToken ct)
    {
        var adminId = GetUserId();
        await _instrumentService.RequestUnblockAsync(id, req.Reason, adminId, ct);
        return Accepted();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RequestDelete(Guid id, CancellationToken ct)
    {
        var adminId = GetUserId();
        await _instrumentService.RequestDeleteAsync(id, adminId, ct);
        return Accepted();
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirst("sub")!.Value);
}