
namespace TradingPlatform.Core.Models;

/// <summary>
/// Domain model representing an immutable audit log entry.
/// Used to track all administrative actions for compliance and security.
/// Each entry is created once and never modified (append-only log).
/// </summary>
public sealed record AuditLog(
    Guid Id,
    Guid AdminId,
    string Action,
    string? EntityType,
    Guid? EntityId,
    string Details,
    string IpAddress,
    DateTimeOffset CreatedAtUtc);
