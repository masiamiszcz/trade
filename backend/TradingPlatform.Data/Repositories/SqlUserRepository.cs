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
            PasswordHash = passwordHash
        };

        _dbContext.Users.Add(entity);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users.FindAsync(new object[] { user.Id }, cancellationToken);
        if (entity == null)
            throw new InvalidOperationException($"User with ID {user.Id} not found");

        entity.BlockedUntilUtc = user.BlockedUntilUtc;
        entity.BlockReason = user.BlockReason;
        entity.DeletedAtUtc = user.DeletedAtUtc;
        entity.LastLoginAtUtc = user.LastLoginAtUtc;
        entity.Status = user.Status;

        _dbContext.Users.Update(entity);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Users.ToListAsync(cancellationToken);
        return entities.Select(MapToDomain).ToList();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Update user's last login timestamp
    /// </summary>
    public async Task UpdateLastLoginAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (entity == null)
            return;

        entity.LastLoginAtUtc = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static User MapToDomain(UserEntity entity)
    {
        var user = new User(
            Id: entity.Id,
            UserName: entity.UserName,
            Email: entity.Email,
            FirstName: entity.FirstName,
            LastName: entity.LastName,

            Role: entity.Role,
            EmailConfirmed: entity.EmailConfirmed,
            TwoFactorEnabled: entity.TwoFactorEnabled,
            TwoFactorSecret: entity.TwoFactorSecret ?? string.Empty,
            BackupCodes: entity.BackupCodes ?? string.Empty,

            Status: entity.Status,

            BaseCurrency: entity.BaseCurrency,
            CreatedAtUtc: entity.CreatedAtUtc,

            BlockedUntilUtc: entity.BlockedUntilUtc,
            BlockReason: entity.BlockReason,
            DeletedAtUtc: entity.DeletedAtUtc,
            LastLoginAtUtc: entity.LastLoginAtUtc
        );

        return user;
    }
}
