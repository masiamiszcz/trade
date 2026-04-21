
// IAdminService
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Service interface for administrative operations.
/// Handles business logic for admin requests, approvals, and audit logging.
/// Implements two-step approval workflow.
/// </summary>
public interface IAdminService
{
    // ADMIN REQUEST OPERATIONS

    /// <summary>
    /// Get all admin requests with optional filtering
    /// </summary>
    Task<IEnumerable<AdminRequestDto>> GetAllRequestsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending requests awaiting approval
    /// </summary>
    Task<IEnumerable<AdminRequestDto>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all users for admin management
    /// </summary>
    Task<IEnumerable<UserListItemDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific admin request by ID
    /// </summary>
    Task<AdminRequestDto> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default);

    // INSTRUMENT MANAGEMENT

    /// <summary>
    /// Get all instruments for admin management
    /// </summary>
    Task<IEnumerable<InstrumentDto>> GetAllInstrumentsAsync(CancellationToken cancellationToken = default);

    // REQUEST CREATION

    /// <summary>
    /// Create a new block request for an instrument
    /// Does not immediately block - requires approval
    /// </summary>
    Task<AdminRequestDto> CreateBlockRequestAsync(
        Guid instrumentId,
        string reason,
        Guid requestedByAdminId,
        string adminIpAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new unblock request for an instrument
    /// Does not immediately unblock - requires approval
    /// </summary>
    Task<AdminRequestDto> CreateUnblockRequestAsync(
        Guid instrumentId,
        string reason,
        Guid requestedByAdminId,
        string adminIpAddress,
        CancellationToken cancellationToken = default);

    // APPROVAL/REJECTION OPERATIONS

    /// <summary>
    /// Approve an admin request
    /// If approved, executes the requested action (block/unblock/delete)
    /// Creates audit log entry
    /// </summary>
    Task<AdminRequestDto> ApproveRequestAsync(
        Guid requestId,
        Guid approvedByAdminId,
        string adminIpAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reject an admin request without executing the action
    /// Creates audit log entry
    /// </summary>
    Task<AdminRequestDto> RejectRequestAsync(
        Guid requestId,
        Guid rejectedByAdminId,
        string adminIpAddress,
        CancellationToken cancellationToken = default);

    // AUDIT LOG OPERATIONS

    /// <summary>
    /// Get audit logs for a specific admin
    /// </summary>
    Task<IEnumerable<AuditLogDto>> GetAuditLogsByAdminAsync(
        Guid adminId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a specific entity (instrument, request, etc.)
    /// </summary>
    Task<IEnumerable<AuditLogDto>> GetAuditLogsByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all audit logs (paginated)
    /// </summary>
    Task<(IEnumerable<AuditLogDto> Items, int TotalCount)> GetAuditLogsPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // ADMIN AUDIT LOG OPERATIONS

    /// <summary>
    /// Get all admin action audit logs
    /// </summary>
    Task<IEnumerable<AdminAuditLogDto>> GetAdminAuditLogsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get admin action audit logs for specific admin
    /// </summary>
    Task<IEnumerable<AdminAuditLogDto>> GetAdminAuditLogsByAdminIdAsync(Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent admin action audit logs
    /// </summary>
    Task<IEnumerable<AdminAuditLogDto>> GetRecentAdminAuditLogsAsync(int count = 50, CancellationToken cancellationToken = default);
}
