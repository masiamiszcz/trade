using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

/// <summary>
/// Handler for executing approved user management actions
/// Handles deletion, blocking, unblocking, and other admin operations on users
/// </summary>
public sealed class UserApprovalHandler : IUserApprovalHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<UserApprovalHandler> _logger;

    public UserApprovalHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<UserApprovalHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// Execute approved user deletion (soft delete)
    /// Sets user Status to Deleted and DeletedAtUtc timestamp
    /// </summary>
    public async Task ExecuteApprovedDeleteAsync(
        Guid userId,
        AdminRequest request,
        Guid approvedByAdminId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing approved deletion for user {UserId} (approved by admin {ApprovedByAdminId})",
            userId,
            approvedByAdminId);

        // Get the user
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User with ID {userId} not found");

        // Perform soft delete
        var deletedUser = user with
        {
            Status = UserStatus.Deleted,
            DeletedAtUtc = DateTimeOffset.UtcNow
        };

        // Update the user in database
        await _userRepository.UpdateAsync(deletedUser, cancellationToken);

        _logger.LogInformation(
            "✅ User {UserId} successfully deleted (status set to Deleted)",
            userId);

        // Log the deletion with full audit trail
        var detailsObj = new
        {
            userId = userId,
            reason = request.Reason,
            requestedBy = request.RequestedByAdminId,
            approvedBy = approvedByAdminId,
            executedAt = DateTimeOffset.UtcNow,
            userEmail = user.Email,
            userName = user.UserName
        };

        var auditLog = new AuditLog(
            Id: Guid.NewGuid(),
            AdminId: approvedByAdminId,
            Action: "USER_DELETE",
            EntityType: "User",
            EntityId: userId,
            Details: JsonSerializer.Serialize(detailsObj, new JsonSerializerOptions { WriteIndented = true }),
            IpAddress: "N/A",
            CreatedAtUtc: DateTimeOffset.UtcNow
        );

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        _logger.LogInformation(
            "Audit log created for user deletion: {UserId} by admin {ApprovedByAdminId}",
            userId,
            approvedByAdminId);
    }
}
