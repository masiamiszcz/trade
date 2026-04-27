using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Controllers;

[ApiController]
[Route("api/admin/approvals")]
[Authorize(Roles = "Admin")]
public sealed class ApprovalController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly IInstrumentService _instrumentService;
    private readonly ILogger<ApprovalController> _logger;

    public ApprovalController(IApprovalService approvalService, IInstrumentService instrumentService, ILogger<ApprovalController> logger)
    {
        _approvalService = approvalService;
        _instrumentService = instrumentService;
        _logger = logger;
    }

    // ===== RETRIEVAL OPERATIONS =====

    /// <summary>
    /// Get all admin requests
    /// </summary>
    /// <response code="200">List of all admin requests</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdminRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving all admin requests");
            var requests = await _approvalService.GetAllAsync(ct);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all requests");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve requests" });
        }
    }

    /// <summary>
    /// Get pending requests awaiting approval
    /// </summary>
    /// <response code="200">List of pending admin requests</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<AdminRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving pending admin requests");
            var requests = await _approvalService.GetPendingAsync(ct);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending requests");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve pending requests" });
        }
    }

    /// <summary>
    /// Get a specific admin request by ID
    /// </summary>
    /// <param name="id">ID of the admin request</param>
    /// <response code="200">Admin request details</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Request not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AdminRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving admin request {RequestId}", id);
            var request = await _approvalService.GetByIdAsync(id, ct);
            return Ok(request);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Request not found: {RequestId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving request {RequestId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve request" });
        }
    }

    // ===== APPROVAL/REJECTION OPERATIONS =====

    /// <summary>
    /// Approve a pending admin request
    /// Executes the requested action (block, unblock, update, delete, etc.)
    /// Prevents self-approval
    /// </summary>
    /// <param name="id">ID of the admin request to approve</param>
    /// <response code="200">Request approved and action executed</response>
    /// <response code="400">Invalid request (e.g., already approved/rejected, self-approval)</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Request not found</response>
    [HttpPatch("{id}/approve")]
    [ProducesResponseType(typeof(AdminRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        try
        {
            var adminId = GetUserId();
            _logger.LogInformation("✅ [APPROVE] Request from admin {AdminId} to approve request {RequestId}", adminId, id);

            var result = await _approvalService.ApproveAsync(id, adminId, _instrumentService, ct);
            _logger.LogInformation("✅ [APPROVE] Request {RequestId} approved successfully. Status: {Status}", 
                id, result.Status);
            
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "❌ [APPROVE] Invalid approval operation for request {RequestId}. Details: {Message}", id, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [APPROVE] Error approving request {RequestId}. Details: {Message}", id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to approve request" });
        }
    }

    /// <summary>
    /// Reject a pending admin request
    /// Does not execute the requested action
    /// </summary>
    /// <param name="id">ID of the admin request to reject</param>
    /// <param name="req">Rejection details (optional reason)</param>
    /// <response code="200">Request rejected</response>
    /// <response code="400">Invalid request (e.g., already approved/rejected)</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Request not found</response>
    [HttpPatch("{id}/reject")]
    [ProducesResponseType(typeof(AdminRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequest? req, CancellationToken ct)
    {
        try
        {
            var adminId = GetUserId();
            _logger.LogInformation("🚫 [REJECT] Request from admin {AdminId} to reject request {RequestId}. Reason: {Reason}", 
                adminId, id, req?.Reason ?? "Not provided");

            var result = await _approvalService.RejectAsync(id, adminId, req?.Reason, ct);
            _logger.LogInformation("🚫 [REJECT] Request {RequestId} rejected successfully. Status: {Status}", 
                id, result.Status);
            
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "❌ [REJECT] Invalid rejection operation for request {RequestId}. Details: {Message}", id, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting request {RequestId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to reject request" });
        }
    }

    /// <summary>
    /// Add a comment to an admin request
    /// Request remains in its current status
    /// </summary>
    /// <param name="id">ID of the admin request</param>
    /// <param name="req">Comment text</param>
    /// <response code="200">Comment added successfully</response>
    /// <response code="400">Invalid comment</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Request not found</response>
    [HttpPost("{id}/comment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Comment(Guid id, [FromBody] CommentRequest req, CancellationToken ct)
    {
        try
        {
            var adminId = GetUserId();
            _logger.LogInformation("Admin {AdminId} adding comment to request {RequestId}", adminId, id);

            await _approvalService.AddCommentAsync(id, adminId, req.Text, ct);
            return Ok(new { message = "Comment added successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid comment operation for request {RequestId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to request {RequestId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to add comment" });
        }
    }

    // ===== HELPER METHODS =====

    private Guid GetUserId()
        => Guid.Parse(User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found in token"));
}

// ===== REQUEST/RESPONSE DTOs =====

public record RejectRequest(string Reason);

public record CommentRequest(string Text);