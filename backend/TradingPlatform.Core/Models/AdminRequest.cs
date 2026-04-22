
namespace TradingPlatform.Core.Models;

using TradingPlatform.Core.Enums;

/// <summary>
/// Domain model representing an administrative request to perform an action on an instrument.
/// Immutable record used in business logic and service layer.
/// Part of FAZA 3 State Machine Engine - two-step approval workflow.
/// </summary>
public sealed record AdminRequest(
    Guid Id,
    Guid InstrumentId,
    Guid RequestedByAdminId,
    Guid? ApprovedByAdminId,
    AdminRequestActionType Action,     // Type-safe enum: Create, RequestApproval, Approve, Reject, Block, Unblock, Archive, RetrySubmission
    string Reason,                      // Context/notes/rejection reason
    AdminRequestStatus Status,          // Pending | Approved | Rejected
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ApprovedAtUtc);
