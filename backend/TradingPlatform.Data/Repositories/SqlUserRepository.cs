using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Repositories;

public sealed class SqlUserRepository : IUserRepository
{
    private readonly TradingPlatformDbContext _dbContext;

    public SqlUserRepository(TradingPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == userName.ToLower(), cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users.FindAsync(new object[] { id }, cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<(User? user, string? passwordHash)> GetByUserNameOrEmailWithPasswordHashAsync(string userNameOrEmail, CancellationToken cancellationToken = default)
    {
        var checkDto = userNameOrEmail.ToLower();
        var entity = await _dbContext.Users.FirstOrDefaultAsync(x =>
            x.UserName.ToLower() == checkDto
            || x.Email.ToLower() == checkDto,
            cancellationToken);

        return entity is null
            ? (null, null)
            : (MapToDomain(entity), entity.PasswordHash);
    }

    public async Task AddAsync(User user, string passwordHash, CancellationToken cancellationToken = default)
    {
        var entity = new UserEntity
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            TwoFactorSecret = user.TwoFactorSecret,
            BackupCodes = user.BackupCodes,
            Status = user.Status,
            Role = user.Role,
            BaseCurrency = user.BaseCurrency,
            CreatedAtUtc = user.CreatedAtUtc,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            PasswordHash = passwordHash,
            // NEW: Lifecycle fields - initialized as null for new users
            BlockedUntilUtc = user.BlockedUntilUtc,
            BlockReason = user.BlockReason,
            DeletedAtUtc = user.DeletedAtUtc,
            DeleteReason = user.DeleteReason,
            LastModifiedByAdminId = user.LastModifiedByAdminId
        };

        _dbContext.Users.Add(entity);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Users.ToListAsync(cancellationToken);
        return entities.Select(MapToDomain).ToList();
    }

    /// <summary>
    /// Get all users with optional soft-deleted filtering.
    /// </summary>
    public async Task<IEnumerable<User>> GetAllUsersAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsQueryable();
        
        if (!includeDeleted)
        {
            query = query.Where(u => u.Status != Core.Enums.UserStatus.Deleted);
        }
        
        var entities = await query.ToListAsync(cancellationToken);
        return entities.Select(MapToDomain).ToList();
    }

    /// <summary>
    /// Block a user temporarily (48h default).
    /// Single database operation - NO approval needed.
    /// </summary>
    public async Task BlockUserAsync(Guid userId, string reason, DateTimeOffset blockedUntil, Guid adminId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} not found");

        entity.Status = Core.Enums.UserStatus.Blocked;
        entity.BlockedUntilUtc = blockedUntil;
        entity.BlockReason = reason;
        entity.LastModifiedByAdminId = adminId;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _dbContext.Users.Update(entity);
    }

    /// <summary>
    /// Unblock a user immediately.
    /// Single database operation - NO approval needed.
    /// </summary>
    public async Task UnblockUserAsync(Guid userId, Guid adminId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} not found");

        entity.Status = Core.Enums.UserStatus.Active;
        entity.BlockedUntilUtc = null;
        entity.BlockReason = null;
        entity.LastModifiedByAdminId = adminId;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _dbContext.Users.Update(entity);
    }

    /// <summary>
    /// Soft delete a user after approval.
    /// Sets Status = Deleted and records deletion timestamp.
    /// </summary>
    public async Task DeleteUserAsync(Guid userId, string reason, Guid adminId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} not found");

        entity.Status = Core.Enums.UserStatus.Deleted;
        entity.DeletedAtUtc = DateTimeOffset.UtcNow;
        entity.DeleteReason = reason;
        entity.LastModifiedByAdminId = adminId;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _dbContext.Users.Update(entity);
    }

    /// <summary>
    /// Restore a soft-deleted user after approval.
    /// Sets Status = Active and clears deletion info.
    /// </summary>
    public async Task RestoreUserAsync(Guid userId, Guid adminId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} not found");

        entity.Status = Core.Enums.UserStatus.Active;
        entity.DeletedAtUtc = null;
        entity.DeleteReason = null;
        entity.LastModifiedByAdminId = adminId;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _dbContext.Users.Update(entity);
    }

    /// <summary>
    /// Get user by ID including soft-deleted users.
    /// Used for validation in delete/restore requests.
    /// </summary>
    public async Task<User?> GetUserByIdIncludingDeletedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static User MapToDomain(UserEntity entity)
        => new User(
            entity.Id,
            entity.UserName,
            entity.Email,
            entity.FirstName,
            entity.LastName,
            entity.Role,
            entity.EmailConfirmed,
            entity.TwoFactorEnabled,
            entity.TwoFactorSecret,
            entity.BackupCodes,
            entity.Status,
            entity.BaseCurrency,
            entity.CreatedAtUtc,
            // NEW: Lifecycle fields
            entity.BlockedUntilUtc,
            entity.BlockReason,
            entity.DeletedAtUtc,
            entity.DeleteReason,
            entity.LastModifiedByAdminId
        );
}
