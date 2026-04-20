
namespace TradingPlatform.Core.Models;

/// <summary>
/// Domain model representing an administrative request to perform an action on an instrument.
/// Immutable record used in business logic and service layer.
/// Part of the two-step approval workflow.
/// </summary>
public sealed record AdminRequest(
    Guid Id,
    Guid InstrumentId,
    Guid RequestedByAdminId,
    Guid? ApprovedByAdminId,
    string Action,
    string Reason,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ApprovedAtUtc);
