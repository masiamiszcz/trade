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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static User MapToDomain(UserEntity entity)
        => new User(entity.Id, entity.UserName, entity.Email, entity.FirstName, entity.LastName, entity.Role, entity.EmailConfirmed, entity.TwoFactorEnabled, entity.TwoFactorSecret, entity.BackupCodes, entity.Status, entity.BaseCurrency, entity.CreatedAtUtc);
}
