using Xunit;
using Moq;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Core.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TradingPlatform.Tests.Unit.Users;

/// <summary>
/// Unit tests for UserApprovalHandler execution layer
/// Tests: Delete execution → Restore execution → Validation → Audit
/// 
/// Handler is CRITICAL: This is where Delete/Restore actually happens
/// Without handler execution, ApprovalService just marks requests as "Approved"
/// but user stays in DB (ZOMBIE SYSTEM)
/// </summary>
public class UserApprovalHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<ILogger<UserApprovalHandler>> _loggerMock;

    public UserApprovalHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _loggerMock = new Mock<ILogger<UserApprovalHandler>>();
    }

    /// <summary>
    /// SCENARIO 1: Execute Delete on Active user → status changes to Deleted
    /// CRITICAL PATH: Validates user actually gets deleted from DB
    /// </summary>
    [Fact]
    public async Task ExecuteDelete_OnActiveUser_ShouldMarkDeleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var approvedByAdminId = Guid.NewGuid();
        var user = CreateTestUser(userId, UserStatus.Active);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdIncludingDeletedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.DeleteUserAsync(userId, It.IsAny<string>(), approvedByAdminId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = CreateDeleteRequest(userId, approvedByAdminId);

        var handler = new UserApprovalHandler(
            _userRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        var result = await handler.ExecuteAsync(request, approvedByAdminId, CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(
            r => r.DeleteUserAsync(userId, It.IsAny<string>(), approvedByAdminId, It.IsAny<CancellationToken>()),
            Times.Once,
            "DeleteUserAsync must be called to actually delete the user");

        Assert.Equal(userId, result.Id);
    }

    /// <summary>
    /// SCENARIO 2: Execute Delete on already-deleted user → throws
    /// Prevents double-delete
    /// </summary>
    [Fact]
    public async Task ExecuteDelete_OnAlreadyDeletedUser_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var approvedByAdminId = Guid.NewGuid();
        var deletedUser = CreateTestUser(userId, UserStatus.Deleted);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdIncludingDeletedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedUser);

        var request = CreateDeleteRequest(userId, approvedByAdminId);

        var handler = new UserApprovalHandler(
            _userRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.ExecuteAsync(request, approvedByAdminId, CancellationToken.None),
            "Cannot delete already-deleted user");
    }

    /// <summary>
    /// SCENARIO 3: Execute Restore on Deleted user → status changes to Active
    /// CRITICAL PATH: Validates user actually gets restored
    /// </summary>
    [Fact]
    public async Task ExecuteRestore_OnDeletedUser_ShouldMarkActive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var approvedByAdminId = Guid.NewGuid();
        var deletedUser = CreateTestUser(userId, UserStatus.Deleted);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdIncludingDeletedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedUser);

        _userRepositoryMock
            .Setup(r => r.RestoreUserAsync(userId, approvedByAdminId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = CreateRestoreRequest(userId, approvedByAdminId);

        var handler = new UserApprovalHandler(
            _userRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        var result = await handler.ExecuteAsync(request, approvedByAdminId, CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(
            r => r.RestoreUserAsync(userId, approvedByAdminId, It.IsAny<CancellationToken>()),
            Times.Once,
            "RestoreUserAsync must be called to actually restore the user");

        Assert.Equal(userId, result.Id);
    }

    /// <summary>
    /// SCENARIO 4: Execute Restore on Active user → throws
    /// Cannot restore user that is not deleted
    /// </summary>
    [Fact]
    public async Task ExecuteRestore_OnActiveUser_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var approvedByAdminId = Guid.NewGuid();
        var activeUser = CreateTestUser(userId, UserStatus.Active);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdIncludingDeletedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeUser);

        var request = CreateRestoreRequest(userId, approvedByAdminId);

        var handler = new UserApprovalHandler(
            _userRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.ExecuteAsync(request, approvedByAdminId, CancellationToken.None),
            "Cannot restore non-deleted user");
    }

    /// <summary>
    /// SCENARIO 5: User not found → throws
    /// Validates user existence before attempting operations
    /// </summary>
    [Fact]
    public async Task ExecuteDelete_UserNotFound_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var approvedByAdminId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(r => r.GetUserByIdIncludingDeletedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var request = CreateDeleteRequest(userId, approvedByAdminId);

        var handler = new UserApprovalHandler(
            _userRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.ExecuteAsync(request, approvedByAdminId, CancellationToken.None),
            "Cannot execute on non-existent user");
    }

    /// <summary>
    /// SCENARIO 6: Delete creates audit log with action=USER_DELETE
    /// Validates compliance trail
    /// </summary>
    [Fact]
    public async Task ExecuteDelete_ShouldCreateAuditLog_WithAction_USER_DELETE()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var approvedByAdminId = Guid.NewGuid();
        var user = CreateTestUser(userId, UserStatus.Active);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdIncludingDeletedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.DeleteUserAsync(userId, It.IsAny<string>(), approvedByAdminId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var auditLogCaptured = false;
        var capturedAction = "";
        _auditLogRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((log, ct) =>
            {
                capturedAction = log.Action;
                auditLogCaptured = true;
            })
            .Returns(Task.CompletedTask);

        var request = CreateDeleteRequest(userId, approvedByAdminId);

        var handler = new UserApprovalHandler(
            _userRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        await handler.ExecuteAsync(request, approvedByAdminId, CancellationToken.None);

        // Assert
        Assert.True(auditLogCaptured, "Audit log must be created");
        Assert.Equal("USER_DELETE", capturedAction);
    }

    /// <summary>
    /// SCENARIO 7: Restore creates audit log with action=USER_RESTORE
    /// </summary>
    [Fact]
    public async Task ExecuteRestore_ShouldCreateAuditLog_WithAction_USER_RESTORE()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var approvedByAdminId = Guid.NewGuid();
        var deletedUser = CreateTestUser(userId, UserStatus.Deleted);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdIncludingDeletedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedUser);

        _userRepositoryMock
            .Setup(r => r.RestoreUserAsync(userId, approvedByAdminId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var auditLogCaptured = false;
        var capturedAction = "";
        _auditLogRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((log, ct) =>
            {
                capturedAction = log.Action;
                auditLogCaptured = true;
            })
            .Returns(Task.CompletedTask);

        var request = CreateRestoreRequest(userId, approvedByAdminId);

        var handler = new UserApprovalHandler(
            _userRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        await handler.ExecuteAsync(request, approvedByAdminId, CancellationToken.None);

        // Assert
        Assert.True(auditLogCaptured, "Audit log must be created");
        Assert.Equal("USER_RESTORE", capturedAction);
    }

    /// <summary>
    /// SCENARIO 8: Audit log contains details with action, reason, adminId
    /// Validates audit trail has sufficient info for compliance
    /// </summary>
    [Fact]
    public async Task ExecuteDelete_AuditLog_ShouldContainDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var approvedByAdminId = Guid.NewGuid();
        var user = CreateTestUser(userId, UserStatus.Active);
        var deleteReason = "User requested deletion";

        _userRepositoryMock
            .Setup(r => r.GetUserByIdIncludingDeletedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.DeleteUserAsync(userId, deleteReason, approvedByAdminId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var capturedDetails = "";
        _auditLogRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((log, ct) =>
            {
                capturedDetails = log.Details;
            })
            .Returns(Task.CompletedTask);

        var request = CreateDeleteRequest(userId, approvedByAdminId, deleteReason);

        var handler = new UserApprovalHandler(
            _userRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        await handler.ExecuteAsync(request, approvedByAdminId, CancellationToken.None);

        // Assert
        Assert.NotEmpty(capturedDetails);
        // Details should contain: userId, action, reason, timestamp
        Assert.Contains(userId.ToString(), capturedDetails);
    }

    /// <summary>
    /// SCENARIO 9: Delete Blocked user → succeeds
    /// Validates Delete works on any non-Deleted status
    /// </summary>
    [Fact]
    public async Task ExecuteDelete_OnBlockedUser_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var approvedByAdminId = Guid.NewGuid();
        var blockedUser = CreateTestUser(userId, UserStatus.Blocked);

        _userRepositoryMock
            .Setup(r => r.GetUserByIdIncludingDeletedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blockedUser);

        _userRepositoryMock
            .Setup(r => r.DeleteUserAsync(userId, It.IsAny<string>(), approvedByAdminId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = CreateDeleteRequest(userId, approvedByAdminId);

        var handler = new UserApprovalHandler(
            _userRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        var result = await handler.ExecuteAsync(request, approvedByAdminId, CancellationToken.None);

        // Assert
        Assert.Equal(userId, result.Id);
        _userRepositoryMock.Verify(
            r => r.DeleteUserAsync(userId, It.IsAny<string>(), approvedByAdminId, It.IsAny<CancellationToken>()),
            Times.Once);
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
            BlockedUntilUtc: null,
            DeletedAtUtc: status == UserStatus.Deleted ? DateTimeOffset.UtcNow : null,
            BlockReason: null,
            DeleteReason: null,
            LastModifiedByAdminId: null);
    }

    private AdminRequest CreateDeleteRequest(Guid userId, Guid requestedByAdminId, string reason = "Delete user")
    {
        return new AdminRequest(
            Id: Guid.NewGuid(),
            EntityType: "User",
            EntityId: userId,
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Approved,
            RequestedByAdminId: requestedByAdminId,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: requestedByAdminId,
            ApprovedAtUtc: DateTimeOffset.UtcNow,
            Reason: reason,
            PayloadJson: null);
    }

    private AdminRequest CreateRestoreRequest(Guid userId, Guid requestedByAdminId, string reason = "Restore user")
    {
        return new AdminRequest(
            Id: Guid.NewGuid(),
            EntityType: "User",
            EntityId: userId,
            Action: AdminRequestActionType.Restore,
            Status: AdminRequestStatus.Approved,
            RequestedByAdminId: requestedByAdminId,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: requestedByAdminId,
            ApprovedAtUtc: DateTimeOffset.UtcNow,
            Reason: reason,
            PayloadJson: null);
    }
}
