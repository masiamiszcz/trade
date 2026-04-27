using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

/// <summary>
/// Service implementation for approval workflow management.
/// Single responsibility: handle approval, rejection, and workflow operations for admin requests.
/// Calls InstrumentService to execute approved actions.
/// Maintains comprehensive audit logging for compliance.
/// </summary>
public sealed class ApprovalService : IApprovalService
{
    private readonly IAdminRequestRepository _adminRequestRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAdminAuthRepository _adminAuthRepository;
    private readonly IUserApprovalHandler _userHandler;
    private readonly IInstrumentApprovalHandler _instrumentHandler;
    private readonly IMapper _mapper;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(
        IAdminRequestRepository adminRequestRepository,
        IAuditLogRepository auditLogRepository,
        IAdminAuthRepository adminAuthRepository,
        IUserApprovalHandler userHandler,
        IInstrumentApprovalHandler instrumentHandler,
        IMapper mapper,
        ILogger<ApprovalService> logger)
    {
        _adminRequestRepository = adminRequestRepository;
        _auditLogRepository = auditLogRepository;
        _adminAuthRepository = adminAuthRepository;
        _userHandler = userHandler ?? throw new ArgumentNullException(nameof(userHandler));
        _instrumentHandler = instrumentHandler ?? throw new ArgumentNullException(nameof(instrumentHandler));
        _mapper = mapper;
        _logger = logger;
    }

    // ===== RETRIEVAL OPERATIONS =====

    public async Task<IEnumerable<AdminRequestDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all admin requests");

        var requests = await _adminRequestRepository.GetPagedAsync(1, 1000, cancellationToken);
        var dtos = requests.Items.Select(_mapper.Map<AdminRequestDto>);

        return dtos;
    }

    public async Task<IEnumerable<AdminRequestDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving pending admin requests");

        var requests = await _adminRequestRepository.GetPendingAsync(cancellationToken);
        var dtos = requests.Select(_mapper.Map<AdminRequestDto>);

        return dtos;
    }

    public async Task<AdminRequestDto> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving admin request {RequestId}", requestId);

        var request = await _adminRequestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new InvalidOperationException($"Admin request with ID {requestId} not found");

        return _mapper.Map<AdminRequestDto>(request);
    }

    // ===== APPROVAL/REJECTION OPERATIONS =====

    public async Task<AdminRequestDto> ApproveAsync(
        Guid requestId,
        Guid approvedByAdminId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing approval for request {RequestId} by admin {AdminId}",
            requestId,
            approvedByAdminId);

        // Get the pending request
        var request = await _adminRequestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new InvalidOperationException($"Admin request with ID {requestId} not found");

        // Validate request is pending
        if (request.Status != AdminRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot approve a request with status '{request.Status}'");

        // Prevent self-approval with role-based exception
        // Rule: Regular admin cannot approve own request, SuperAdmin CAN approve own request
        if (request.RequestedByAdminId == approvedByAdminId)
        {
            var isSuperAdmin = await _adminAuthRepository.IsUserSuperAdminAsync(approvedByAdminId, cancellationToken);
            if (!isSuperAdmin)
            {
                throw new InvalidOperationException("An admin cannot approve their own request");
            }
        }

        // Dispatch execution to appropriate handler based on entity type
        try
        {
            if (request.EntityType == "Instrument")
            {
                await _instrumentHandler.ExecuteAsync(
                    request,
                    approvedByAdminId,
                    cancellationToken);
            }
            else if (request.EntityType == "User")
            {
                await _userHandler.ExecuteAsync(
                    request,
                    approvedByAdminId,
                    cancellationToken);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported entity type: {request.EntityType}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute action {Action} for request {RequestId}", request.Action, requestId);
            throw;
        }

        // Update the admin request status to Approved
        var approvedRequest = request with
        {
            Status = AdminRequestStatus.Approved,
            ApprovedByAdminId = approvedByAdminId,
            ApprovedAtUtc = DateTimeOffset.UtcNow
        };

        await _adminRequestRepository.UpdateAsync(approvedRequest, cancellationToken);

        // Log the approval with full details
        var auditLog = CreateAuditLogEntry(
            adminId: approvedByAdminId,
            action: $"APPROVE_{request.Action.ToString().ToUpper()}_REQUEST",
            entityType: request.EntityType,
            entityId: request.EntityId,
            details: new
            {
                entityId = request.EntityId,
                action = request.Action,
                originalReason = request.Reason,
                approvalTimestamp = DateTimeOffset.UtcNow
            });

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        // Handlers create their own entity-specific audit logs
        // ApprovalService logs only the approval action itself

        await _adminRequestRepository.SaveChangesAsync(cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Request {RequestId} approved successfully. Entity {EntityType} {EntityId} action '{Action}' executed",
            requestId,
            request.EntityType,
            request.EntityId ?? Guid.Empty,
            request.Action);

        return _mapper.Map<AdminRequestDto>(approvedRequest);
    }

    public async Task<AdminRequestDto> RejectAsync(
        Guid requestId,
        Guid rejectedByAdminId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing rejection for request {RequestId} by admin {AdminId}",
            requestId,
            rejectedByAdminId);

        // Get the pending request
        var request = await _adminRequestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new InvalidOperationException($"Admin request with ID {requestId} not found");

        // Validate request is pending
        if (request.Status != AdminRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot reject a request with status '{request.Status}'");

        // Update request status to rejected
        var rejectedRequest = request with
        {
            Status = AdminRequestStatus.Rejected,
            ApprovedByAdminId = rejectedByAdminId,
            ApprovedAtUtc = DateTimeOffset.UtcNow
        };

        await _adminRequestRepository.UpdateAsync(rejectedRequest, cancellationToken);

        // Log the rejection
        var auditLog = CreateAuditLogEntry(
            adminId: rejectedByAdminId,
            action: $"REJECT_{request.Action.ToString().ToUpper()}_REQUEST",
            entityType: "AdminRequest",
            entityId: requestId,
            details: new
            {
                entityId = request.EntityId,
                entityType = request.EntityType,
                action = request.Action,
                originalReason = request.Reason,
                rejectionReason = reason,
                rejectionTimestamp = DateTimeOffset.UtcNow
            });

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _adminRequestRepository.SaveChangesAsync(cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Request {RequestId} rejected successfully",
            requestId);

        return _mapper.Map<AdminRequestDto>(rejectedRequest);
    }

    public async Task AddCommentAsync(
        Guid requestId,
        Guid adminId,
        string commentText,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Adding comment to request {RequestId} by admin {AdminId}",
            requestId,
            adminId);

        // Get the request to verify it exists
        var request = await _adminRequestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new InvalidOperationException($"Admin request with ID {requestId} not found");

        // Validate comment is not empty
        if (string.IsNullOrWhiteSpace(commentText))
            throw new InvalidOperationException("Comment cannot be empty");

        // Log the comment addition
        var auditLog = CreateAuditLogEntry(
            adminId: adminId,
            action: "ADD_COMMENT",
            entityType: "AdminRequest",
            entityId: requestId,
            details: new
            {
                entityId = request.EntityId,
                entityType = request.EntityType,
                action = request.Action,
                comment = commentText,
                requestStatus = request.Status,
                commentTimestamp = DateTimeOffset.UtcNow
            });

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Comment added to request {RequestId}",
            requestId);
    }

    // ===== REQUEST CREATION =====

    public async Task<AdminRequestDto> CreateRequestAsync(
        string entityType,
        Guid? entityId,
        AdminRequestActionType action,
        Guid requestedByAdminId,
        string? reason = null,
        string? payloadJson = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating approval request for {EntityType} {EntityId}, action {Action} by admin {AdminId}",
            entityType,
            entityId ?? Guid.Empty,
            action,
            requestedByAdminId);

        // Check for existing pending request with same payload (idempotency)
        // For CREATE actions, entityId is null; for others it's the entity being modified
        var existingRequests = await _adminRequestRepository.GetByEntityAsync(entityType, entityId, cancellationToken);
        var existingRequest = existingRequests.FirstOrDefault(r => r.Status == AdminRequestStatus.Pending && r.Action == action);
        
        if (existingRequest is not null && payloadJson is not null)
        {
            // Compare payloads for idempotency
            if (payloadJson == existingRequest.PayloadJson)
            {
                _logger.LogInformation(
                    "Idempotent request detected for {EntityType} {EntityId}, action {Action}. Returning existing request {RequestId}",
                    entityType,
                    entityId ?? Guid.Empty,
                    action,
                    existingRequest.Id);
                
                return _mapper.Map<AdminRequestDto>(existingRequest);
            }
        }

        // NOTE: Entity validation is handled at the service level (InstrumentService, etc)
        // NOT here. This service only manages the request lifecycle, not entity state.
        // - For CREATE: entityId is null (entity doesn't exist yet)
        // - For UPDATE/DELETE/BLOCK/UNBLOCK: entityId is validated by the calling service

        // Create admin request
        var request = new AdminRequest(
            Id: Guid.NewGuid(),
            EntityType: entityType,
            EntityId: entityId,
            RequestedByAdminId: requestedByAdminId,
            ApprovedByAdminId: null,
            Action: action,
            Reason: reason?.Trim(),
            Status: AdminRequestStatus.Pending,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: null,
            PayloadJson: payloadJson);

        await _adminRequestRepository.AddAsync(request, cancellationToken);

        // Log the request creation
        var auditLog = CreateAuditLogEntry(
            adminId: requestedByAdminId,
            action: $"CREATE_{action.ToString().ToUpper()}_REQUEST",
            entityType: entityType,
            entityId: entityId,
            details: new
            {
                requestId = request.Id,
                reason,
                actionType = action
            });

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _adminRequestRepository.SaveChangesAsync(cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Approval request created successfully: {RequestId} for action {Action}",
            request.Id,
            action);

        return _mapper.Map<AdminRequestDto>(request);
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
