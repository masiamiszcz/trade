using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Handler for executing approved user management actions
/// Follows handler pattern for separation of concerns
/// </summary>
public interface IUserApprovalHandler
{
    /// <summary>
    /// Execute approved user deletion (soft delete)
    /// </summary>
    Task ExecuteApprovedDeleteAsync(
        Guid userId,
        AdminRequest request,
        Guid approvedByAdminId,
        CancellationToken cancellationToken = default);
}
