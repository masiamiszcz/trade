
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Repositories;

/// <summary>
/// Repository for admin authentication operations
/// </summary>
public sealed class AdminAuthRepository : IAdminAuthRepository
{
    private readonly TradingPlatformDbContext _context;

    public AdminAuthRepository(TradingPlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<User?> GetAdminByIdAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Users
            .Where(u => u.Id == adminId && u.Role == UserRole.Admin)
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? null : MapEntityToUser(entity);
    }

    public async Task<User?> GetAdminByUserNameAsync(string username, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Users
            .Where(u => u.UserName.ToLower() == username.ToLower() && u.Role == UserRole.Admin)
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? null : MapEntityToUser(entity);
    }

    /// <summary>
    /// Check if ANY admin exists
    /// Used for bootstrap protection - ensures only ONE super admin can be created
    /// </summary>
    public async Task<bool> HasAnyAdminAsync(CancellationToken cancellationToken = default)
    {
        // Check if any AdminEntity exists (more accurate than checking Users.Role==Admin)
        // since AdminEntity is created after user creation
        return await _context.Admins
            .AnyAsync(cancellationToken);
    }

    public async Task<(User? admin, string? passwordHash)> GetAdminWithPasswordHashAsync(
        string usernameOrEmail, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Where(u => (u.UserName.ToLower() == usernameOrEmail.ToLower() || 
                         u.Email!.ToLower() == usernameOrEmail.ToLower()) && 
                        u.Role == UserRole.Admin)
            .Select(u => new { u, PasswordHash = EF.Property<string>(u, "PasswordHash") })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            return (null, null);

        return (MapEntityToUser(user.u), user.PasswordHash);
    }

    public async Task CreateAdminAsync(User admin, string passwordHash, CancellationToken cancellationToken = default)
    {
        var entity = new UserEntity
        {
            Id = admin.Id,
            UserName = admin.UserName,
            Email = admin.Email,
            FirstName = admin.FirstName,
            LastName = admin.LastName,
            PasswordHash = passwordHash,
            Role = admin.Role,
            Status = UserStatus.Active,
            EmailConfirmed = admin.EmailConfirmed,
            TwoFactorEnabled = admin.TwoFactorEnabled,
            TwoFactorSecret = admin.TwoFactorSecret,  // ✅ FIX: Include encrypted TOTP secret
            BackupCodes = admin.BackupCodes,          // ✅ FIX: Include backup codes JSON
            BaseCurrency = admin.BaseCurrency,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        _context.Users.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Map UserEntity to User (Core model)
    /// </summary>
    private static User MapEntityToUser(UserEntity entity)
    {
        return new User(
            Id: entity.Id,
            UserName: entity.UserName,
            Email: entity.Email,
            FirstName: entity.FirstName,
            LastName: entity.LastName,
            Role: (UserRole)entity.Role,
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
    }

    public async Task UpdateAdminTwoFactorAsync(
        Guid adminId, string encryptedSecret, string backupCodesJson, bool enabled,
        CancellationToken cancellationToken = default)
    {
        var admin = await _context.Users.FindAsync(new object[] { adminId }, cancellationToken);
        if (admin == null)
            throw new InvalidOperationException("Admin not found");

        admin.TwoFactorSecret = encryptedSecret;
        admin.BackupCodes = backupCodesJson;
        admin.TwoFactorEnabled = enabled;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAdminTwoFactorAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var admin = await _context.Users.FindAsync(new object[] { adminId }, cancellationToken);
        if (admin == null)
            throw new InvalidOperationException("Admin not found");

        admin.TwoFactorSecret = null;
        admin.BackupCodes = null;
        admin.TwoFactorEnabled = false;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAdminBackupCodesAsync(Guid adminId, string backupCodesJson, CancellationToken cancellationToken = default)
    {
        var admin = await _context.Users.FindAsync(new object[] { adminId }, cancellationToken);
        if (admin == null)
            throw new InvalidOperationException("Admin not found");

        admin.BackupCodes = backupCodesJson;

        await _context.SaveChangesAsync(cancellationToken);
    }

    // COMMENTED OUT: LastLoginAttemptUtc not needed for current features
    // May use in future for notification system or brute-force detection
    // public async Task UpdateLastLoginAttemptAsync(Guid adminId, CancellationToken cancellationToken = default)
    // {
    //     var admin = await _context.Users.FindAsync(new object[] { adminId }, cancellationToken);
    //     if (admin == null)
    //         throw new InvalidOperationException("Admin not found");
    //
    //     admin.LastLoginAttemptUtc = DateTimeOffset.UtcNow;
    //
    //     await _context.SaveChangesAsync(cancellationToken);
    // }

    /// <summary>
    /// Check if admin is super admin
    /// Returns false if admin doesn't exist or is not in Admins table
    /// </summary>
    public async Task<bool> IsUserSuperAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var admin = await _context.Admins
            .Where(a => a.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
        
        return admin?.IsSuperAdmin ?? false;
    }

    /// <summary>
    /// Create admin entity with SuperAdmin flag
    /// Must be called AFTER CreateAdminAsync to ensure user exists
    /// </summary>
    public async Task CreateAdminEntityAsync(Guid userId, bool isSuperAdmin, CancellationToken cancellationToken = default)
    {
        // Verify user exists
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found. Call CreateAdminAsync first.");

        // Check if admin record already exists (safety)
        var existingAdmin = await _context.Admins
            .Where(a => a.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (existingAdmin != null)
            throw new InvalidOperationException($"Admin record for user {userId} already exists");

        // Create admin entity
        var adminEntity = new AdminEntity
        {
            UserId = userId,
            IsSuperAdmin = isSuperAdmin,
            User = user
        };

        _context.Admins.Add(adminEntity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Repository for admin invitations
/// </summary>
public sealed class AdminInvitationRepository : IAdminInvitationRepository
{
    private readonly TradingPlatformDbContext _context;

    public AdminInvitationRepository(TradingPlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<dynamic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AdminInvitations.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<dynamic?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.AdminInvitations
            .Where(x => x.Token == token)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<dynamic?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.AdminInvitations
            .Where(x => x.Email == email.ToLower())
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<dynamic> GetExpiredInvitationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _context.AdminInvitations
            .Where(x => x.ExpiresAt < now && x.Status != AdminInvitationStatus.Expired)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(dynamic invitation, CancellationToken cancellationToken = default)
    {
        _context.AdminInvitations.Add((AdminInvitationEntity)invitation);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(dynamic invitation, CancellationToken cancellationToken = default)
    {
        _context.AdminInvitations.Update((AdminInvitationEntity)invitation);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Repository for admin registration logs
/// </summary>
public sealed class AdminRegistrationLogRepository : IAdminRegistrationLogRepository
{
    private readonly TradingPlatformDbContext _context;

    public AdminRegistrationLogRepository(TradingPlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddAsync(dynamic log, CancellationToken cancellationToken = default)
    {
        _context.AdminRegistrationLogs.Add((AdminRegistrationLogEntity)log);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Repository for admin audit logs
/// </summary>
public sealed class AdminAuditLogRepository : IAdminAuditLogRepository
{
    private readonly TradingPlatformDbContext _context;

    public AdminAuditLogRepository(TradingPlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<dynamic>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await _context.AdminAuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.Cast<dynamic>().ToList();
    }

    public async Task<IEnumerable<dynamic>> GetByAdminIdAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var results = await _context.AdminAuditLogs
            .Where(x => x.AdminId == adminId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.Cast<dynamic>().ToList();
    }

    public async Task<IEnumerable<dynamic>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        var results = await _context.AdminAuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
        return results.Cast<dynamic>().ToList();
    }

    public async Task AddAsync(dynamic log, CancellationToken cancellationToken = default)
    {
        if (log is AdminAuditLogEntity entity)
        {
            _context.AdminAuditLogs.Add(entity);
        }
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
