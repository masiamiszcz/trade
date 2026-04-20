using TradingPlatform.Core.Enums;

namespace TradingPlatform.Data.Entities;

public sealed class InstrumentEntity
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public InstrumentType Type { get; set; }
    public AccountPillar Pillar { get; set; }
    public string BaseCurrency { get; set; } = "USD";
    public string QuoteCurrency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public bool IsBlocked { get; set; } = false;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<PositionEntity> Positions { get; set; } = [];
}
