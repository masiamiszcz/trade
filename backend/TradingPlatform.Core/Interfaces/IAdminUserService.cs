using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Service for admin user management operations
/// Single responsibility: handle user blocking, unblocking, and deletion requests
/// Does NOT execute delete - that's handled by ApprovalService + UserApprovalHandler
/// Maintains audit logging for compliance
/// </summary>
public interface IAdminUserService
{
    /// <summary>Get all users for admin dashboard</summary>
    Task<IEnumerable<UserListItemDto>> GetAllUsersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Block a user account with reason and optional duration</summary>
    /// <param name="userId">User ID to block</param>
    /// <param name="reason">Reason for blocking</param>
    /// <param name="durationMs">Duration in milliseconds (0 = permanent)</param>
    /// <param name="requestedByAdminId">ID of admin requesting the block</param>
    /// <returns>Updated user with block status</returns>
    Task<User> BlockUserAsync(
        Guid userId,
        string reason,
        long durationMs,
        Guid requestedByAdminId,
        CancellationToken cancellationToken = default);

    /// <summary>Unblock a user account</summary>
    /// <param name="userId">User ID to unblock</param>
    /// <param name="reason">Reason for unblocking</param>
    /// <param name="requestedByAdminId">ID of admin requesting the unblock</param>
    /// <returns>Updated user</returns>
    Task<User> UnblockUserAsync(
        Guid userId,
        string reason,
        Guid requestedByAdminId,
        CancellationToken cancellationToken = default);

    /// <summary>Create deletion approval request for user</summary>
    /// <remarks>
    /// DOES NOT delete immediately - creates a request that must be approved by another admin
    /// Actual deletion is executed by ExecuteApprovedDeleteAsync after approval
    /// </remarks>
    /// <param name="userId">User ID to delete</param>
    /// <param name="reason">Reason for deletion</param>
    /// <param name="requestedByAdminId">Admin ID requesting deletion</param>
    /// <returns>Admin request DTO</returns>
    Task<AdminRequestDto> CreateDeleteApprovalAsync(
        Guid userId,
        string reason,
        Guid requestedByAdminId,
        CancellationToken cancellationToken = default);

    /// <summary>Execute approved user deletion (soft delete)</summary>
    /// <remarks>
    /// Sets user Status to Deleted and DeletedAtUtc timestamp
    /// Called by ApprovalService after approval workflow completes
    /// </remarks>
    /// <param name="userId">User ID to delete</param>
    /// <param name="request">The approved AdminRequest</param>
    /// <param name="approvedByAdminId">Admin ID who approved the deletion</param>
    /// <returns>Deleted user (Status=Deleted)</returns>
    Task<User> ExecuteApprovedDeleteAsync(
        Guid userId,
        AdminRequest request,
        Guid approvedByAdminId,
        CancellationToken cancellationToken = default);
}