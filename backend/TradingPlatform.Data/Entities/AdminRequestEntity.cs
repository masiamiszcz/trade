
using TradingPlatform.Core.Enums;

namespace TradingPlatform.Data.Entities;


/// <summary>
/// Represents an administrative request to perform an action on any entity (Instrument, User, Account, etc).
/// Used in a two-step approval workflow where an admin creates a request
/// and another admin (approver) must approve it before execution.
/// Generic design: EntityType + EntityId replace entity-specific foreign keys.
/// </summary>
public sealed class AdminRequestEntity
{
    /// <summary>
    /// Unique identifier for this request
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of entity being managed: "Instrument", "User", "Account", etc.
    /// </summary>
    public string EntityType { get; set; } = default!;

    /// <summary>
    /// The entity ID being managed. Null for CREATE actions (before entity exists).
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// The admin user who created this request
    /// </summary>
    public Guid RequestedByAdminId { get; set; }

    /// <summary>
    /// The admin user who approved this request (null if pending)
    /// </summary>
    public Guid? ApprovedByAdminId { get; set; }

    /// <summary>
    /// The type of action requested: Create, RequestApproval, Approve, Reject, Block, Unblock, Archive, RetrySubmission
    /// </summary>
    public AdminRequestActionType Action { get; set; }

    /// <summary>
    /// The reason for this request (for audit trail). Optional.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Current status: Pending, Approved, Rejected
    /// </summary>
    public AdminRequestStatus Status { get; set; } = AdminRequestStatus.Pending;

    /// <summary>
    /// When this request was created
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// When this request was approved or rejected (null if still pending)
    /// </summary>
    public DateTimeOffset? ApprovedAtUtc { get; set; }

    /// <summary>
    /// JSON payload containing the data for this request (for audit trail and execution)
    /// </summary>
    public string? PayloadJson { get; set; }

    // Navigation properties
    public UserEntity? RequestedByAdmin { get; set; }
    public UserEntity? ApprovedByAdmin { get; set; }
}
