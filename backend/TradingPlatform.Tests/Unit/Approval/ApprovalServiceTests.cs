using Xunit;
using Moq;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Core.Services;
using AutoMapper;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TradingPlatform.Tests.Unit.Approval;

/// <summary>
/// Unit tests for ApprovalService orchestrator
/// Tests: Request loading → Handler dispatch → Request state update → Audit logging
/// 
/// ApprovalService is a PURE ORCHESTRATOR (70 lines):
/// 1. Load AdminRequest
/// 2. Validate pending status
/// 3. Check self-approval (Admin cannot self-approve unless SuperAdmin)
/// 4. Dispatch to appropriate handler (User vs Instrument)
/// 5. Update request.Status = Approved
/// 6. Create audit log
/// 7. Return result
/// </summary>
public class ApprovalServiceTests
{
    private readonly Mock<IAdminRequestRepository> _adminRequestRepositoryMock;
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<IAdminAuthRepository> _adminAuthRepositoryMock;
    private readonly Mock<IUserApprovalHandler> _userHandlerMock;
    private readonly Mock<IInstrumentApprovalHandler> _instrumentHandlerMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<ApprovalService>> _loggerMock;

    public ApprovalServiceTests()
    {
        _adminRequestRepositoryMock = new Mock<IAdminRequestRepository>();
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _adminAuthRepositoryMock = new Mock<IAdminAuthRepository>();
        _userHandlerMock = new Mock<IUserApprovalHandler>();
        _instrumentHandlerMock = new Mock<IInstrumentApprovalHandler>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<ApprovalService>>();
    }

    /// <summary>
    /// SCENARIO 1: Approve pending request → loads request, validates, dispatches to handler
    /// </summary>
    [Fact]
    public async Task ApproveAsync_ValidRequest_ShouldDispatchToHandler()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new AdminRequest(
            Id: requestId,
            EntityType: "User",
            EntityId: userId,
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: Guid.NewGuid(),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "Delete user",
            PayloadJson: null);

        _adminRequestRepositoryMock
            .Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var userDto = new UserDto(
            Id: userId,
            UserName: "deleteduser",
            Email: "deleted@example.com",
            FirstName: "Deleted",
            LastName: "User",
            Role: UserRole.User,
            Status: UserStatus.Deleted);

        _userHandlerMock
            .Setup(h => h.ExecuteAsync(request, approverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);

        var service = new ApprovalService(
            _adminRequestRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _adminAuthRepositoryMock.Object,
            _userHandlerMock.Object,
            _instrumentHandlerMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.ApproveAsync(requestId, approverId, CancellationToken.None);

        // Assert
        _userHandlerMock.Verify(
            h => h.ExecuteAsync(request, approverId, It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Equal(userId, result.Id);
    }

    /// <summary>
    /// SCENARIO 2: Request not found → throws
    /// </summary>
    [Fact]
    public async Task ApproveAsync_RequestNotFound_ShouldThrow()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        _adminRequestRepositoryMock
            .Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminRequest?)null);

        var service = new ApprovalService(
            _adminRequestRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _adminAuthRepositoryMock.Object,
            _userHandlerMock.Object,
            _instrumentHandlerMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveAsync(requestId, approverId, CancellationToken.None));
    }

    /// <summary>
    /// SCENARIO 3: Already approved request → throws
    /// Prevents re-approval of completed requests
    /// </summary>
    [Fact]
    public async Task ApproveAsync_AlreadyApproved_ShouldThrow()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        var approvedRequest = new AdminRequest(
            Id: requestId,
            EntityType: "User",
            EntityId: Guid.NewGuid(),
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Approved,  // Already approved
            RequestedByAdminId: Guid.NewGuid(),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: approverId,
            ApprovedAtUtc: DateTimeOffset.UtcNow,
            Reason: "Delete user",
            PayloadJson: null);

        _adminRequestRepositoryMock
            .Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedRequest);

        var service = new ApprovalService(
            _adminRequestRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _adminAuthRepositoryMock.Object,
            _userHandlerMock.Object,
            _instrumentHandlerMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveAsync(requestId, approverId, CancellationToken.None));
    }

    /// <summary>
    /// SCENARIO 4: Admin approves own request → throws (unless SuperAdmin)
    /// Business rule: Admin cannot self-approve, SuperAdmin can
    /// </summary>
    [Fact]
    public async Task ApproveAsync_AdminSelfApprove_ShouldThrow()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var request = new AdminRequest(
            Id: requestId,
            EntityType: "User",
            EntityId: Guid.NewGuid(),
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: adminId,  // Admin requested it
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "Delete user",
            PayloadJson: null);

        var admin = new Admin(
            Id: adminId,
            Name: "Test Admin",
            Email: "admin@example.com",
            Role: AdminRole.Admin,  // Not SuperAdmin
            CreatedAtUtc: DateTimeOffset.UtcNow,
            LastLoginAtUtc: null);

        _adminRequestRepositoryMock
            .Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _adminAuthRepositoryMock
            .Setup(r => r.GetByIdAsync(adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var service = new ApprovalService(
            _adminRequestRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _adminAuthRepositoryMock.Object,
            _userHandlerMock.Object,
            _instrumentHandlerMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveAsync(requestId, adminId, CancellationToken.None));
    }

    /// <summary>
    /// SCENARIO 5: SuperAdmin can self-approve
    /// Business rule: SuperAdmin has unrestricted approval power
    /// </summary>
    [Fact]
    public async Task ApproveAsync_SuperAdminSelfApprove_ShouldSucceed()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var superAdminId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new AdminRequest(
            Id: requestId,
            EntityType: "User",
            EntityId: userId,
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: superAdminId,  // SuperAdmin requested it
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "Delete user",
            PayloadJson: null);

        var superAdmin = new Admin(
            Id: superAdminId,
            Name: "Super Admin",
            Email: "superadmin@example.com",
            Role: AdminRole.SuperAdmin,  // SuperAdmin role
            CreatedAtUtc: DateTimeOffset.UtcNow,
            LastLoginAtUtc: null);

        var userDto = new UserDto(
            Id: userId,
            UserName: "deleteduser",
            Email: "deleted@example.com",
            FirstName: "Deleted",
            LastName: "User",
            Role: UserRole.User,
            Status: UserStatus.Deleted);

        _adminRequestRepositoryMock
            .Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _adminAuthRepositoryMock
            .Setup(r => r.GetByIdAsync(superAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(superAdmin);

        _userHandlerMock
            .Setup(h => h.ExecuteAsync(request, superAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);

        var service = new ApprovalService(
            _adminRequestRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _adminAuthRepositoryMock.Object,
            _userHandlerMock.Object,
            _instrumentHandlerMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.ApproveAsync(requestId, superAdminId, CancellationToken.None);

        // Assert
        _userHandlerMock.Verify(
            h => h.ExecuteAsync(request, superAdminId, It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Equal(userId, result.Id);
    }

    /// <summary>
    /// SCENARIO 6: Instrument request → dispatches to InstrumentHandler
    /// </summary>
    [Fact]
    public async Task ApproveAsync_InstrumentRequest_ShouldDispatchToInstrumentHandler()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var instrumentId = Guid.NewGuid();

        var request = new AdminRequest(
            Id: requestId,
            EntityType: "Instrument",
            EntityId: instrumentId,
            Action: AdminRequestActionType.Create,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: Guid.NewGuid(),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "Create instrument",
            PayloadJson: null);

        var instrumentDto = new InstrumentDto(
            Id: instrumentId,
            Symbol: "AAPL",
            Name: "Apple Inc.",
            Status: InstrumentStatus.Active);

        _adminRequestRepositoryMock
            .Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _instrumentHandlerMock
            .Setup(h => h.ExecuteAsync(request, approverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrumentDto);

        var service = new ApprovalService(
            _adminRequestRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _adminAuthRepositoryMock.Object,
            _userHandlerMock.Object,
            _instrumentHandlerMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.ApproveAsync(requestId, approverId, CancellationToken.None);

        // Assert
        _instrumentHandlerMock.Verify(
            h => h.ExecuteAsync(request, approverId, It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Equal(instrumentId, result.Id);
    }

    /// <summary>
    /// SCENARIO 7: Unknown entity type → throws
    /// Prevents dispatch to non-existent handlers
    /// </summary>
    [Fact]
    public async Task ApproveAsync_UnknownEntityType_ShouldThrow()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        var request = new AdminRequest(
            Id: requestId,
            EntityType: "UnknownEntity",  // Not "User" or "Instrument"
            EntityId: Guid.NewGuid(),
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: Guid.NewGuid(),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "Delete unknown",
            PayloadJson: null);

        _adminRequestRepositoryMock
            .Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var service = new ApprovalService(
            _adminRequestRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _adminAuthRepositoryMock.Object,
            _userHandlerMock.Object,
            _instrumentHandlerMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            () => service.ApproveAsync(requestId, approverId, CancellationToken.None));
    }

    /// <summary>
    /// SCENARIO 8: Audit log created after approval
    /// Validates compliance trail for all approvals
    /// </summary>
    [Fact]
    public async Task ApproveAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new AdminRequest(
            Id: requestId,
            EntityType: "User",
            EntityId: userId,
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: Guid.NewGuid(),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "Delete user",
            PayloadJson: null);

        var userDto = new UserDto(
            Id: userId,
            UserName: "deleteduser",
            Email: "deleted@example.com",
            FirstName: "Deleted",
            LastName: "User",
            Role: UserRole.User,
            Status: UserStatus.Deleted);

        _adminRequestRepositoryMock
            .Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var auditLogCaptured = false;
        _auditLogRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((log, ct) =>
            {
                Assert.NotNull(log);
                auditLogCaptured = true;
            })
            .Returns(Task.CompletedTask);

        _userHandlerMock
            .Setup(h => h.ExecuteAsync(request, approverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);

        var service = new ApprovalService(
            _adminRequestRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _adminAuthRepositoryMock.Object,
            _userHandlerMock.Object,
            _instrumentHandlerMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        // Act
        await service.ApproveAsync(requestId, approverId, CancellationToken.None);

        // Assert
        Assert.True(auditLogCaptured);
    }

    /// <summary>
    /// SCENARIO 9: Request status updated to Approved after execution
    /// </summary>
    [Fact]
    public async Task ApproveAsync_ShouldUpdateRequestStatus()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new AdminRequest(
            Id: requestId,
            EntityType: "User",
            EntityId: userId,
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: Guid.NewGuid(),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "Delete user",
            PayloadJson: null);

        var userDto = new UserDto(
            Id: userId,
            UserName: "deleteduser",
            Email: "deleted@example.com",
            FirstName: "Deleted",
            LastName: "User",
            Role: UserRole.User,
            Status: UserStatus.Deleted);

        _adminRequestRepositoryMock
            .Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _adminRequestRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<AdminRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AdminRequest, CancellationToken>((req, ct) =>
            {
                Assert.Equal(AdminRequestStatus.Approved, req.Status);
                Assert.Equal(approverId, req.ApprovedByAdminId);
                Assert.NotNull(req.ApprovedAtUtc);
            })
            .Returns(Task.CompletedTask);

        _userHandlerMock
            .Setup(h => h.ExecuteAsync(request, approverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);

        var service = new ApprovalService(
            _adminRequestRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _adminAuthRepositoryMock.Object,
            _userHandlerMock.Object,
            _instrumentHandlerMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        // Act
        await service.ApproveAsync(requestId, approverId, CancellationToken.None);

        // Assert
        _adminRequestRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<AdminRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
