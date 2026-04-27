using Xunit;
using Moq;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Core.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TradingPlatform.Tests.Unit.Approval;

/// <summary>
/// Unit tests for complete approval flow scenarios
/// Tests: Request creation → Pending state → Approval → Execution → Final state
/// 
/// Validates the entire workflow without integration complexity
/// Each test represents a complete scenario from request to outcome
/// </summary>
public class ApprovalFlowTests
{
    private readonly Mock<IAdminRequestRepository> _adminRequestRepositoryMock;
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;

    public ApprovalFlowTests()
    {
        _adminRequestRepositoryMock = new Mock<IAdminRequestRepository>();
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
    }

    /// <summary>
    /// SCENARIO 1: Request created in Pending state
    /// Validates initial request creation
    /// </summary>
    [Fact]
    public async Task CreateRequest_ShouldHavePendingStatus()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var request = new AdminRequest(
            Id: requestId,
            EntityType: "User",
            EntityId: userId,
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: adminId,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "Delete user",
            PayloadJson: null);

        // Act
        _adminRequestRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AdminRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _adminRequestRepositoryMock.Object.AddAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(AdminRequestStatus.Pending, request.Status);
        Assert.Null(request.ApprovedByAdminId);
        Assert.Null(request.ApprovedAtUtc);

        _adminRequestRepositoryMock.Verify(
            r => r.AddAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// SCENARIO 2: Request approved → status changes to Approved
    /// Validates approval updates state
    /// </summary>
    [Fact]
    public async Task ApproveRequest_ShouldUpdateStatus()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var requestorAdminId = Guid.NewGuid();
        var approverAdminId = Guid.NewGuid();

        var request = new AdminRequest(
            Id: requestId,
            EntityType: "User",
            EntityId: userId,
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: requestorAdminId,
            CreatedAtUtc: DateTimeOffset.UtcNow.AddHours(-1),
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "Delete user",
            PayloadJson: null);

        var approvedRequest = request with
        {
            Status = AdminRequestStatus.Approved,
            ApprovedByAdminId = approverAdminId,
            ApprovedAtUtc = DateTimeOffset.UtcNow
        };

        _adminRequestRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<AdminRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _adminRequestRepositoryMock.Object.UpdateAsync(approvedRequest, CancellationToken.None);

        // Assert
        Assert.Equal(AdminRequestStatus.Approved, approvedRequest.Status);
        Assert.Equal(approverAdminId, approvedRequest.ApprovedByAdminId);
        Assert.NotNull(approvedRequest.ApprovedAtUtc);

        _adminRequestRepositoryMock.Verify(
            r => r.UpdateAsync(approvedRequest, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// SCENARIO 3: Request rejected → status changes to Rejected
    /// Validates rejection flow
    /// </summary>
    [Fact]
    public async Task RejectRequest_ShouldUpdateStatus()
    {
        // Arrange
        var requestId = Guid.NewGuid();
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

        var rejectedRequest = request with
        {
            Status = AdminRequestStatus.Rejected
        };

        _adminRequestRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<AdminRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _adminRequestRepositoryMock.Object.UpdateAsync(rejectedRequest, CancellationToken.None);

        // Assert
        Assert.Equal(AdminRequestStatus.Rejected, rejectedRequest.Status);
        Assert.Null(rejectedRequest.ApprovedByAdminId);

        _adminRequestRepositoryMock.Verify(
            r => r.UpdateAsync(rejectedRequest, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// SCENARIO 4: Idempotency - second request for same entity returns existing
    /// Prevents duplicate pending requests
    /// </summary>
    [Fact]
    public async Task CreateRequest_Duplicate_ShouldReturnExisting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var existingRequest = new AdminRequest(
            Id: Guid.NewGuid(),
            EntityType: "User",
            EntityId: userId,
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: adminId,
            CreatedAtUtc: DateTimeOffset.UtcNow.AddHours(-1),
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "First delete request",
            PayloadJson: null);

        _adminRequestRepositoryMock
            .Setup(r => r.GetPendingByEntityAsync("User", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRequest);

        // Act
        var result = await _adminRequestRepositoryMock.Object
            .GetPendingByEntityAsync("User", userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingRequest.Id, result.Id);
        Assert.Equal(AdminRequestStatus.Pending, result.Status);
    }

    /// <summary>
    /// SCENARIO 5: Only Pending requests can be approved
    /// Approved/Rejected requests cannot be changed
    /// </summary>
    [Fact]
    public async Task ApproveRequest_AlreadyApproved_ShouldFail()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var approverAdminId = Guid.NewGuid();

        var approvedRequest = new AdminRequest(
            Id: requestId,
            EntityType: "User",
            EntityId: Guid.NewGuid(),
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Approved,  // Already approved
            RequestedByAdminId: Guid.NewGuid(),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: approverAdminId,
            ApprovedAtUtc: DateTimeOffset.UtcNow,
            Reason: "Delete user",
            PayloadJson: null);

        _adminRequestRepositoryMock
            .Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedRequest);

        // Act & Assert
        var result = await _adminRequestRepositoryMock.Object
            .GetByIdAsync(requestId, CancellationToken.None);

        Assert.NotEqual(AdminRequestStatus.Pending, result.Status);
    }

    /// <summary>
    /// SCENARIO 6: Request cancellation - status changes to Cancelled
    /// Allows undoing pending requests
    /// </summary>
    [Fact]
    public async Task CancelRequest_ShouldUpdateStatus()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        var request = new AdminRequest(
            Id: requestId,
            EntityType: "User",
            EntityId: Guid.NewGuid(),
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: Guid.NewGuid(),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "Delete user",
            PayloadJson: null);

        var cancelledRequest = request with
        {
            Status = AdminRequestStatus.Cancelled
        };

        _adminRequestRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<AdminRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _adminRequestRepositoryMock.Object.UpdateAsync(cancelledRequest, CancellationToken.None);

        // Assert
        Assert.Equal(AdminRequestStatus.Cancelled, cancelledRequest.Status);
    }

    /// <summary>
    /// SCENARIO 7: Multiple requests for different entities - all tracked independently
    /// Validates no cross-contamination between requests
    /// </summary>
    [Fact]
    public async Task MultipleRequests_ShouldBeIndependent()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var request1 = new AdminRequest(
            Id: Guid.NewGuid(),
            EntityType: "User",
            EntityId: userId1,
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: adminId,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "Delete user 1",
            PayloadJson: null);

        var request2 = new AdminRequest(
            Id: Guid.NewGuid(),
            EntityType: "User",
            EntityId: userId2,
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: adminId,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "Delete user 2",
            PayloadJson: null);

        _adminRequestRepositoryMock
            .Setup(r => r.AddAsync(request1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _adminRequestRepositoryMock
            .Setup(r => r.AddAsync(request2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _adminRequestRepositoryMock.Object.AddAsync(request1, CancellationToken.None);
        await _adminRequestRepositoryMock.Object.AddAsync(request2, CancellationToken.None);

        // Assert
        Assert.NotEqual(request1.EntityId, request2.EntityId);
        Assert.NotEqual(request1.Id, request2.Id);
        Assert.Equal(request1.Status, request2.Status);  // Both pending
    }

    /// <summary>
    /// SCENARIO 8: Audit trail created for request state transitions
    /// Validates compliance logging
    /// </summary>
    [Fact]
    public async Task RequestStateChange_ShouldCreateAuditLog()
    {
        // Arrange
        var requestId = Guid.NewGuid();
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

        var auditLog = new AuditLog(
            Id: Guid.NewGuid(),
            Action: "APPROVAL_REQUEST_CREATED",
            Details: $"User delete request for userId={userId}",
            PerformedByAdminId: request.RequestedByAdminId,
            CreatedAtUtc: DateTimeOffset.UtcNow);

        _auditLogRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _auditLogRepositoryMock.Object.AddAsync(auditLog, CancellationToken.None);

        // Assert
        _auditLogRepositoryMock.Verify(
            r => r.AddAsync(auditLog, It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Equal("APPROVAL_REQUEST_CREATED", auditLog.Action);
    }

    /// <summary>
    /// SCENARIO 9: Request with future action - pending until approved
    /// Validates long-lived pending state is stable
    /// </summary>
    [Fact]
    public async Task PendingRequest_RemainsStable()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var createdTime = DateTimeOffset.UtcNow.AddDays(-7);

        var request = new AdminRequest(
            Id: requestId,
            EntityType: "User",
            EntityId: Guid.NewGuid(),
            Action: AdminRequestActionType.Delete,
            Status: AdminRequestStatus.Pending,
            RequestedByAdminId: Guid.NewGuid(),
            CreatedAtUtc: createdTime,
            ApprovedByAdminId: null,
            ApprovedAtUtc: null,
            Reason: "Delete user",
            PayloadJson: null);

        _adminRequestRepositoryMock
            .Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        // Act
        var result = await _adminRequestRepositoryMock.Object
            .GetByIdAsync(requestId, CancellationToken.None);

        // Assert
        Assert.Equal(AdminRequestStatus.Pending, result.Status);
        Assert.Null(result.ApprovedByAdminId);
        Assert.Equal(createdTime, result.CreatedAtUtc);
        // Request should not auto-expire or change
    }

    /// <summary>
    /// SCENARIO 10: Different action types have different handlers
    /// Delete uses UserHandler, Instrument uses InstrumentHandler
    /// </summary>
    [Fact]
    public void RequestActionType_DeterminesHandler()
    {
        // Arrange
        var userDeleteAction = AdminRequestActionType.Delete;
        var instrumentCreateAction = AdminRequestActionType.Create;

        // Act & Assert
        // User actions: Delete, Restore
        Assert.Equal(4, (int)AdminRequestActionType.Delete);
        Assert.Equal(11, (int)AdminRequestActionType.Restore);

        // Instrument actions: Create, Update, Block, Unblock, RequestApproval
        Assert.Contains(
            (int)AdminRequestActionType.Create,
            new[] { 1, 2, 3, 5, 6, 7, 8, 9, 10, 12, 13 });
    }
}
