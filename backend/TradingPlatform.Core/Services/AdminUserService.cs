using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

/// <summary>
/// Service for admin user management operations
/// Single responsibility: handle user blocking/unblocking and deletion requests
/// Does NOT execute delete - that's handled by UserApprovalHandler
/// Calls ApprovalService to create deletion requests
/// Maintains audit logging for compliance
/// </summary>
public sealed class AdminUserService : IAdminUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IApprovalService _approvalService;
    private readonly IMapper _mapper;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IApprovalService approvalService,
        IMapper mapper,
        ILogger<AdminUserService> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _approvalService = approvalService;
        _mapper = mapper;
        _logger = logger;
    }

    // ===== RETRIEVAL OPERATIONS =====

    public async Task<IEnumerable<UserListItemDto>> GetAllUsersAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all users for admin dashboard");

        var users = await _userRepository.GetAllUsersAsync(cancellationToken);
        var dtos = users.Select(_mapper.Map<UserListItemDto>);

        return dtos;
    }

    // ===== BLOCKING/UNBLOCKING OPERATIONS =====

    public async Task<User> BlockUserAsync(
        Guid userId,
        string reason,
        long durationMs,
        Guid requestedByAdminId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "🔒 [BLOCK] Starting block operation for user {UserId}, duration {Duration}ms, reason: {Reason}, requested by admin {AdminId}",
            userId,
            durationMs,
            reason,
            requestedByAdminId);

        try
        {
            // Get user
            _logger.LogInformation("🔍 [BLOCK] Fetching user from repository");
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new InvalidOperationException($"User with ID {userId} not found");

            _logger.LogInformation(
                "✓ [BLOCK] User found: {UserName} (ID: {UserId}, CurrentStatus: {Status})",
                user.UserName,
                user.Id,
                user.Status);

            // Validate: cannot block a deleted user
            if (user.IsDeleted)
            {
                _logger.LogWarning("❌ [BLOCK] Cannot block deleted user {UserId}", userId);
                throw new InvalidOperationException("Cannot block a deleted user account");
            }

            // Calculate blocked until timestamp
            DateTimeOffset? blockedUntil = durationMs == 0
                ? null // permanent block
                : DateTimeOffset.UtcNow.AddMilliseconds(durationMs);

            _logger.LogInformation(
                "📅 [BLOCK] Calculated blockedUntil: {BlockedUntil} (permanent: {IsPermanent})",
                blockedUntil,
                durationMs == 0);

            // Update user with block status
            var blockedUser = user with
            {
                Status = UserStatus.Blocked,
                BlockReason = reason?.Trim(),
                BlockedUntilUtc = blockedUntil
            };

            _logger.LogInformation(
                "💾 [BLOCK] Updating user in repository (Status: {Status}, Reason: {Reason})",
                blockedUser.Status,
                blockedUser.BlockReason);

            await _userRepository.UpdateAsync(blockedUser, cancellationToken);

            _logger.LogInformation("✓ [BLOCK] User updated successfully in database");

            // Log the block action WITH requestedByAdminId
            _logger.LogInformation("📝 [BLOCK] Creating audit log entry with AdminId: {AdminId}", requestedByAdminId);
            var auditLog = CreateAuditLogEntry(
                adminId: requestedByAdminId,
                action: "BLOCK_USER",
                entityType: "User",
                entityId: userId,
                details: new
                {
                    userName = user.UserName,
                    email = user.Email,
                    reason,
                    durationMs,
                    isPermanent = durationMs == 0,
                    blockedUntilUtc = blockedUntil,
                    blockTimestamp = DateTimeOffset.UtcNow,
                    blockedByAdminId = requestedByAdminId
                });

            _logger.LogInformation("💾 [BLOCK] Adding audit log to repository");
            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("💾 [BLOCK] Saving user changes");
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("💾 [BLOCK] Saving audit log changes");
            await _auditLogRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "✅ [BLOCK] User {UserId} ({UserName}) blocked successfully. Permanent: {IsPermanent}, UntilUtc: {UntilUtc}, RequestedByAdmin: {AdminId}",
                userId,
                blockedUser.UserName,
                durationMs == 0,
                blockedUntil,
                requestedByAdminId);

            return blockedUser;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "❌ [BLOCK] User not found: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [BLOCK] Error blocking user {UserId}: {Message}", userId, ex.Message);
            _logger.LogError(ex, "❌ [BLOCK] Stack trace: {StackTrace}", ex.StackTrace);
            throw;
        }
    }

    public async Task<User> UnblockUserAsync(
        Guid userId,
        string reason,
        Guid requestedByAdminId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "🔓 [UNBLOCK] Processing unblock for user {UserId} by admin {AdminId}",
            userId,
            requestedByAdminId);

        // Get user
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User with ID {userId} not found");

        _logger.LogInformation(
            "✓ [UNBLOCK] User found: {UserName} (ID: {UserId}, CurrentStatus: {Status})",
            user.UserName,
            user.Id,
            user.Status);

        // Validate: cannot unblock a deleted user
        if (user.IsDeleted)
        {
            _logger.LogWarning("❌ [UNBLOCK] Cannot unblock deleted user {UserId}", userId);
            throw new InvalidOperationException("Cannot unblock a deleted user account");
        }

        // Update user with unblock status
        var unblockedUser = user with
        {
            Status = UserStatus.Active,
            BlockReason = null,
            BlockedUntilUtc = null
        };

        _logger.LogInformation("💾 [UNBLOCK] Updating user in repository");
        await _userRepository.UpdateAsync(unblockedUser, cancellationToken);

        // Log the unblock action WITH requestedByAdminId
        _logger.LogInformation("📝 [UNBLOCK] Creating audit log entry with AdminId: {AdminId}", requestedByAdminId);
        var auditLog = CreateAuditLogEntry(
            adminId: requestedByAdminId,
            action: "UNBLOCK_USER",
            entityType: "User",
            entityId: userId,
            details: new
            {
                userName = user.UserName,
                email = user.Email,
                reason,
                unblockTimestamp = DateTimeOffset.UtcNow,
                unblockedByAdminId = requestedByAdminId
            });

        _logger.LogInformation("💾 [UNBLOCK] Saving changes");
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "✅ [UNBLOCK] User {UserId} ({UserName}) unblocked successfully by admin {AdminId}",
            userId,
            unblockedUser.UserName,
            requestedByAdminId);

        return unblockedUser;
    }

    // ===== DELETION REQUEST OPERATIONS =====

    public async Task<AdminRequestDto> CreateDeleteApprovalAsync(
        Guid userId,
        string reason,
        Guid requestedByAdminId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating delete approval request for user {UserId} by admin {AdminId}",
            userId,
            requestedByAdminId);

        // Verify user exists
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User with ID {userId} not found");

        // Prevent self-deletion (only SuperAdmin can delete their own account via approval)
        if (userId == requestedByAdminId)
        {
            throw new InvalidOperationException("An admin cannot request deletion of their own account");
        }

        // Create approval request via ApprovalService
        var request = await _approvalService.CreateRequestAsync(
            entityType: "User",
            entityId: userId,
            action: AdminRequestActionType.Delete,
            requestedByAdminId: requestedByAdminId,
            reason: reason,
            payloadJson: JsonSerializer.Serialize(new { userId, userName = user.UserName }),
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Delete approval request created: {RequestId} for user {UserId}",
            request.Id,
            userId);

        return request;
    }

    public async Task<User> ExecuteApprovedDeleteAsync(
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
        await _userRepository.SaveChangesAsync(cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Audit log created for user deletion: {UserId} by admin {ApprovedByAdminId}",
            userId,
            approvedByAdminId);

        return deletedUser;
    }

    // ===== HELPER METHODS =====

    /// <summary>
    /// Creates an audit log entry with standardized format
    /// </summary>
    private static AuditLog CreateAuditLogEntry(
        Guid adminId,
        string action,
        string? entityType,
        Guid? entityId,
        object details)
    {
        return new AuditLog(
            Id: Guid.NewGuid(),
            AdminId: adminId,
            Action: action,
            EntityType: entityType,
            EntityId: entityId,
            Details: JsonSerializer.Serialize(details, new JsonSerializerOptions { WriteIndented = true }),
            IpAddress: "N/A",
            CreatedAtUtc: DateTimeOffset.UtcNow);
    }
}
