

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
    private readonly IInstrumentService _instrumentService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAdminService adminService,
        IInstrumentService instrumentService,
        ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _instrumentService = instrumentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all instruments for admin management
    /// Returns all instruments (all statuses) for admin decision-making
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

            // Use InstrumentService (SINGLE SOURCE OF TRUTH for instrument logic)
            var instruments = await _instrumentService.GetAllAsync(page: 1, pageSize: 50, cancellationToken: cancellationToken);
            return Ok(instruments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving instruments");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve instruments" });
        }
    }

    /// <summary>
    /// Get instrument by ID (ADMIN only)
    /// </summary>
    /// <response code="200">Instrument details</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Instrument not found</response>
    [HttpGet("instruments/{id}")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstrumentDto>> GetInstrument(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Admin retrieving instrument {InstrumentId}", id);
            var instrument = await _instrumentService.GetByIdAsync(id, cancellationToken);
            if (instrument == null)
                return NotFound(new { error = "Instrument not found" });
            return Ok(instrument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving instrument {InstrumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve instrument" });
        }
    }

    /// <summary>
    /// Create new instrument (ADMIN only)
    /// Status starts as Draft, awaiting admin action
    /// </summary>
    /// <param name="request">Instrument creation request</param>
    /// <response code="201">Instrument created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    [HttpPost("instruments")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InstrumentDto>> CreateInstrument(
        [FromBody] CreateInstrumentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminIdFromToken();
            _logger.LogInformation("Admin {AdminId} creating instrument {Symbol}", adminId, request.Symbol);

            var instrument = await _instrumentService.CreateAsync(request, adminId, cancellationToken);
            return CreatedAtAction(nameof(GetInstrument), new { id = instrument.Id }, instrument);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for instrument creation");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating instrument");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to create instrument" });
        }
    }

    /// <summary>
    /// Update existing instrument (ADMIN only)
    /// Fields are optional — only provided fields will be updated
    /// Optimistic concurrency control via RowVersion
    /// </summary>
    /// <param name="id">Instrument ID</param>
    /// <param name="request">Update request with optional fields</param>
    /// <response code="200">Instrument updated successfully</response>
    /// <response code="400">Invalid request data or concurrency conflict</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Instrument not found</response>
    [HttpPut("instruments/{id}")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstrumentDto>> UpdateInstrument(
        Guid id,
        [FromBody] UpdateInstrumentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = GetAdminIdFromToken();
            _logger.LogInformation("Admin {AdminId} updating instrument {InstrumentId}", adminId, id);

            var instrument = await _instrumentService.UpdateAsync(id, request, adminId, cancellationToken);
            return Ok(instrument);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for instrument update");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating instrument {InstrumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to update instrument" });
        }
    }

    /// <summary>
    /// <response code="200">List of all users</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IEnumerable<UserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UserListItemDto>>> GetAllUsers(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Admin requesting all users");

            var users = await _adminService.GetAllUsersAsync(cancellationToken);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve users" });
        }
    }

    /// <summary>
    /// Delete instrument (ADMIN only)
    /// Removes instrument entirely from database
    /// </summary>
    /// <param name="id">Instrument ID to delete</param>
    /// <response code="204">Instrument deleted successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Instrument not found</response>
    [HttpDelete("instruments/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteInstrument(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var adminId = GetAdminIdFromToken();
            _logger.LogInformation("Admin {AdminId} deleting instrument {InstrumentId}", adminId, id);

            var exists = await _instrumentService.GetByIdAsync(id, cancellationToken);
            if (exists == null)
                return NotFound(new { error = "Instrument not found" });

            await _instrumentService.DeleteAsync(id, adminId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for instrument deletion");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting instrument {InstrumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to delete instrument" });
        }
    }

    /// <summary>
    /// Block instrument (ADMIN only)
    /// Sets IsBlocked=true, instrument becomes unavailable
    /// </summary>
    /// <param name="id">Instrument ID to block</param>
    /// <response code="200">Instrument blocked successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Instrument not found</response>
    [HttpPatch("instruments/{id}/block")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstrumentDto>> BlockInstrument(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var adminId = GetAdminIdFromToken();
            _logger.LogInformation("Admin {AdminId} blocking instrument {InstrumentId}", adminId, id);

            var instrument = await _instrumentService.BlockAsync(id, adminId, cancellationToken);
            return Ok(instrument);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for instrument block");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking instrument {InstrumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to block instrument" });
        }
    }

    /// <summary>
    /// Unblock instrument (ADMIN only)
    /// Sets IsBlocked=false, instrument becomes available again
    /// </summary>
    /// <param name="id">Instrument ID to unblock</param>
    /// <response code="200">Instrument unblocked successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Instrument not found or not blocked</response>
    [HttpPatch("instruments/{id}/unblock")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstrumentDto>> UnblockInstrument(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var adminId = GetAdminIdFromToken();
            _logger.LogInformation("Admin {AdminId} unblocking instrument {InstrumentId}", adminId, id);

            var instrument = await _instrumentService.UnblockAsync(id, adminId, cancellationToken);
            return Ok(instrument);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for instrument unblock");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking instrument {InstrumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to unblock instrument" });
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

    /// <summary>
    /// Get admin action audit logs (history of all admin actions)
    /// </summary>
    /// <response code="200">List of admin action audit logs</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin or not connected via VPN</response>
    [HttpGet("audit-history")]
    [ProducesResponseType(typeof(IEnumerable<AdminAuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<AdminAuditLogDto>>> GetAdminAuditHistory(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Admin requesting audit history");

            var logs = await _adminService.GetRecentAdminAuditLogsAsync(cancellationToken: cancellationToken);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin audit history");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve audit history" });
        }
    }

    // ============ FAZA 3: INSTRUMENT LIFECYCLE WORKFLOW ============
    // Enterprise-grade state machine workflow with AdminRequest audit trail
    // Lightweight design: no CQRS, no event sourcing, no message bus
    // All transitions controlled in Service layer (single source of truth)
    // AdminRequest used for audit/history (not event stream)
    // Extensible: future event bus can hook into success responses without refactor

    /// <summary>
    /// Request approval for a draft instrument
    /// Transition: Draft → PendingApproval
    /// Creates AdminRequest for audit trail
    /// </summary>
    /// <param name="id">Instrument ID in Draft state</param>
    /// <response code="200">Approval requested successfully</response>
    /// <response code="400">Invalid state or preconditions not met</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Instrument not found</response>
    /// <response code="409">Instrument already approved or in different state</response>
    [HttpPost("instruments/{id}/request-approval")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InstrumentDto>> RequestApproval(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var adminId = GetAdminIdFromToken();
            _logger.LogInformation("Admin {AdminId} requesting approval for instrument {InstrumentId}", adminId, id);

            // FUTURE EXTENSION POINT: Hook event bus here to notify reviewers (without refactor)
            var instrument = await _instrumentService.RequestApprovalAsync(id, adminId, cancellationToken);
            return Ok(instrument);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid state transition for approval request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting approval for instrument {InstrumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to request approval" });
        }
    }

    /// <summary>
    /// Approve a pending instrument
    /// Transition: PendingApproval → Approved
    /// CRITICAL: Requires approver ≠ creator (self-approval forbidden)
    /// Updates AdminRequest with approval details
    /// </summary>
    /// <param name="id">Instrument ID awaiting approval</param>
    /// <response code="200">Instrument approved successfully</response>
    /// <response code="400">Invalid state or self-approval detected</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Instrument not found</response>
    /// <response code="409">Instrument not in PendingApproval state</response>
    [HttpPost("instruments/{id}/approve")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InstrumentDto>> Approve(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var approverAdminId = GetAdminIdFromToken();
            _logger.LogInformation("Admin {AdminId} approving instrument {InstrumentId}", approverAdminId, id);

            // FUTURE EXTENSION POINT: After approval, publish event to event bus (notification, analytics, etc)
            var instrument = await _instrumentService.ApproveAsync(id, approverAdminId, cancellationToken);
            return Ok(instrument);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Approval failed: {Message}", ex.Message);
            // Self-approval or invalid state
            return ex.Message.Contains("Self-approval") 
                ? BadRequest(new { error = "Self-approval not allowed. Creator cannot approve own submission." })
                : BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving instrument {InstrumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to approve instrument" });
        }
    }

    /// <summary>
    /// Reject a pending instrument with reason
    /// Transition: PendingApproval → Rejected
    /// CRITICAL: Requires rejector ≠ creator (self-approval forbidden)
    /// Reason required (min 10 chars) and stored in AdminRequest.Reason for audit
    /// </summary>
    /// <param name="id">Instrument ID awaiting approval</param>
    /// <param name="request">Rejection request with required reason</param>
    /// <response code="200">Instrument rejected successfully</response>
    /// <response code="400">Invalid state, self-rejection, or reason too short</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Instrument not found</response>
    /// <response code="409">Instrument not in PendingApproval state</response>
    [HttpPost("instruments/{id}/reject")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InstrumentDto>> Reject(
        Guid id,
        [FromBody] RejectInstrumentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var rejectorAdminId = GetAdminIdFromToken();
            _logger.LogInformation("Admin {AdminId} rejecting instrument {InstrumentId}", rejectorAdminId, id);

            // FUTURE EXTENSION POINT: Publish rejection event for notifications/audit dashboards
            var instrument = await _instrumentService.RejectAsync(id, request.Reason, rejectorAdminId, cancellationToken);
            return Ok(instrument);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rejection failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid rejection reason");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting instrument {InstrumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to reject instrument" });
        }
    }

    /// <summary>
    /// Retry a rejected instrument submission
    /// Transition: Rejected → Draft (allows re-editing and resubmission)
    /// Creator or any admin can retry
    /// </summary>
    /// <param name="id">Instrument ID in Rejected state</param>
    /// <response code="200">Retry submitted successfully</response>
    /// <response code="400">Instrument not in Rejected state</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Instrument not found</response>
    [HttpPost("instruments/{id}/retry-submission")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstrumentDto>> RetrySubmission(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var adminId = GetAdminIdFromToken();
            _logger.LogInformation("Admin {AdminId} retrying submission for instrument {InstrumentId}", adminId, id);

            var instrument = await _instrumentService.RetrySubmissionAsync(id, adminId, cancellationToken);
            return Ok(instrument);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Retry submission failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying submission for instrument {InstrumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retry submission" });
        }
    }

    /// <summary>
    /// Archive an approved instrument
    /// Transition: Approved → Archived (soft delete - not removed from DB)
    /// Any admin can archive
    /// </summary>
    /// <param name="id">Instrument ID in Approved state</param>
    /// <response code="200">Instrument archived successfully</response>
    /// <response code="400">Instrument not in Approved state</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Instrument not found</response>
    [HttpPost("instruments/{id}/archive")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstrumentDto>> Archive(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var adminId = GetAdminIdFromToken();
            _logger.LogInformation("Admin {AdminId} archiving instrument {InstrumentId}", adminId, id);

            // FUTURE EXTENSION POINT: Post-archive cleanup (notifications, compliance logging, etc)
            var instrument = await _instrumentService.ArchiveAsync(id, adminId, cancellationToken);
            return Ok(instrument);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Archive failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving instrument {InstrumentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to archive instrument" });
        }
    }

    // ============ END FAZA 3 WORKFLOW ENDPOINTS ============

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
