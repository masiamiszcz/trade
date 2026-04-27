using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Models;
using System;

namespace TradingPlatform.Tests.Helpers;

/// <summary>
/// Factory helper for creating test data
/// USAGE: var user = TestDataBuilder.CreateUser(); 
/// Ensures consistent test data across all test files
/// </summary>
public static class TestDataBuilder
{
    private static int _userCounter = 0;
    private static int _adminCounter = 0;
    private static int _requestCounter = 0;

    /// <summary>
    /// Creates test user with default values
    /// Can override specific fields
    /// </summary>
    public static User CreateUser(
        Guid? id = null,
        string? username = null,
        UserStatus status = UserStatus.Active,
        UserRole role = UserRole.User,
        bool emailConfirmed = true,
        DateTimeOffset? blockedUntilUtc = null,
        string? blockReason = null)
    {
        _userCounter++;
        var userId = id ?? Guid.NewGuid();
        var userName = username ?? $"testuser{_userCounter}";

        return new User(
            Id: userId,
            UserName: userName,
            Email: $"{userName}@test.example.com",
            FirstName: "Test",
            LastName: $"User{_userCounter}",
            Role: role,
            EmailConfirmed: emailConfirmed,
            TwoFactorEnabled: false,
            TwoFactorSecret: string.Empty,
            BackupCodes: string.Empty,
            Status: status,
            BaseCurrency: "USD",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            BlockedUntilUtc: blockedUntilUtc,
            DeletedAtUtc: status == UserStatus.Deleted ? DateTimeOffset.UtcNow : null,
            BlockReason: blockReason,
            DeleteReason: status == UserStatus.Deleted ? "Test deletion" : null,
            LastModifiedByAdminId: null);
    }

    /// <summary>
    /// Creates active user (shorthand)
    /// </summary>
    public static User CreateActiveUser(Guid? id = null, string? username = null)
    {
        return CreateUser(id: id, username: username, status: UserStatus.Active);
    }

    /// <summary>
    /// Creates blocked user
    /// </summary>
    public static User CreateBlockedUser(Guid? id = null, string? reason = "Test block")
    {
        return CreateUser(
            id: id,
            status: UserStatus.Blocked,
            blockedUntilUtc: DateTimeOffset.UtcNow.AddDays(7),
            blockReason: reason);
    }

    /// <summary>
    /// Creates deleted user
    /// </summary>
    public static User CreateDeletedUser(Guid? id = null)
    {
        return CreateUser(id: id, status: UserStatus.Deleted);
    }

    /// <summary>
    /// Creates suspended user
    /// </summary>
    public static User CreateSuspendedUser(Guid? id = null)
    {
        return CreateUser(id: id, status: UserStatus.Suspended);
    }

    /// <summary>
    /// Creates pending confirmation user
    /// </summary>
    public static User CreatePendingUser(Guid? id = null)
    {
        return CreateUser(
            id: id,
            status: UserStatus.PendingEmailConfirmation,
            emailConfirmed: false);
    }

    /// <summary>
    /// Creates admin user
    /// </summary>
    public static Admin CreateAdmin(
        Guid? id = null,
        AdminRole role = AdminRole.Admin,
        string? name = null)
    {
        _adminCounter++;
        var adminId = id ?? Guid.NewGuid();
        var adminName = name ?? $"Admin{_adminCounter}";

        return new Admin(
            Id: adminId,
            Name: adminName,
            Email: $"{adminName}@admin.example.com",
            Role: role,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            LastLoginAtUtc: null);
    }

    /// <summary>
    /// Creates SuperAdmin
    /// </summary>
    public static Admin CreateSuperAdmin(Guid? id = null)
    {
        return CreateAdmin(id: id, role: AdminRole.SuperAdmin, name: "SuperAdmin");
    }

    /// <summary>
    /// Creates regular Admin
    /// </summary>
    public static Admin CreateRegularAdmin(Guid? id = null)
    {
        return CreateAdmin(id: id, role: AdminRole.Admin, name: "Admin");
    }

    /// <summary>
    /// Creates approval request for Delete action
    /// </summary>
    public static AdminRequest CreateDeleteApprovalRequest(
        Guid userId,
        Guid? requestId = null,
        Guid? requestedByAdminId = null,
        AdminRequestStatus status = AdminRequestStatus.Pending)
    {
        _requestCounter++;
        var reqId = requestId ?? Guid.NewGuid();
        var adminId = requestedByAdminId ?? Guid.NewGuid();

        return new AdminRequest(
            Id: reqId,
            EntityType: "User",
            EntityId: userId,
            Action: AdminRequestActionType.Delete,
            Status: status,
            RequestedByAdminId: adminId,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: status == AdminRequestStatus.Approved ? Guid.NewGuid() : null,
            ApprovedAtUtc: status == AdminRequestStatus.Approved ? DateTimeOffset.UtcNow : null,
            Reason: "Test delete request",
            PayloadJson: null);
    }

    /// <summary>
    /// Creates approval request for Restore action
    /// </summary>
    public static AdminRequest CreateRestoreApprovalRequest(
        Guid userId,
        Guid? requestId = null,
        Guid? requestedByAdminId = null,
        AdminRequestStatus status = AdminRequestStatus.Pending)
    {
        _requestCounter++;
        var reqId = requestId ?? Guid.NewGuid();
        var adminId = requestedByAdminId ?? Guid.NewGuid();

        return new AdminRequest(
            Id: reqId,
            EntityType: "User",
            EntityId: userId,
            Action: AdminRequestActionType.Restore,
            Status: status,
            RequestedByAdminId: adminId,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: status == AdminRequestStatus.Approved ? Guid.NewGuid() : null,
            ApprovedAtUtc: status == AdminRequestStatus.Approved ? DateTimeOffset.UtcNow : null,
            Reason: "Test restore request",
            PayloadJson: null);
    }

    /// <summary>
    /// Creates generic approval request
    /// </summary>
    public static AdminRequest CreateApprovalRequest(
        string entityType,
        Guid entityId,
        AdminRequestActionType action,
        Guid? requestId = null,
        Guid? requestedByAdminId = null,
        AdminRequestStatus status = AdminRequestStatus.Pending)
    {
        _requestCounter++;
        var reqId = requestId ?? Guid.NewGuid();
        var adminId = requestedByAdminId ?? Guid.NewGuid();

        return new AdminRequest(
            Id: reqId,
            EntityType: entityType,
            EntityId: entityId,
            Action: action,
            Status: status,
            RequestedByAdminId: adminId,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: status == AdminRequestStatus.Approved ? Guid.NewGuid() : null,
            ApprovedAtUtc: status == AdminRequestStatus.Approved ? DateTimeOffset.UtcNow : null,
            Reason: $"Test {action} request",
            PayloadJson: null);
    }

    /// <summary>
    /// Creates audit log entry
    /// </summary>
    public static AuditLog CreateAuditLog(
        string action,
        Guid performedByAdminId,
        string? details = null)
    {
        return new AuditLog(
            Id: Guid.NewGuid(),
            Action: action,
            Details: details ?? $"Test {action}",
            PerformedByAdminId: performedByAdminId,
            CreatedAtUtc: DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Reset counters for test isolation
    /// Call in test setup/cleanup
    /// </summary>
    public static void ResetCounters()
    {
        _userCounter = 0;
        _adminCounter = 0;
        _requestCounter = 0;
    }
}
