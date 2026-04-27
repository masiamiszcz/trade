using Microsoft.Extensions.Logging;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

/// <summary>
/// Executes approved actions on User entities (Delete, Restore).
/// 
/// This handler owns the complete domain logic for user lifecycle:
/// - Validates user state
/// - Executes repository operations
/// - Creates audit logs
/// - Manages persistence (ATOMIC operations)
/// 
/// ApprovalService calls this handler, not the repository directly.
/// This ensures clean separation: ApprovalService is only orchestrator.
/// </summary>
public class UserApprovalHandler : IUserApprovalHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<UserApprovalHandler> _logger;

    public UserApprovalHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<UserApprovalHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute an approved action on a User entity (Delete or Restore).
    /// 
    /// Flow:
    /// 1. Validate user still exists + status is appropriate for the action
    /// 2. Execute repository operation (Delete or Restore)
    /// 3. Create audit log entry
    /// 4. ATOMIC: Save user changes + audit log together
    /// </summary>
    public async Task<UserDto> ExecuteAsync(
        AdminRequest request,
        Guid approvedByAdminId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Executing User action {Action} on user {UserId}",
            request.Action, request.EntityId);

        // Validation: User actions must have EntityId
        if (request.EntityId == null)
            throw new InvalidOperationException("User action requires EntityId");

        // Validation: User still exists (state might have changed since request was created)
        var user = await _userRepository.GetUserByIdIncludingDeletedAsync(request.EntityId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"User {request.EntityId} not found");

        // Execute action based on action type
        switch (request.Action)
        {
            case AdminRequestActionType.Delete:
                await ExecuteDeleteAsync(user, request, approvedByAdminId, cancellationToken);
                break;

            case AdminRequestActionType.Restore:
                await ExecuteRestoreAsync(user, request, approvedByAdminId, cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Unsupported User action: {request.Action}");
        }

        _logger.LogInformation(
            "User action {Action} executed and audited for user {UserId}",
            request.Action, request.EntityId);

        // Return updated user DTO
        var updatedUser = await _userRepository.GetUserByIdIncludingDeletedAsync(request.EntityId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"User {request.EntityId} not found after execution");

        return MapToDto(updatedUser);
    }

    /// <summary>
    /// Execute DELETE action on a user (soft delete).
    /// </summary>
    private async Task ExecuteDeleteAsync(
        User user,
        AdminRequest request,
        Guid approvedByAdminId,
        CancellationToken cancellationToken)
    {
        // Validation: User must not already be deleted
        if (user.Status == UserStatus.Deleted)
            throw new InvalidOperationException($"User {request.EntityId} is already deleted");

        // Execute delete (soft delete)
        await _userRepository.DeleteUserAsync(
            request.EntityId!.Value,
            request.Reason ?? "Deleted by admin approval",
            approvedByAdminId,
            cancellationToken);

        _logger.LogInformation("User {UserId} deleted successfully", request.EntityId);

        // ATOMIC: Save user changes
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Create audit log for the deletion
        var auditLog = CreateAuditLogEntry(
            adminId: approvedByAdminId,
            action: "USER_DELETE",
            userId: request.EntityId!.Value,
            request: request);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        // ATOMIC: Save audit log
        await _auditLogRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Execute RESTORE action on a user (undo soft delete).
    /// </summary>
    private async Task ExecuteRestoreAsync(
        User user,
        AdminRequest request,
        Guid approvedByAdminId,
        CancellationToken cancellationToken)
    {
        // Validation: User must be deleted to restore
        if (user.Status != UserStatus.Deleted)
            throw new InvalidOperationException($"User {request.EntityId} is not deleted (current status: {user.Status})");

        // Execute restore
        await _userRepository.RestoreUserAsync(
            request.EntityId!.Value,
            approvedByAdminId,
            cancellationToken);

        _logger.LogInformation("User {UserId} restored successfully", request.EntityId);

        // ATOMIC: Save user changes
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Create audit log for the restoration
        var auditLog = CreateAuditLogEntry(
            adminId: approvedByAdminId,
            action: "USER_RESTORE",
            userId: request.EntityId!.Value,
            request: request);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        // ATOMIC: Save audit log
        await _auditLogRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Create audit log entry for user action.
    /// </summary>
    private AuditLog CreateAuditLogEntry(
        Guid adminId,
        string action,
        Guid userId,
        AdminRequest request)
    {
        var details = System.Text.Json.JsonSerializer.Serialize(new
        {
            userId = userId,
            actionType = request.Action.ToString(),
            approvalRequestId = request.Id,
            reason = request.Reason,
            executionTimestamp = DateTimeOffset.UtcNow
        });

        return new AuditLog(
            Id: Guid.NewGuid(),
            AdminId: adminId,
            Action: action,
            EntityType: "User",
            EntityId: userId,
            Details: details,
            IpAddress: "Internal", // Internal action, no client IP
            CreatedAtUtc: DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Maps User domain model to UserDto for API response
    /// </summary>
    private UserDto MapToDto(User user)
    {
        return new UserDto(
            Id: user.Id,
            UserName: user.UserName,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: user.Role.ToString(),
            EmailConfirmed: user.EmailConfirmed,
            TwoFactorEnabled: user.TwoFactorEnabled,
            Status: user.Status.ToString(),
            BaseCurrency: user.BaseCurrency,
            CreatedAtUtc: user.CreatedAtUtc);
    }
}
