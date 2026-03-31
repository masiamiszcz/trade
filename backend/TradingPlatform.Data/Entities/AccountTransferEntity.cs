using TradingPlatform.Core.Enums;

namespace TradingPlatform.Data.Entities;

public sealed class AccountTransferEntity
{
    public Guid Id { get; set; }
    public Guid? FromAccountId { get; set; }
    public Guid? ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public TransferType TransferType { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public string? Title { get; set; }
    public string? ExternalReference { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }

    public AccountEntity? FromAccount { get; set; }
    public AccountEntity? ToAccount { get; set; }
}
