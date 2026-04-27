
using Microsoft.Extensions.Logging;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

/// <summary>
/// Admin User Service - handles user management operations.
/// Integrates with:
/// - IUserRepository: for user persistence
/// - IApprovalService: for Delete/Restore workflows
/// - IAuditLogRepository: for audit trail
/// </summary>
public class AdminUsersService : IAdminUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IApprovalService _approvalService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AdminUsersService> _logger;

    public AdminUsersService(
        IUserRepository userRepository,
        IApprovalService approvalService,
        IAuditLogRepository auditLogRepository,
        ILogger<AdminUsersService> logger)
    {
        _userRepository = userRepository;
        _approvalService = approvalService;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    // ===== READ OPERATIONS =====

    /// <summary>
    /// Get users filtered by status (predictable API - frontend decides what to fetch).
    /// 
    /// Examples:
    /// - GetUsersAsync(null) → ALL users (Active, Blocked, Deleted)
    /// - GetUsersAsync(UserStatus.Active) → Active users only
    /// - GetUsersAsync(UserStatus.Deleted) → Deleted users only
    /// 
    /// NO magic defaults - frontend controls the query.
    /// </summary>
    public async Task<IEnumerable<UserListItemDto>> GetUsersAsync(
        UserStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving users with status filter: {Status}", status?.ToString() ?? "All");

        // Determine if we need to include deleted users
        bool includeDeleted = status == null || status == UserStatus.Deleted;
        var users = await _userRepository.GetAllUsersAsync(includeDeleted, cancellationToken);

        // Apply status filter
        IEnumerable<UserListItemDto> dtos;
        if (status.HasValue)
        {
            dtos = users
                .Where(u => u.Status == status)
                .Select(MapToDto)
                .ToList();
        }
        else
        {
            dtos = users.Select(MapToDto).ToList();
        }

        _logger.LogInformation("Retrieved {Count} users", dtos.Count());
        return dtos;
    }

    /// <summary>
    /// Get single user by ID (including soft-deleted).
    /// Throws if user not found.
    /// </summary>
    public async Task<UserListItemDto> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving user {UserId}", userId);

        var user = await _userRepository.GetUserByIdIncludingDeletedAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} not found");

        return MapToDto(user);
    }

    // ===== USER LIFECYCLE: IMMEDIATE OPERATIONS (NO APPROVAL) =====

    /// <summary>
    /// Block user temporarily (48h default).
    /// Immediate operation - does NOT require approval.
    /// Calls repo directly.
    /// 
    /// BUSINESS DECISION: Block is RESTRICTIVE (does not auto-extend).
    /// If admin wants to extend block:
    /// 1. Call UnblockUserAsync() first
    /// 2. Then BlockUserAsync() again
    /// This prevents accidental block duration changes.
    /// 
    /// Alternative pattern exists in some systems (auto-extend = reset 48h),
    /// but this implementation chose explicit control.
    /// </summary>
    public async Task<UserListItemDto> BlockUserAsync(
        Guid userId,
        string reason,
        Guid performedByAdminId,
        DateTimeOffset? blockedUntilUtc = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Block request for user {UserId} by admin {AdminId}", userId, performedByAdminId);

        // Validation: User exists
        var user = await _userRepository.GetUserByIdIncludingDeletedAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} not found");

        // Validation: Not SuperAdmin
        if (user.Role == UserRole.SuperAdmin)
            throw new InvalidOperationException("Cannot block SuperAdmin users");

        // Validation: Not already blocked with active block (RESTRICTIVE pattern)
        // To extend block, admin must unblock first, then block again
        if (user.Status == UserStatus.Blocked && user.BlockedUntilUtc.HasValue && user.BlockedUntilUtc > DateTimeOffset.UtcNow)
            throw new InvalidOperationException($"User {userId} is already blocked until {user.BlockedUntilUtc}. Unblock first to change duration.");

        // Set default block duration (48h)
        blockedUntilUtc ??= DateTimeOffset.UtcNow.AddHours(48);

        // Execute block (immediate, no approval)
        await _userRepository.BlockUserAsync(userId, reason, blockedUntilUtc.Value, performedByAdminId, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Audit log
        await LogAuditAsync("USER_BLOCKED", userId, performedByAdminId, reason, cancellationToken);

        var blockedUser = await _userRepository.GetUserByIdIncludingDeletedAsync(userId, cancellationToken);
        if (blockedUser is null)
            throw new InvalidOperationException($"User {userId} not found after block operation");
        
        return MapToDto(blockedUser);
    }

    /// <summary>
    /// Unblock user immediately.
    /// Immediate operation - does NOT require approval.
    /// Calls repo directly.
    /// </summary>
    public async Task<UserListItemDto> UnblockUserAsync(
        Guid userId,
        Guid performedByAdminId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unblock request for user {UserId} by admin {AdminId}", userId, performedByAdminId);

        // Validation: User exists
        var user = await _userRepository.GetUserByIdIncludingDeletedAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} not found");

        // Validation: User is actually blocked
        if (user.Status != UserStatus.Blocked)
            throw new InvalidOperationException($"User {userId} is not blocked (status: {user.Status})");

        // Execute unblock (immediate, no approval)
        await _userRepository.UnblockUserAsync(userId, performedByAdminId, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Audit log
        await LogAuditAsync("USER_UNBLOCKED", userId, performedByAdminId, null, cancellationToken);

        var unblockedUser = await _userRepository.GetUserByIdIncludingDeletedAsync(userId, cancellationToken);
        if (unblockedUser is null)
            throw new InvalidOperationException($"User {userId} not found after unblock operation");
        
        return MapToDto(unblockedUser);
    }

    // ===== USER LIFECYCLE: APPROVAL-REQUIRED OPERATIONS =====

    /// <summary>
    /// Request user deletion (creates approval workflow).
    /// Does NOT execute delete - creates AdminRequest for approval.
    /// 
    /// Idempotent: If pending Delete request already exists, returns it instead of creating new.
    /// </summary>
    public async Task<AdminRequestDto> RequestDeleteUserAsync(
        Guid userId,
        Guid requestedByAdminId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Delete request for user {UserId} by admin {AdminId}", userId, requestedByAdminId);

        // Validation: User exists
        var user = await _userRepository.GetUserByIdIncludingDeletedAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} not found");

        // Validation: Not already deleted
        if (user.Status == UserStatus.Deleted)
            throw new InvalidOperationException($"User {userId} is already deleted");

        // Validation: Not SuperAdmin
        if (user.Role == UserRole.SuperAdmin)
            throw new InvalidOperationException("Cannot delete SuperAdmin users");

        // Validation: Not self-deletion
        if (userId == requestedByAdminId)
            throw new InvalidOperationException("An admin cannot delete their own account");

        // IDEMPOTENCY: Check if pending Delete request already exists for this user
        // (ApprovalService.CreateRequestAsync will return existing if payloadJson matches,
        // but for User Delete payloadJson=null, so we check here explicitly)
        var existingRequests = await _approvalService.GetAllAsync(cancellationToken);
        var existingDeleteRequest = existingRequests.FirstOrDefault(r =>
            r.EntityType == "User" &&
            r.EntityId == userId &&
            r.Action == AdminRequestActionType.Delete.ToString() &&
            r.Status == AdminRequestStatus.Pending.ToString());

        if (existingDeleteRequest is not null)
        {
            _logger.LogInformation(
                "Idempotent Delete request detected for user {UserId}. Returning existing request {RequestId}",
                userId, existingDeleteRequest.Id);
            return existingDeleteRequest;
        }

        // Create new approval request
        var request = await _approvalService.CreateRequestAsync(
            entityType: "User",
            entityId: userId,
            action: AdminRequestActionType.Delete,
            requestedByAdminId: requestedByAdminId,
            reason: reason,
            payloadJson: null,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Delete request created: {RequestId}", request.Id);
        return request;
    }

    /// <summary>
    /// Request user restoration (creates approval workflow).
    /// Does NOT execute restore - creates AdminRequest for approval.
    /// 
    /// Idempotent: If pending Restore request already exists, returns it instead of creating new.
    /// </summary>
    public async Task<AdminRequestDto> RequestRestoreUserAsync(
        Guid userId,
        Guid requestedByAdminId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restore request for user {UserId} by admin {AdminId}", userId, requestedByAdminId);

        // Validation: User exists
        var user = await _userRepository.GetUserByIdIncludingDeletedAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} not found");

        // Validation: User is actually deleted
        if (user.Status != UserStatus.Deleted)
            throw new InvalidOperationException($"User {userId} is not deleted (status: {user.Status})");

        // IDEMPOTENCY: Check if pending Restore request already exists for this user
        var existingRequests = await _approvalService.GetAllAsync(cancellationToken);
        var existingRestoreRequest = existingRequests.FirstOrDefault(r =>
            r.EntityType == "User" &&
            r.EntityId == userId &&
            r.Action == AdminRequestActionType.Restore.ToString() &&
            r.Status == AdminRequestStatus.Pending.ToString());

        if (existingRestoreRequest is not null)
        {
            _logger.LogInformation(
                "Idempotent Restore request detected for user {UserId}. Returning existing request {RequestId}",
                userId, existingRestoreRequest.Id);
            return existingRestoreRequest;
        }

        // Create new approval request
        var request = await _approvalService.CreateRequestAsync(
            entityType: "User",
            entityId: userId,
            action: AdminRequestActionType.Restore,
            requestedByAdminId: requestedByAdminId,
            reason: reason,
            payloadJson: null,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Restore request created: {RequestId}", request.Id);
        return request;
    }

    // ===== HELPER METHODS =====

    private static UserListItemDto MapToDto(User user)
        => new UserListItemDto(
            user.Id,
            user.UserName,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.Status.ToString(),
            user.CreatedAtUtc);

    private async Task LogAuditAsync(
        string action,
        Guid userId,
        Guid performedByAdminId,
        string? details,
        CancellationToken cancellationToken)
    {
        var auditLog = new AuditLog(
            Id: Guid.NewGuid(),
            AdminId: performedByAdminId,
            Action: action,
            EntityType: "User",
            EntityId: userId,
            Details: details ?? string.Empty,
            IpAddress: "N/A", // TODO: Extract from HttpContext
            CreatedAtUtc: DateTimeOffset.UtcNow);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);
    }
}