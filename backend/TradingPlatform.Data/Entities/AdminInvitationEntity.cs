namespace TradingPlatform.Data.Entities;


/// <summary>
/// Represents an invitation for a new admin account
/// Super Admin generates these tokens to invite new administrators
/// Token expires after 48 hours and can only be used once
/// </summary>
public sealed class AdminInvitationEntity
{
    /// <summary>
    /// Unique identifier for this invitation
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique token (32 alphanumeric chars) sent to email
    /// Example: /admin/register-invite?token=ABC123XYZ...
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the admin being invited
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First name for the new admin account
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name for the new admin account
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Super Admin who created this invitation (FK to Users.Id)
    /// </summary>
    public Guid InvitedBy { get; set; }

    /// <summary>
    /// When the invitation was generated
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the invitation expires (typically 48 hours from creation)
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// When the invitation was used (if at all)
    /// Null = not used yet
    /// </summary>
    public DateTimeOffset? UsedAt { get; set; }

    /// <summary>
    /// Admin who registered using this invitation (FK to Users.Id)
    /// Null = invitation not used yet
    /// </summary>
    public Guid? UsedBy { get; set; }

    /// <summary>
    /// Current status of this invitation
    /// Pending = not used yet and not expired
    /// Used = admin registered with this token
    /// Expired = token validity period has passed
    /// </summary>
    public AdminInvitationStatus Status { get; set; } = AdminInvitationStatus.Pending;

    /// <summary>
    /// JSON array of permissions/roles to assign
    /// Example: ["ManageInstruments", "ManageUsers", "ViewAuditLogs"]
    /// Optional - can be set by Super Admin during invitation
    /// </summary>
    public string? Permissions { get; set; }
}

/// <summary>
/// Status of an admin invitation token
/// </summary>
public enum AdminInvitationStatus
{
    Pending = 1,
    Used = 2,
    Expired = 3,
    Revoked = 4
}
