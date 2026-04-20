

// AdminController
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Controllers;

/// <summary>
/// Admin API endpoints for managing instruments and approvals
/// ALL endpoints are restricted to admin role and require VPN access (10.8.0.0/24)
/// Implemented at Nginx level, not in controller
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAdminService adminService,
        ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    /// <summary>
    /// Get all instruments for admin management
    /// </summary>
    /// <response code="200">List of all instruments</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin or not connected via VPN</response>
    [HttpGet("instruments")]
    [ProducesResponseType(typeof(IEnumerable<InstrumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<InstrumentDto>>> GetAllInstruments(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Admin requesting all instruments");

            var instruments = await _adminService.GetAllInstrumentsAsync(cancellationToken);
            return Ok(instruments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving instruments");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve instruments" });
        }
    }

    /// <summary>
    /// Get all admin requests with their statuses
    /// </summary>
    /// <response code="200">List of all admin requests</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin or not connected via VPN</response>
    [HttpGet("requests")]
    [ProducesResponseType(typeof(IEnumerable<AdminRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<AdminRequestDto>>> GetAllRequests(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Admin requesting all requests");

            var requests = await _adminService.GetAllRequestsAsync(cancellationToken);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving requests");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve requests" });
        }
    }

    /// <summary>
    /// Get pending requests awaiting approval
    /// </summary>
    /// <response code="200">List of pending requests</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin or not connected via VPN</response>
    [HttpGet("requests/pending")]
    [ProducesResponseType(typeof(IEnumerable<AdminRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<AdminRequestDto>>> GetPendingRequests(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Admin requesting pending requests");

            var requests = await _adminService.GetPendingRequestsAsync(cancellationToken);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending requests");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve pending requests" });
        }
    }

    /// <summary>
    /// Create a block request for an instrument
    /// Request must be approved by another admin before taking effect
    /// </summary>
    /// <param name="instrumentId">ID of the instrument to block</param>
    /// <param name="request">Request body with reason</param>
    /// <response code="201">Block request created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin or not connected via VPN</response>
    /// <response code="404">Instrument not found</response>
    [HttpPost("instruments/{instrumentId}/request-block")]
    [ProducesResponseType(typeof(AdminRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminRequestDto>> RequestBlockInstrument(
        Guid instrumentId,
        [FromBody] CreateBlockRequestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminIdFromToken();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogInformation(
                "Admin {AdminId} from {IpAddress} requesting block for instrument {InstrumentId}",
                adminId,
                ipAddress,
                instrumentId);

            var blockRequest = await _adminService.CreateBlockRequestAsync(
                instrumentId,
                request.Reason,
                adminId,
                ipAddress,
                cancellationToken);

            return CreatedAtAction(nameof(GetPendingRequests), blockRequest);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for block request");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating block request");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to create block request" });
        }
    }

    /// <summary>
    /// Create an unblock request for an instrument
    /// Request must be approved by another admin before taking effect
    /// </summary>
    /// <param name="instrumentId">ID of the instrument to unblock</param>
    /// <param name="request">Request body with reason</param>
    /// <response code="201">Unblock request created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin or not connected via VPN</response>
    /// <response code="404">Instrument not found or not blocked</response>
    [HttpPost("instruments/{instrumentId}/request-unblock")]
    [ProducesResponseType(typeof(AdminRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminRequestDto>> RequestUnblockInstrument(
        Guid instrumentId,
        [FromBody] CreateUnblockRequestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminIdFromToken();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogInformation(
                "Admin {AdminId} from {IpAddress} requesting unblock for instrument {InstrumentId}",
                adminId,
                ipAddress,
                instrumentId);

            var unblockRequest = await _adminService.CreateUnblockRequestAsync(
                instrumentId,
                request.Reason,
                adminId,
                ipAddress,
                cancellationToken);

            return CreatedAtAction(nameof(GetPendingRequests), unblockRequest);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for unblock request");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating unblock request");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to create unblock request" });
        }
    }

    /// <summary>
    /// Approve a pending admin request
    /// Only another admin can approve a request (prevents self-approval)
    /// Executing the requested action (block/unblock) requires approval
    /// </summary>
    /// <param name="requestId">ID of the admin request to approve</param>
    /// <response code="200">Request approved and action executed</response>
    /// <response code="400">Invalid request (e.g., already approved/rejected)</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin or not connected via VPN</response>
    /// <response code="404">Request not found</response>
    [HttpPatch("requests/{requestId}/approve")]
    [ProducesResponseType(typeof(AdminRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminRequestDto>> ApproveRequest(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        try
        {
            var adminId = GetAdminIdFromToken();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogInformation(
                "Admin {AdminId} from {IpAddress} approving request {RequestId}",
                adminId,
                ipAddress,
                requestId);

            var approvedRequest = await _adminService.ApproveRequestAsync(
                requestId,
                adminId,
                ipAddress,
                cancellationToken);

            return Ok(approvedRequest);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for approval");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving request");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to approve request" });
        }
    }

    /// <summary>
    /// Reject a pending admin request
    /// Request status changes to "Rejected" and no action is executed
    /// </summary>
    /// <param name="requestId">ID of the admin request to reject</param>
    /// <response code="200">Request rejected</response>
    /// <response code="400">Invalid request (e.g., already approved/rejected)</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin or not connected via VPN</response>
    /// <response code="404">Request not found</response>
    [HttpPatch("requests/{requestId}/reject")]
    [ProducesResponseType(typeof(AdminRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminRequestDto>> RejectRequest(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        try
        {
            var adminId = GetAdminIdFromToken();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogInformation(
                "Admin {AdminId} from {IpAddress} rejecting request {RequestId}",
                adminId,
                ipAddress,
                requestId);

            var rejectedRequest = await _adminService.RejectRequestAsync(
                requestId,
                adminId,
                ipAddress,
                cancellationToken);

            return Ok(rejectedRequest);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for rejection");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting request");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to reject request" });
        }
    }

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    /// <param name="entityType">Type of entity (e.g., "Instrument", "AdminRequest")</param>
    /// <param name="entityId">ID of the entity</param>
    /// <response code="200">List of audit logs for the entity</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin or not connected via VPN</response>
    [HttpGet("audit-logs/entity/{entityType}/{entityId}")]
    [ProducesResponseType(typeof(IEnumerable<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetEntityAuditLogs(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Admin requesting audit logs for {EntityType} {EntityId}",
                entityType,
                entityId);

            var logs = await _adminService.GetAuditLogsByEntityAsync(entityType, entityId, cancellationToken);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve audit logs" });
        }
    }

    // ======== HELPER METHODS ========

    /// <summary>
    /// Extract admin ID from JWT token claims
    /// </summary>
    private Guid GetAdminIdFromToken()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim?.Value == null || !Guid.TryParse(userIdClaim.Value, out var adminId))
            throw new InvalidOperationException("Unable to extract admin ID from token");

        return adminId;
    }
}
