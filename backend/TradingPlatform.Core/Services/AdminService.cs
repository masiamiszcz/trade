
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;


/// <summary>
/// Service implementation for administrative operations
/// Handles two-step approval workflow and comprehensive audit logging
/// Each approval logs the complete action trail for compliance
/// </summary>
public sealed class AdminService : IAdminService
{
    private readonly IAdminRequestRepository _adminRequestRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAdminAuditLogRepository _adminAuditLogRepository;
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IAdminRequestRepository adminRequestRepository,
        IAuditLogRepository auditLogRepository,
        IAdminAuditLogRepository adminAuditLogRepository,
        IInstrumentRepository instrumentRepository,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<AdminService> logger)
    {
        _adminRequestRepository = adminRequestRepository;
        _auditLogRepository = auditLogRepository;
        _adminAuditLogRepository = adminAuditLogRepository;
        _instrumentRepository = instrumentRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    // ======== ADMIN REQUEST OPERATIONS ========

    public async Task<IEnumerable<AdminRequestDto>> GetAllRequestsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all admin requests");

        var requests = await _adminRequestRepository.GetPagedAsync(1, 1000, cancellationToken);
        var dtos = requests.Items.Select(_mapper.Map<AdminRequestDto>);

        return dtos;
    }

    public async Task<IEnumerable<AdminRequestDto>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving pending admin requests");

        var requests = await _adminRequestRepository.GetPendingAsync(cancellationToken);
        var dtos = requests.Select(_mapper.Map<AdminRequestDto>);

        return dtos;
    }

    public async Task<AdminRequestDto> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving admin request {RequestId}", requestId);

        var request = await _adminRequestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new InvalidOperationException($"Admin request with ID {requestId} not found");

        return _mapper.Map<AdminRequestDto>(request);
    }

    // ======== INSTRUMENT MANAGEMENT ========

    public async Task<IEnumerable<InstrumentDto>> GetAllInstrumentsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all instruments for admin management");

        var instruments = await _instrumentRepository.GetAllAsync(cancellationToken);
        var dtos = instruments.Select(_mapper.Map<InstrumentDto>);

        return dtos;
    }

    // ======== USER MANAGEMENT ========

    public async Task<IEnumerable<UserListItemDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all users for admin dashboard");

        var users = await _userRepository.GetAllUsersAsync(cancellationToken);
        var dtos = users.Select(u => new UserListItemDto(
            u.Id,
            u.UserName,
            u.Email,
            u.FirstName,
            u.LastName,
            u.Role.ToString(),
            u.Status.ToString(),
            u.CreatedAtUtc
        )).ToList();

        return dtos;
    }

    // ======== REQUEST CREATION ========

    public async Task<AdminRequestDto> CreateBlockRequestAsync(
        Guid instrumentId,
        string reason,
        Guid requestedByAdminId,
        string adminIpAddress,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating block request for instrument {InstrumentId} by admin {AdminId}",
            instrumentId,
            requestedByAdminId);

        // Validate instrument exists
        var instrument = await _instrumentRepository.GetByIdAsync(instrumentId, cancellationToken)
            ?? throw new InvalidOperationException($"Instrument with ID {instrumentId} not found");

        // Validate reason is not empty
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Block request reason cannot be empty");

        // Create admin request
        var request = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: instrumentId,
            RequestedByAdminId: requestedByAdminId,
            ApprovedByAdminId: null,
            Action: AdminRequestActionType.Block,
            Reason: reason.Trim(),
            Status: AdminRequestStatus.Pending,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: null);

        await _adminRequestRepository.AddAsync(request, cancellationToken);

        // Log the request creation
        var auditLog = CreateAuditLogEntry(
            adminId: requestedByAdminId,
            action: "CREATE_BLOCK_REQUEST",
            entityType: "Instrument",
            entityId: instrumentId,
            details: new { requestId = request.Id, reason },
            ipAddress: adminIpAddress);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _adminRequestRepository.SaveChangesAsync(cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Block request created successfully: {RequestId}",
            request.Id);

        return _mapper.Map<AdminRequestDto>(request);
    }

    public async Task<AdminRequestDto> CreateUnblockRequestAsync(
        Guid instrumentId,
        string reason,
        Guid requestedByAdminId,
        string adminIpAddress,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating unblock request for instrument {InstrumentId} by admin {AdminId}",
            instrumentId,
            requestedByAdminId);

        // Validate instrument exists
        var instrument = await _instrumentRepository.GetByIdAsync(instrumentId, cancellationToken)
            ?? throw new InvalidOperationException($"Instrument with ID {instrumentId} not found");

        // Validate instrument is actually blocked
        if (!instrument.IsBlocked)
            throw new InvalidOperationException("Cannot create unblock request for an instrument that is not blocked");

        // Validate reason is not empty
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Unblock request reason cannot be empty");

        // Create admin request
        var request = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: instrumentId,
            RequestedByAdminId: requestedByAdminId,
            ApprovedByAdminId: null,
            Action: AdminRequestActionType.Unblock,
            Reason: reason.Trim(),
            Status: AdminRequestStatus.Pending,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: null);

        await _adminRequestRepository.AddAsync(request, cancellationToken);

        // Log the request creation
        var auditLog = CreateAuditLogEntry(
            adminId: requestedByAdminId,
            action: "CREATE_UNBLOCK_REQUEST",
            entityType: "Instrument",
            entityId: instrumentId,
            details: new { requestId = request.Id, reason },
            ipAddress: adminIpAddress);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _adminRequestRepository.SaveChangesAsync(cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Unblock request created successfully: {RequestId}",
            request.Id);

        return _mapper.Map<AdminRequestDto>(request);
    }

    // ======== APPROVAL/REJECTION OPERATIONS ========

    public async Task<AdminRequestDto> ApproveRequestAsync(
        Guid requestId,
        Guid approvedByAdminId,
        string adminIpAddress,
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

        // Prevent self-approval
        if (request.RequestedByAdminId == approvedByAdminId)
            throw new InvalidOperationException("An admin cannot approve their own request");

        // Get the instrument
        var instrument = await _instrumentRepository.GetByIdAsync(request.InstrumentId, cancellationToken)
            ?? throw new InvalidOperationException($"Instrument with ID {request.InstrumentId} not found");

        // Execute the requested action
        var updatedInstrument = request.Action switch
        {
            AdminRequestActionType.Block => instrument with { IsBlocked = true },
            AdminRequestActionType.Unblock => instrument with { IsBlocked = false },
            _ => throw new InvalidOperationException($"Unknown action type: {request.Action}")
        };

        await _instrumentRepository.UpdateAsync(updatedInstrument, cancellationToken);

        // Update the admin request status
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
            entityType: "AdminRequest",
            entityId: requestId,
            details: new
            {
                instrumentId = request.InstrumentId,
                instrumentSymbol = instrument.Symbol,
                action = request.Action,
                originalReason = request.Reason,
                approvalTimestamp = DateTimeOffset.UtcNow
            },
            ipAddress: adminIpAddress);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        // Also log the instrument change
        var instrumentAuditLog = CreateAuditLogEntry(
            adminId: approvedByAdminId,
            action: $"INSTRUMENT_{request.Action.ToString().ToUpper()}",
            entityType: "Instrument",
            entityId: request.InstrumentId,
            details: new
            {
                symbol = instrument.Symbol,
                previouslyBlocked = instrument.IsBlocked,
                nowBlocked = updatedInstrument.IsBlocked,
                adminRequestId = requestId
            },
            ipAddress: adminIpAddress);

        await _auditLogRepository.AddAsync(instrumentAuditLog, cancellationToken);
        await _adminRequestRepository.SaveChangesAsync(cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Request {RequestId} approved successfully. Instrument {InstrumentId} action '{Action}' executed",
            requestId,
            request.InstrumentId,
            request.Action);

        return _mapper.Map<AdminRequestDto>(approvedRequest);
    }

    public async Task<AdminRequestDto> RejectRequestAsync(
        Guid requestId,
        Guid rejectedByAdminId,
        string adminIpAddress,
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
                instrumentId = request.InstrumentId,
                action = request.Action,
                originalReason = request.Reason,
                rejectionTimestamp = DateTimeOffset.UtcNow
            },
            ipAddress: adminIpAddress);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _adminRequestRepository.SaveChangesAsync(cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Request {RequestId} rejected successfully",
            requestId);

        return _mapper.Map<AdminRequestDto>(rejectedRequest);
    }

    // ======== AUDIT LOG OPERATIONS ========

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByAdminAsync(
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving audit logs for admin {AdminId}", adminId);

        var logs = await _auditLogRepository.GetByAdminIdAsync(adminId, cancellationToken);
        var dtos = logs.Select(_mapper.Map<AuditLogDto>);

        return dtos;
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving audit logs for entity {EntityType} {EntityId}",
            entityType,
            entityId);

        var logs = await _auditLogRepository.GetByEntityAsync(entityType, entityId, cancellationToken);
        var dtos = logs.Select(_mapper.Map<AuditLogDto>);

        return dtos;
    }

    public async Task<(IEnumerable<AuditLogDto> Items, int TotalCount)> GetAuditLogsPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving audit logs - page {PageNumber}, size {PageSize}",
            pageNumber,
            pageSize);

        var (items, totalCount) = await _auditLogRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        var dtos = items.Select(_mapper.Map<AuditLogDto>);

        return (dtos, totalCount);
    }

    // ======== ADMIN AUDIT LOG OPERATIONS ========

    public async Task<IEnumerable<AdminAuditLogDto>> GetAdminAuditLogsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all admin audit logs");

        var logs = await _adminAuditLogRepository.GetAllAsync(cancellationToken);
        var admins = await _userRepository.GetAllUsersAsync(cancellationToken);
        var adminDict = admins.ToDictionary(a => a.Id, a => a.UserName);

        var dtos = logs.Select(log =>
        {
            adminDict.TryGetValue(log.AdminId, out string? adminUserName);
            return new AdminAuditLogDto(
                Id: log.Id,
                AdminId: log.AdminId,
                AdminUserName: adminUserName ?? "Unknown",
                Action: log.Action.ToString(),
                IpAddress: log.IpAddress,
                UserAgent: log.UserAgent,
                CreatedAtUtc: log.CreatedAt,
                Details: log.Details);
        });

        return dtos;
    }

    public async Task<IEnumerable<AdminAuditLogDto>> GetAdminAuditLogsByAdminIdAsync(
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving admin audit logs for admin {AdminId}", adminId);

        var logs = await _adminAuditLogRepository.GetByAdminIdAsync(adminId, cancellationToken);
        var admin = await _userRepository.GetByIdAsync(adminId, cancellationToken);
        var adminUserName = admin?.UserName ?? "Unknown";

        var dtos = logs.Select(log => new AdminAuditLogDto(
            Id: log.Id,
            AdminId: log.AdminId,
            AdminUserName: adminUserName,
            Action: log.Action.ToString(),
            IpAddress: log.IpAddress,
            UserAgent: log.UserAgent,
            CreatedAtUtc: log.CreatedAt,
            Details: log.Details));

        return dtos;
    }

    public async Task<IEnumerable<AdminAuditLogDto>> GetRecentAdminAuditLogsAsync(
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving recent {Count} admin audit logs", count);

        var logs = await _adminAuditLogRepository.GetRecentAsync(count, cancellationToken);
        var admins = await _userRepository.GetAllUsersAsync(cancellationToken);
        var adminDict = admins.ToDictionary(a => a.Id, a => a.UserName);

        var dtos = logs.Select(log =>
        {
            adminDict.TryGetValue(log.AdminId, out string? adminUserName);
            return new AdminAuditLogDto(
                Id: log.Id,
                AdminId: log.AdminId,
                AdminUserName: adminUserName ?? "Unknown",
                Action: log.Action.ToString(),
                IpAddress: log.IpAddress,
                UserAgent: log.UserAgent,
                CreatedAtUtc: log.CreatedAt,
                Details: log.Details);
        });

        return dtos;
    }

    // ======== HELPER METHODS ========

    /// <summary>
    /// Creates an audit log entry with standardized format
    /// </summary>
    private static AuditLog CreateAuditLogEntry(
        Guid adminId,
        string action,
        string? entityType,
        Guid? entityId,
        object details,
        string ipAddress)
    {
        return new AuditLog(
            Id: Guid.NewGuid(),
            AdminId: adminId,
            Action: action,
            EntityType: entityType,
            EntityId: entityId,
            Details: JsonSerializer.Serialize(details, new JsonSerializerOptions { WriteIndented = true }),
            IpAddress: ipAddress,
            CreatedAtUtc: DateTimeOffset.UtcNow);
    }
}
