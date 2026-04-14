using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

public sealed class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;

    public AccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Account> CreateMainAccountAsync(Guid userId, string baseCurrency, decimal initialBalance = 10000, CancellationToken cancellationToken = default)
    {
        var accountNumber = GenerateAccountNumber();
        var account = new Account(
            Id: Guid.NewGuid(),
            UserId: userId,
            ParentAccountId: null,
            AccountNumber: accountNumber,
            Name: $"Main Account - {baseCurrency}",
            AccountType: AccountType.Main,
            Pillar: AccountPillar.General,
            Status: AccountStatus.Active,
            Currency: baseCurrency,
            AvailableBalance: initialBalance,
            ReservedBalance: 0,
            CreatedAtUtc: DateTimeOffset.UtcNow
        );

        await _accountRepository.AddAsync(account, cancellationToken);
        await _accountRepository.SaveChangesAsync(cancellationToken);

        return account;
    }

    public async Task<Account?> GetMainAccountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _accountRepository.GetMainAccountByUserIdAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetUserAccountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _accountRepository.GetAccountsByUserIdAsync(userId, cancellationToken);
    }

    private static string GenerateAccountNumber()
    {
        // Format: ACC-YYYYMMDD-XXXXXXXX (random 8 chars)
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return $"ACC-{timestamp}-{randomPart}";
    }
}
