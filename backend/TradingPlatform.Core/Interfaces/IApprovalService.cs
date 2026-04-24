using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Service interface for approval workflow management.
/// Handles approval, rejection, and related operations for admin requests.
/// Single responsibility: manage the approval workflow.
/// </summary>
public interface IApprovalService
{
    // ===== RETRIEVAL OPERATIONS =====

    /// <summary>
    /// Get all admin requests
    /// </summary>
    Task<IEnumerable<AdminRequestDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get only pending admin requests awaiting approval
    /// </summary>
    Task<IEnumerable<AdminRequestDto>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific admin request by ID
    /// </summary>
    Task<AdminRequestDto> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default);

    // ===== APPROVAL/REJECTION OPERATIONS =====

    /// <summary>
    /// Approve a pending admin request
    /// Executes the requested action based on AdminRequestActionType (block, unblock, update, etc.)
    /// Prevents self-approval
    /// Creates comprehensive audit logs
    /// IMPORTANT: Requires IInstrumentService to be passed as parameter to avoid circular dependency
    /// </summary>
    Task<AdminRequestDto> ApproveAsync(
        Guid requestId,
        Guid approvedByAdminId,
        IInstrumentService instrumentService,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reject a pending admin request without executing the action
    /// Creates audit log entry
    /// </summary>
    Task<AdminRequestDto> RejectAsync(
        Guid requestId,
        Guid rejectedByAdminId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a comment to an admin request
    /// Request remains in current status (Pending, Approved, etc.)
    /// </summary>
    Task AddCommentAsync(
        Guid requestId,
        Guid adminId,
        string commentText,
        CancellationToken cancellationToken = default);

    // ===== REQUEST CREATION =====

    /// <summary>
    /// Create a new admin approval request - generic version supporting any entity type
    /// Called by services (InstrumentService, etc.) when requesting an action that requires approval
    /// Handles idempotency checking (returns existing request if payload already pending)
    /// EntityId can be null for CREATE actions (entity doesn't exist yet)
    /// </summary>
    Task<AdminRequestDto> CreateRequestAsync(
        string entityType,
        Guid? entityId,
        AdminRequestActionType action,
        Guid requestedByAdminId,
        string? reason = null,
        string? payloadJson = null,
        CancellationToken cancellationToken = default);
}
