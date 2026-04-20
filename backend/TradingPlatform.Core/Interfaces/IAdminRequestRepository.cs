
// IAdminRequestRepository
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Repository interface for managing AdminRequest persistence.
/// Provides CRUD operations and query methods for admin requests.
/// </summary>
public interface IAdminRequestRepository
{
    /// <summary>
    /// Get a specific admin request by ID
    /// </summary>
    Task<AdminRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all pending admin requests (awaiting approval)
    /// </summary>
    Task<IEnumerable<AdminRequest>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all admin requests for a specific instrument
    /// </summary>
    Task<IEnumerable<AdminRequest>> GetByInstrumentIdAsync(Guid instrumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all admin requests created by a specific admin
    /// </summary>
    Task<IEnumerable<AdminRequest>> GetByRequestedByAdminIdAsync(Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated list of all admin requests
    /// </summary>
    Task<(IEnumerable<AdminRequest> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new admin request
    /// </summary>
    Task AddAsync(AdminRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing admin request
    /// </summary>
    Task UpdateAsync(AdminRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persist all changes to the database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
