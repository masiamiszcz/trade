
namespace TradingPlatform.Core.Models;

using TradingPlatform.Core.Enums;

/// <summary>
/// Domain model representing an administrative request to perform an action on any entity.
/// Immutable record used in business logic and service layer.
/// Part of FAZA 3 State Machine Engine - two-step approval workflow.
/// Generic design allows handling of Instrument, User, Account, and future entity types.
/// </summary>
public sealed record AdminRequest(
    Guid Id,
    string EntityType,                  // e.g. "Instrument", "User" - identifies the entity being managed
    Guid? EntityId,                     // null for CREATE actions (new entity), set for UPDATE/DELETE/BLOCK/UNBLOCK
    Guid RequestedByAdminId,
    Guid? ApprovedByAdminId,
    AdminRequestActionType Action,     // Type-safe enum: Create, RequestApproval, Approve, Reject, Block, Unblock, Archive, RetrySubmission
    string? Reason,                     // Context/notes/rejection reason - nullable
    AdminRequestStatus Status,          // Pending | Approved | Rejected
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ApprovedAtUtc,
    string? PayloadJson = null);        // Full request payload as JSON for audit trail
