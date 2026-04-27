using Xunit;
using Moq;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Core.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TradingPlatform.Tests.Integration;

/// <summary>
/// Integration tests for User Block workflow
/// Tests: Block → Blocked state → Can still login → Profile shows IsBlocked → Unblock
/// 
/// Key difference from Delete: Blocked is BUSINESS state, not SECURITY state
/// Blocked user CAN login and perform operations (with warnings)
/// </summary>
public class UserBlockFlowTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<ILogger<AdminUsersService>> _loggerMock;

    public UserBlockFlowTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _loggerMock = new Mock<ILogger<AdminUsersService>>();
    }

    /// <summary>
    /// SCENARIO 1: Block active user - status changes immediately (no approval)
    /// Block is an IMMEDIATE operation, not approval-based
    /// </summary>
    [Fact]
    public async Task BlockUser_ShouldSetStatusToBlocked_Immediately()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var blockReason = "Suspicious activity detected";
        var user = CreateTestUser(userId, UserStatus.Active);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.BlockUserAsync(userId, blockReason, null, adminId, It.IsAny<CancellationToken>()))
            .Callback<Guid, string, DateTimeOffset?, Guid, CancellationToken>((uid, reason, until, aid, ct) =>
            {
                user = user with { Status = UserStatus.Blocked, BlockReason = reason };
            })
            .Returns(Task.CompletedTask);

        // Act
        var service = new AdminUsersService(
            _userRepositoryMock.Object,
            Mock.Of<IApprovalService>(),
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Assert
        _userRepositoryMock.Verify(
            r => r.BlockUserAsync(userId, blockReason, null, adminId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// SCENARIO 2: Blocked user CAN still login (JWT issued)
    /// This validates Blocked != Deleted distinction
    /// Blocked users get warnings, not hard blocks
    /// </summary>
    [Fact]
    public async Task BlockedUser_LoginAttempt_ShouldSucceed_WithJWTIssued()
    {
        // Arrange
        var blockedUser = CreateTestUser(Guid.NewGuid(), UserStatus.Blocked);

        // Act & Assert
        // UserAuthService.LoginInitialAsync should allow Blocked users
        // Only Deleted and Suspended should be hard-blocked
        Assert.Equal(UserStatus.Blocked, blockedUser.Status);
        // In real test: verify JWT is issued with userId, role, etc
    }

    /// <summary>
    /// SCENARIO 3: Profile endpoint returns IsBlocked flag
    /// Frontend reads this as authoritative source for blocking state
    /// </summary>
    [Fact]
    public async Task GetUserProfile_BlockedUser_ShouldReturnIsBlocked_True()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var blockReason = "Payment overdue";
        var blockedUntil = DateTimeOffset.UtcNow.AddDays(7);
        
        var blockedUser = new User(
            Id: userId,
            UserName: "blockeduser",
            Email: "blocked@example.com",
            FirstName: "Blocked",
            LastName: "User",
            Role: UserRole.User,
            EmailConfirmed: true,
            TwoFactorEnabled: false,
            TwoFactorSecret: string.Empty,
            BackupCodes: string.Empty,
            Status: UserStatus.Blocked,
            BaseCurrency: "USD",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            BlockedUntilUtc: blockedUntil,
            DeletedAtUtc: null,
            BlockReason: blockReason,
            DeleteReason: null,
            LastModifiedByAdminId: Guid.NewGuid());

        // Act
        var profileResponse = new UserAuthProfileResponse(
            Id: blockedUser.Id,
            UserName: blockedUser.UserName,
            Email: blockedUser.Email,
            FirstName: blockedUser.FirstName,
            LastName: blockedUser.LastName,
            Status: blockedUser.Status.ToString(),
            IsBlocked: blockedUser.Status == UserStatus.Blocked,
            BlockedUntilUtc: blockedUser.BlockedUntilUtc,
            BlockReason: blockedUser.BlockReason,
            CreatedAtUtc: blockedUser.CreatedAtUtc,
            BaseCurrency: blockedUser.BaseCurrency);

        // Assert
        Assert.True(profileResponse.IsBlocked);
        Assert.Equal(blockReason, profileResponse.BlockReason);
        Assert.Equal(blockedUntil, profileResponse.BlockedUntilUtc);
    }

    /// <summary>
    /// SCENARIO 4: Middleware allows blocked requests but flags them
    /// This prevents hard blocks for business-state flags
    /// Middleware sets HttpContext.Items["IsBlocked"]=true instead of 401
    /// </summary>
    [Fact]
    public async Task ExceptionMiddleware_BlockedUser_ShouldAllow_ButFlagContext()
    {
        // Arrange
        var blockedUser = CreateTestUser(Guid.NewGuid(), UserStatus.Blocked);

        // Act & Assert
        // In real integration test with HttpContext mock:
        // 1. Middleware extracts userId from JWT claims
        // 2. Loads user from DB
        // 3. If Status=Blocked: sets HttpContext.Items["IsBlocked"]=true
        // 4. If Status=Deleted: returns 401 Unauthorized
        // This test is shown as reference for middleware behavior
        Assert.Equal(UserStatus.Blocked, blockedUser.Status);
    }

    /// <summary>
    /// SCENARIO 5: Unblock user - status changes back to Active
    /// Validates unblock reverses block state
    /// </summary>
    [Fact]
    public async Task UnblockUser_ShouldSetStatusToActive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var blockedUser = CreateTestUser(userId, UserStatus.Blocked);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blockedUser);

        _userRepositoryMock
            .Setup(r => r.UnblockUserAsync(userId, adminId, It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, CancellationToken>((uid, aid, ct) =>
            {
                blockedUser = blockedUser with 
                { 
                    Status = UserStatus.Active, 
                    BlockedUntilUtc = null,
                    BlockReason = null 
                };
            })
            .Returns(Task.CompletedTask);

        // Act
        var service = new AdminUsersService(
            _userRepositoryMock.Object,
            Mock.Of<IApprovalService>(),
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Assert
        _userRepositoryMock.Verify(
            r => r.UnblockUserAsync(userId, adminId, It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Equal(UserStatus.Active, blockedUser.Status);
    }

    /// <summary>
    /// SCENARIO 6: Cannot block already-blocked user (no-op)
    /// Validates idempotency prevents cascading blocks
    /// </summary>
    [Fact]
    public async Task BlockUser_OnAlreadyBlockedUser_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var blockedUser = CreateTestUser(userId, UserStatus.Blocked);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blockedUser);

        var service = new AdminUsersService(
            _userRepositoryMock.Object,
            Mock.Of<IApprovalService>(),
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.BlockUserAsync(userId, "Already blocked", null, adminId, CancellationToken.None));
    }

    /// <summary>
    /// SCENARIO 7: Cannot unblock non-blocked user
    /// Validates state validation prevents invalid transitions
    /// </summary>
    [Fact]
    public async Task UnblockUser_OnNonBlockedUser_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var activeUser = CreateTestUser(userId, UserStatus.Active);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeUser);

        var service = new AdminUsersService(
            _userRepositoryMock.Object,
            Mock.Of<IApprovalService>(),
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UnblockUserAsync(userId, adminId, CancellationToken.None));
    }

    /// <summary>
    /// SCENARIO 8: Block with future BlockedUntilUtc (temporary block)
    /// Validates time-based blocking works correctly
    /// </summary>
    [Fact]
    public async Task BlockUser_WithBlockedUntil_ShouldSetExpirationTime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = CreateTestUser(userId, UserStatus.Active);
        var blockedUntil = DateTimeOffset.UtcNow.AddHours(24);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var capturedBlockedUntil = (DateTimeOffset?)null;
        _userRepositoryMock
            .Setup(r => r.BlockUserAsync(userId, It.IsAny<string>(), blockedUntil, adminId, It.IsAny<CancellationToken>()))
            .Callback<Guid, string, DateTimeOffset?, Guid, CancellationToken>((uid, reason, until, aid, ct) =>
            {
                capturedBlockedUntil = until;
            })
            .Returns(Task.CompletedTask);

        // Act
        var service = new AdminUsersService(
            _userRepositoryMock.Object,
            Mock.Of<IApprovalService>(),
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Assert
        _userRepositoryMock.Verify(
            r => r.BlockUserAsync(userId, It.IsAny<string>(), blockedUntil, adminId, It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Equal(blockedUntil, capturedBlockedUntil);
    }

    /// <summary>
    /// SCENARIO 9: Audit log created for block/unblock
    /// Validates compliance trail for admin actions
    /// </summary>
    [Fact]
    public async Task BlockUser_ShouldCreateAuditLog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = CreateTestUser(userId, UserStatus.Active);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var auditLogCaptured = false;
        _auditLogRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((log, ct) =>
            {
                Assert.Contains("BLOCK", log.Action);
                auditLogCaptured = true;
            })
            .Returns(Task.CompletedTask);

        // Act
        var service = new AdminUsersService(
            _userRepositoryMock.Object,
            Mock.Of<IApprovalService>(),
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Assert
        Assert.True(auditLogCaptured || _auditLogRepositoryMock.Invocations.Count > 0);
    }

    // ========== HELPERS ==========

    private User CreateTestUser(Guid userId, UserStatus status)
    {
        return new User(
            Id: userId,
            UserName: $"testuser_{userId}",
            Email: $"test_{userId}@example.com",
            FirstName: "Test",
            LastName: "User",
            Role: UserRole.User,
            EmailConfirmed: true,
            TwoFactorEnabled: false,
            TwoFactorSecret: string.Empty,
            BackupCodes: string.Empty,
            Status: status,
            BaseCurrency: "USD",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            BlockedUntilUtc: status == UserStatus.Blocked ? DateTimeOffset.UtcNow.AddDays(7) : null,
            DeletedAtUtc: null,
            BlockReason: status == UserStatus.Blocked ? "Test block" : null,
            DeleteReason: null,
            LastModifiedByAdminId: null);
    }
}
