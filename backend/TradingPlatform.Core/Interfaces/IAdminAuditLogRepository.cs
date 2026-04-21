namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Repository for admin audit logs
/// Tracks security-relevant actions by administrators
/// </summary>
public interface IAdminAuditLogRepository
{
    /// <summary>
    /// Get all audit logs
    /// </summary>
    Task<IEnumerable<dynamic>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for specific admin
    /// </summary>
    Task<IEnumerable<dynamic>> GetByAdminIdAsync(Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent audit logs (last N entries)
    /// </summary>
    Task<IEnumerable<dynamic>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add audit log entry
    /// </summary>
    Task AddAsync(dynamic log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

