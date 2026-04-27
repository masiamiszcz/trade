
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
            u.BlockReason,
            u.BlockedUntilUtc,
            u.DeletedAtUtc,
            u.LastLoginAtUtc,
            u.CreatedAtUtc
        )).ToList();

        return dtos;
    }

    // ======== REQUEST CREATION ========
    // NOTE: Request creation has moved to ApprovalService.CreateRequestAsync
    // This allows centralized management of the approval workflow

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
