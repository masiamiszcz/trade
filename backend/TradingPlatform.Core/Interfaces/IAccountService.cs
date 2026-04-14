using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

public interface IAccountService
{
    Task<Account> CreateMainAccountAsync(Guid userId, string baseCurrency, decimal initialBalance = 10000, CancellationToken cancellationToken = default);
    Task<Account?> GetMainAccountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetUserAccountsAsync(Guid userId, CancellationToken cancellationToken = default);
}
