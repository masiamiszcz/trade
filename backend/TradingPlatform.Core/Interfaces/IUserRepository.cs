using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(User? user, string? passwordHash)> GetByUserNameOrEmailWithPasswordHashAsync(string userNameOrEmail, CancellationToken cancellationToken = default);

    Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    Task AddAsync(User user, string passwordHash, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
