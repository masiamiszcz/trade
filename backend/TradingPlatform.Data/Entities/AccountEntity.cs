using TradingPlatform.Core.Enums;

namespace TradingPlatform.Data.Entities;

public sealed class AccountEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ParentAccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; } = AccountType.Main;
    public AccountPillar Pillar { get; set; } = AccountPillar.General;
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public string Currency { get; set; } = "USD";
    public decimal AvailableBalance { get; set; }
    public decimal ReservedBalance { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public UserEntity User { get; set; } = null!;
    public AccountEntity? ParentAccount { get; set; }
    public ICollection<AccountEntity> Subaccounts { get; set; } = [];
    public ICollection<PositionEntity> Positions { get; set; } = [];
    public ICollection<AccountTransferEntity> IncomingTransfers { get; set; } = [];
    public ICollection<AccountTransferEntity> OutgoingTransfers { get; set; } = [];
}
