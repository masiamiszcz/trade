
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Repository interface for managing AuditLog persistence.
/// Provides write and query operations for immutable audit trail.
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Get a specific audit log entry by ID
    /// </summary>
    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all audit log entries for a specific admin
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByAdminIdAsync(Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all audit log entries for a specific entity (instrument, request, etc.)
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by action type
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByActionAsync(string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated audit logs in chronological order
    /// </summary>
    Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new audit log entry (append-only)
    /// </summary>
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persist changes to the database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
