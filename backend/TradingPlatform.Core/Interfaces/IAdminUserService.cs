using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;

namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Admin User Service Contract - handles all user management operations.
/// Integrates with ApprovalService for critical operations (Delete, Restore).
/// 
/// Pattern:
/// - Immediate operations: GetUsers, BlockUser, UnblockUser
/// - Approval-required: RequestDeleteUser, RequestRestoreUser
/// - Distinction by EntityType in AdminRequest ("User" vs "Instrument")
/// </summary>
public interface IAdminUserService
{
    // ===== READ OPERATIONS =====

    /// <summary>
    /// Get users filtered by status (predictable API - frontend decides what to fetch).
    /// 
    /// NO magic defaults - null = ALL, explicit control.
    /// 
    /// Examples:
    /// - GetUsersAsync(null) → ALL users (Active, Blocked, Deleted)
    /// - GetUsersAsync(UserStatus.Active) → Active users only
    /// - GetUsersAsync(UserStatus.Deleted) → Deleted users only
    /// </summary>
    Task<IEnumerable<UserListItemDto>> GetUsersAsync(
        UserStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get single user by ID (including soft-deleted).
    /// Used for validation in delete/restore requests.
    /// Throws if user not found.
    /// </summary>
    Task<UserListItemDto> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    // ===== USER LIFECYCLE: IMMEDIATE OPERATIONS (NO APPROVAL) =====

    /// <summary>
    /// Block user temporarily (48h default).
    /// Immediate operation - does NOT require approval.
    /// 
    /// BUSINESS DECISION: Block is RESTRICTIVE (does not auto-extend).
    /// If admin wants to extend block, must unblock first, then block again.
    /// This prevents accidental duration changes.
    /// 
    /// Validations:
    /// - User exists
    /// - User is not SuperAdmin
    /// - User is not already blocked with active block
    /// </summary>
    Task<UserListItemDto> BlockUserAsync(
        Guid userId,
        string reason,
        Guid performedByAdminId,
        DateTimeOffset? blockedUntilUtc = null,  // null = 48h from now
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblock user immediately.
    /// Immediate operation - does NOT require approval.
    /// Status: Blocked → Active, clears BlockedUntilUtc.
    /// 
    /// Validations:
    /// - User exists
    /// - User is actually blocked (not active)
    /// </summary>
    Task<UserListItemDto> UnblockUserAsync(
        Guid userId,
        Guid performedByAdminId,
        CancellationToken cancellationToken = default);

    // ===== USER LIFECYCLE: APPROVAL-REQUIRED OPERATIONS =====

    /// <summary>
    /// Request user deletion (creates approval workflow).
    /// Does NOT execute delete - creates AdminRequest for approval.
    /// 
    /// IDEMPOTENT: If pending Delete request already exists, returns it.
    /// This prevents duplicate approval requests from double-clicks.
    /// 
    /// Creates AdminRequest with:
    /// - EntityType: "User" (NOT "Instrument")
    /// - EntityId: userId
    /// - Action: Delete (4) - shared with Instrument Delete
    /// - Status: Pending (awaits approval)
    /// 
    /// Returns: AdminRequestDto for tracking workflow.
    /// 
    /// Validations:
    /// - User exists
    /// - User is not already deleted (cannot double-delete)
    /// - User is not SuperAdmin (protected)
    /// - Admin is not deleting themselves (self-deletion prevention)
    /// </summary>
    Task<AdminRequestDto> RequestDeleteUserAsync(
        Guid userId,
        Guid requestedByAdminId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Request user restoration (creates approval workflow).
    /// Does NOT execute restore - creates AdminRequest for approval.
    /// 
    /// IDEMPOTENT: If pending Restore request already exists, returns it.
    /// This prevents duplicate approval requests from double-clicks.
    /// 
    /// Creates AdminRequest with:
    /// - EntityType: "User" (NOT "Instrument")
    /// - EntityId: userId
    /// - Action: Restore (11) - specific to Users
    /// - Status: Pending (awaits approval)
    /// 
    /// Returns: AdminRequestDto for tracking workflow.
    /// 
    /// Validations:
    /// - User exists
    /// - User is actually deleted (cannot restore active user - edge-case)
    /// </summary>
    Task<AdminRequestDto> RequestRestoreUserAsync(
        Guid userId,
        Guid requestedByAdminId,
        string reason,
        CancellationToken cancellationToken = default);
}