using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Controllers;

[ApiController]
[Route("api/approvals")]
[Authorize(Roles = "Admin")]
public sealed class ApprovalController(IApprovalService approvalService) : ControllerBase
{
    private readonly IApprovalService _approvalService = approvalService;

    // ===== REQUESTS =====

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _approvalService.GetAllAsync(ct));

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
        => Ok(await _approvalService.GetPendingAsync(ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _approvalService.GetByIdAsync(id, ct));

    // ===== ACTIONS =====

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var adminId = GetUserId();
        var result = await _approvalService.ApproveAsync(id, adminId, ct);
        return Ok(result);
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, RejectRequest req, CancellationToken ct)
    {
        var adminId = GetUserId();
        var result = await _approvalService.RejectAsync(id, adminId, req.Reason, ct);
        return Ok(result);
    }

    [HttpPost("{id}/comment")]
    public async Task<IActionResult> Comment(Guid id, CommentRequest req, CancellationToken ct)
    {
        var adminId = GetUserId();
        await _approvalService.AddCommentAsync(id, adminId, req.Text, ct);
        return Ok();
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirst("sub")!.Value);
}

public interface IApprovalService
{
}