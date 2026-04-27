

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

    /// <response code="200">List of all instruments</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin or not connected via VPN</response>
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
    /// Get all admin users
    /// </summary>
    /// <response code="200">List of all users</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>

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
