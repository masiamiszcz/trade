namespace TradingPlatform.Core.Dtos;


/// <summary>
/// Data Transfer Object for AdminRequest
/// Used in API responses and service layer
/// </summary>
public sealed record AdminRequestDto(
    Guid Id,
    Guid InstrumentId,
    Guid RequestedByAdminId,
    Guid? ApprovedByAdminId,
    string Action,
    string Reason,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ApprovedAtUtc);

/// <summary>
/// Request body for creating a block request
/// </summary>
public sealed record CreateBlockRequestRequest(
    Guid InstrumentId,
    string Reason);

/// <summary>
/// Request body for creating an unblock request
/// </summary>
public sealed record CreateUnblockRequestRequest(
    Guid InstrumentId,
    string Reason);

/// <summary>
/// Request body for approving a request
/// </summary>
public sealed record ApproveRequestRequest(
    Guid RequestId);

/// <summary>
/// Request body for rejecting a request
/// </summary>
public sealed record RejectRequestRequest(
    Guid RequestId);

/// <summary>
/// Data Transfer Object for AuditLog
/// Used in API responses
/// </summary>
public sealed record AuditLogDto(
    Guid Id,
    Guid AdminId,
    string Action,
    string? EntityType,
    Guid? EntityId,
    string Details,
    string IpAddress,
    DateTimeOffset CreatedAtUtc);
