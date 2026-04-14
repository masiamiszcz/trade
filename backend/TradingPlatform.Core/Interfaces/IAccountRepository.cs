using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetMainAccountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetAccountsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
