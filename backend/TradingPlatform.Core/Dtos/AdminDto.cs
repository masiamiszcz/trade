namespace TradingPlatform.Core.Dtos;


/// <summary>
/// Data Transfer Object for AdminRequest
/// Used in API responses and service layer
/// Generic design: supports any entity type via EntityType + EntityId
/// </summary>
public sealed record AdminRequestDto(
    Guid Id,
    string EntityType,
    Guid? EntityId,
    Guid RequestedByAdminId,
    Guid? ApprovedByAdminId,
    string Action,
    string? Reason,
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

// UserListItemDto moved to UserListItemDto.cs

/// <summary>
/// Data Transfer Object for AdminAuditLog
/// Used in API responses for admin action history
/// </summary>
public sealed record AdminAuditLogDto(
    Guid Id,
    Guid AdminId,
    string AdminUserName,
    string Action,
    string IpAddress,
    string UserAgent,
    DateTimeOffset CreatedAtUtc,
    string? Details);
