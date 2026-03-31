using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Enums;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Tests;

public sealed class TradingPlatformDbContextTests
{
    [Fact]
    public async Task EnsureCreated_ShouldSeedDefaultInstruments()
    {
        var options = CreateOptions();

        await using var context = new TradingPlatformDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var instruments = await context.Instruments
            .OrderBy(x => x.Symbol)
            .ToListAsync();

        Assert.Equal(3, instruments.Count);
        Assert.Contains(instruments, x => x.Symbol == "AAPL" && x.Pillar == AccountPillar.Stocks);
        Assert.Contains(instruments, x => x.Symbol == "BTCUSD" && x.Pillar == AccountPillar.Crypto);
        Assert.Contains(instruments, x => x.Symbol == "US500CFD" && x.Pillar == AccountPillar.Cfd);
    }

    [Fact]
    public async Task SaveChanges_ShouldPersistUserWithMainAndSubaccountHierarchy()
    {
        var options = CreateOptions();
        var userId = Guid.NewGuid();
        var mainAccountId = Guid.NewGuid();

        await using (var setupContext = new TradingPlatformDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();

            setupContext.Users.Add(new UserEntity
            {
                Id = userId,
                UserName = "integration-user",
                Email = "integration@example.com",
                FirstName = "Jan",
                LastName = "Tester",
                PasswordHash = "hashed-password",
                EmailConfirmed = true,
                TwoFactorEnabled = false,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Status = UserStatus.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            setupContext.Accounts.AddRange(
                new AccountEntity
                {
                    Id = mainAccountId,
                    UserId = userId,
                    AccountNumber = "MAIN-0001",
                    Name = "Main account",
                    AccountType = AccountType.Main,
                    Pillar = AccountPillar.General,
                    Status = AccountStatus.Active,
                    Currency = "USD",
                    AvailableBalance = 10000m,
                    ReservedBalance = 0m,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                },
                new AccountEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ParentAccountId = mainAccountId,
                    AccountNumber = "SUB-STOCKS-0001",
                    Name = "Stocks subaccount",
                    AccountType = AccountType.Subaccount,
                    Pillar = AccountPillar.Stocks,
                    Status = AccountStatus.Active,
                    Currency = "USD",
                    AvailableBalance = 2500m,
                    ReservedBalance = 0m,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });

            await setupContext.SaveChangesAsync();
        }

        await using var verifyContext = new TradingPlatformDbContext(options);

        var user = await verifyContext.Users
            .Include(x => x.Accounts)
            .SingleAsync(x => x.Id == userId);

        Assert.Equal(2, user.Accounts.Count);
        Assert.Contains(user.Accounts, x => x.AccountType == AccountType.Main && x.Pillar == AccountPillar.General);
        Assert.Contains(user.Accounts, x => x.AccountType == AccountType.Subaccount && x.ParentAccountId == mainAccountId && x.Pillar == AccountPillar.Stocks);
    }

    [Fact]
    public async Task SaveChanges_ShouldPersistTransferAndPositionRelations()
    {
        var options = CreateOptions();
        var userId = Guid.NewGuid();
        var mainAccountId = Guid.NewGuid();
        var cryptoAccountId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var transferId = Guid.NewGuid();

        await using (var setupContext = new TradingPlatformDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();

            setupContext.Users.Add(new UserEntity
            {
                Id = userId,
                UserName = "portfolio-user",
                Email = "portfolio@example.com",
                FirstName = "Anna",
                LastName = "Trader",
                PasswordHash = "hashed-password",
                EmailConfirmed = true,
                TwoFactorEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                Status = UserStatus.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            setupContext.Accounts.AddRange(
                new AccountEntity
                {
                    Id = mainAccountId,
                    UserId = userId,
                    AccountNumber = "MAIN-0002",
                    Name = "Main account",
                    AccountType = AccountType.Main,
                    Pillar = AccountPillar.General,
                    Status = AccountStatus.Active,
                    Currency = "USD",
                    AvailableBalance = 15000m,
                    ReservedBalance = 0m,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                },
                new AccountEntity
                {
                    Id = cryptoAccountId,
                    UserId = userId,
                    ParentAccountId = mainAccountId,
                    AccountNumber = "SUB-CRYPTO-0002",
                    Name = "Crypto subaccount",
                    AccountType = AccountType.Subaccount,
                    Pillar = AccountPillar.Crypto,
                    Status = AccountStatus.Active,
                    Currency = "USD",
                    AvailableBalance = 3000m,
                    ReservedBalance = 500m,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });

            setupContext.Positions.Add(new PositionEntity
            {
                Id = positionId,
                AccountId = cryptoAccountId,
                InstrumentId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Quantity = 0.125m,
                ReservedQuantity = 0.025m,
                AverageOpenPrice = 64000m,
                OpenedAtUtc = DateTimeOffset.UtcNow
            });

            setupContext.AccountTransfers.Add(new AccountTransferEntity
            {
                Id = transferId,
                FromAccountId = mainAccountId,
                ToAccountId = cryptoAccountId,
                Amount = 1200m,
                Currency = "USD",
                TransferType = TransferType.Internal,
                Status = TransferStatus.Completed,
                Title = "Initial funding",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CompletedAtUtc = DateTimeOffset.UtcNow
            });

            await setupContext.SaveChangesAsync();
        }

        await using var verifyContext = new TradingPlatformDbContext(options);

        var savedPosition = await verifyContext.Positions
            .Include(x => x.Account)
            .Include(x => x.Instrument)
            .SingleAsync(x => x.Id == positionId);

        var savedTransfer = await verifyContext.AccountTransfers
            .Include(x => x.FromAccount)
            .Include(x => x.ToAccount)
            .SingleAsync(x => x.Id == transferId);

        Assert.Equal("BTCUSD", savedPosition.Instrument.Symbol);
        Assert.Equal(AccountPillar.Crypto, savedPosition.Account.Pillar);
        Assert.Equal(1200m, savedTransfer.Amount);
        Assert.Equal("MAIN-0002", savedTransfer.FromAccount!.AccountNumber);
        Assert.Equal("SUB-CRYPTO-0002", savedTransfer.ToAccount!.AccountNumber);
    }

    private static DbContextOptions<TradingPlatformDbContext> CreateOptions()
        => new DbContextOptionsBuilder<TradingPlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
}
