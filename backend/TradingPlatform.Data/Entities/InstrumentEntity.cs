using System.ComponentModel.DataAnnotations;
using TradingPlatform.Core.Enums;

namespace TradingPlatform.Data.Entities;

/// <summary>
/// InstrumentEntity - Persisted domain model with audit trail and concurrency control
/// </summary>
public sealed class InstrumentEntity
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public InstrumentType Type { get; set; }
    public AccountPillar Pillar { get; set; }
    public string BaseCurrency { get; set; } = "USD";
    public string QuoteCurrency { get; set; } = "USD";
    
    // Workflow & Audit Fields
    public InstrumentStatus Status { get; set; } = InstrumentStatus.Draft;
    public bool IsBlocked { get; set; } = false;
    
    // Audit Trail
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }
    
    // Concurrency Control (Optimistic Locking)
    [Timestamp]
    public uint RowVersion { get; set; }
    
    // Relationships
    public ICollection<PositionEntity> Positions { get; set; } = [];
}
