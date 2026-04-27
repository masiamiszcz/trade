using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Executes approved actions on User entities (Delete, Restore).
/// 
/// Responsibilities:
/// - Execute user-specific approval actions
/// - Validate user state (double-check before execution)
/// - Create audit logs for actions
/// - Manage user repository persistence
/// 
/// Domain Ownership:
/// This handler owns all user lifecycle rules and execution logic.
/// ApprovalService doesn't know about user details - only calls handler.Execute()
/// </summary>
public interface IUserApprovalHandler
{
    /// <summary>
    /// Execute an approved action on a User entity.
    /// 
    /// Flow:
    /// 1. Validate user exists
    /// 2. Validate status is appropriate for the action (Delete: not deleted, Restore: is deleted)
    /// 3. Execute repository operation
    /// 4. Create audit log entry
    /// 5. ATOMIC: Save user changes + audit log together
    /// </summary>
    /// <param name="request">The approved admin request</param>
    /// <param name="approvedByAdminId">Admin who approved the request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>UserDto with updated state</returns>
    Task<UserDto> ExecuteAsync(
        AdminRequest request,
        Guid approvedByAdminId,
        CancellationToken cancellationToken);
}
