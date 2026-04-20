
namespace TradingPlatform.Data.Entities;

/// <summary>
/// Immutable audit log entry for tracking all administrative actions.
/// Used for compliance, security investigation, and accountability.
/// </summary>
public sealed class AuditLogEntity
{
    /// <summary>
    /// Unique identifier for this audit log entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The admin user who performed this action
    /// </summary>
    public Guid AdminId { get; set; }

    /// <summary>
    /// Type of action performed: "CREATE_REQUEST", "APPROVE_REQUEST", "REJECT_REQUEST", "BLOCK_INSTRUMENT", etc.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity affected: "Instrument", "AdminRequest", "User", etc.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// ID of the entity that was affected
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Detailed information about what changed (JSON format)
    /// Includes: old values, new values, request reasons, approval notes, etc.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// IP address from which this action was performed
    /// Used for security investigation and tracing
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when this action was performed
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    // Navigation property
    public UserEntity? Admin { get; set; }
}
