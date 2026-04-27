using Xunit;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TradingPlatform.Tests.Helpers;

/// <summary>
/// Common assertion helpers for tests
/// Reduces copy-paste and improves assertion clarity
/// 
/// USAGE: AssertionHelpers.AssertUserIsDeleted(user);
/// </summary>
public static class AssertionHelpers
{
    // ========== USER STATUS ASSERTIONS ==========

    public static void AssertUserIsActive(User user)
    {
        Assert.NotNull(user);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Null(user.DeletedAtUtc);
        Assert.Null(user.BlockedUntilUtc);
        Assert.Null(user.BlockReason);
    }

    public static void AssertUserIsDeleted(User user)
    {
        Assert.NotNull(user);
        Assert.Equal(UserStatus.Deleted, user.Status);
        Assert.NotNull(user.DeletedAtUtc);
        Assert.NotNull(user.DeleteReason);
    }

    public static void AssertUserIsBlocked(User user, DateTimeOffset? expectedUntil = null)
    {
        Assert.NotNull(user);
        Assert.Equal(UserStatus.Blocked, user.Status);
        Assert.NotNull(user.BlockedUntilUtc);
        Assert.NotNull(user.BlockReason);
        
        if (expectedUntil.HasValue)
        {
            // Allow small time delta
            Assert.True(
                Math.Abs((user.BlockedUntilUtc.Value - expectedUntil.Value).TotalSeconds) < 5,
                $"BlockedUntilUtc mismatch: expected ~{expectedUntil}, got {user.BlockedUntilUtc}");
        }
    }

    public static void AssertUserIsSuspended(User user)
    {
        Assert.NotNull(user);
        Assert.Equal(UserStatus.Suspended, user.Status);
    }

    public static void AssertUserIsPending(User user)
    {
        Assert.NotNull(user);
        Assert.Equal(UserStatus.PendingEmailConfirmation, user.Status);
        Assert.False(user.EmailConfirmed);
    }

    // ========== ADMIN REQUEST ASSERTIONS ==========

    public static void AssertRequestIsPending(AdminRequest request)
    {
        Assert.NotNull(request);
        Assert.Equal(AdminRequestStatus.Pending, request.Status);
        Assert.Null(request.ApprovedByAdminId);
        Assert.Null(request.ApprovedAtUtc);
    }

    public static void AssertRequestIsApproved(AdminRequest request)
    {
        Assert.NotNull(request);
        Assert.Equal(AdminRequestStatus.Approved, request.Status);
        Assert.NotNull(request.ApprovedByAdminId);
        Assert.NotNull(request.ApprovedAtUtc);
    }

    public static void AssertRequestIsRejected(AdminRequest request)
    {
        Assert.NotNull(request);
        Assert.Equal(AdminRequestStatus.Rejected, request.Status);
        Assert.Null(request.ApprovedByAdminId);
    }

    public static void AssertRequestAction(AdminRequest request, AdminRequestActionType expectedAction)
    {
        Assert.NotNull(request);
        Assert.Equal(expectedAction, request.Action);
    }

    public static void AssertRequestEntity(AdminRequest request, string entityType, Guid entityId)
    {
        Assert.NotNull(request);
        Assert.Equal(entityType, request.EntityType);
        Assert.Equal(entityId, request.EntityId);
    }

    // ========== AUDIT LOG ASSERTIONS ==========

    public static void AssertAuditLogExists(List<AuditLog> logs, string expectedAction, Guid performedByAdminId)
    {
        Assert.NotNull(logs);
        Assert.NotEmpty(logs);
        
        var relevantLog = logs.FirstOrDefault(l => 
            l.Action == expectedAction && 
            l.PerformedByAdminId == performedByAdminId);

        Assert.NotNull(relevantLog);
    }

    public static void AssertAuditLogContains(AuditLog log, string expectedAction, string expectedDetailPart)
    {
        Assert.NotNull(log);
        Assert.Equal(expectedAction, log.Action);
        Assert.Contains(expectedDetailPart, log.Details);
    }

    // ========== ROLE ASSERTIONS ==========

    public static void AssertUserRole(User user, UserRole expectedRole)
    {
        Assert.NotNull(user);
        Assert.Equal(expectedRole, user.Role);
    }

    public static void AssertAdminRole(Admin admin, AdminRole expectedRole)
    {
        Assert.NotNull(admin);
        Assert.Equal(expectedRole, admin.Role);
    }

    public static void AssertIsSuperAdmin(Admin admin)
    {
        AssertAdminRole(admin, AdminRole.SuperAdmin);
    }

    public static void AssertIsRegularAdmin(Admin admin)
    {
        AssertAdminRole(admin, AdminRole.Admin);
    }

    // ========== DATETIME ASSERTIONS ==========

    public static void AssertRecentTimestamp(DateTimeOffset timestamp, int toleranceSeconds = 5)
    {
        var delta = Math.Abs((DateTimeOffset.UtcNow - timestamp).TotalSeconds);
        Assert.True(delta < toleranceSeconds, $"Timestamp not recent: {timestamp}");
    }

    public static void AssertFutureTimestamp(DateTimeOffset? timestamp, int minSecondsInFuture = 1)
    {
        Assert.NotNull(timestamp);
        var delta = (timestamp.Value - DateTimeOffset.UtcNow).TotalSeconds;
        Assert.True(delta >= minSecondsInFuture, $"Timestamp not in future: {timestamp}");
    }

    // ========== EMAIL ASSERTIONS ==========

    public static void AssertEmailConfirmed(User user)
    {
        Assert.NotNull(user);
        Assert.True(user.EmailConfirmed, "User email should be confirmed");
    }

    public static void AssertEmailNotConfirmed(User user)
    {
        Assert.NotNull(user);
        Assert.False(user.EmailConfirmed, "User email should not be confirmed");
    }

    // ========== 2FA ASSERTIONS ==========

    public static void AssertTwoFactorEnabled(User user)
    {
        Assert.NotNull(user);
        Assert.True(user.TwoFactorEnabled, "2FA should be enabled");
        Assert.NotEmpty(user.TwoFactorSecret);
    }

    public static void AssertTwoFactorDisabled(User user)
    {
        Assert.NotNull(user);
        Assert.False(user.TwoFactorEnabled, "2FA should be disabled");
    }

    // ========== COLLECTION ASSERTIONS ==========

    public static void AssertContainsUser(List<User> users, Guid userId)
    {
        Assert.NotNull(users);
        Assert.Contains(users, u => u.Id == userId);
    }

    public static void AssertNotContainsUser(List<User> users, Guid userId)
    {
        Assert.NotNull(users);
        Assert.DoesNotContain(users, u => u.Id == userId);
    }

    public static void AssertUserCount(List<User> users, int expectedCount)
    {
        Assert.NotNull(users);
        Assert.Equal(expectedCount, users.Count);
    }

    public static void AssertNoDeletedUsers(List<User> users)
    {
        Assert.NotNull(users);
        Assert.DoesNotContain(users, u => u.Status == UserStatus.Deleted);
    }

    public static void AssertAllActive(List<User> users)
    {
        Assert.NotNull(users);
        Assert.NotEmpty(users);
        Assert.All(users, u => Assert.Equal(UserStatus.Active, u.Status));
    }

    // ========== REASON/MESSAGE ASSERTIONS ==========

    public static void AssertHasReason(User user, string expectedReason)
    {
        Assert.NotNull(user);
        
        if (user.Status == UserStatus.Blocked)
        {
            Assert.NotNull(user.BlockReason);
            Assert.Contains(expectedReason, user.BlockReason);
        }
        else if (user.Status == UserStatus.Deleted)
        {
            Assert.NotNull(user.DeleteReason);
            Assert.Contains(expectedReason, user.DeleteReason);
        }
    }

    // ========== IDENTITY ASSERTIONS ==========

    public static void AssertUserInfo(User user, string expectedUsername, string expectedEmail)
    {
        Assert.NotNull(user);
        Assert.Equal(expectedUsername, user.UserName);
        Assert.Equal(expectedEmail, user.Email);
    }

    public static void AssertAdminInfo(Admin admin, string expectedName, string expectedEmail)
    {
        Assert.NotNull(admin);
        Assert.Equal(expectedName, admin.Name);
        Assert.Equal(expectedEmail, admin.Email);
    }
}
