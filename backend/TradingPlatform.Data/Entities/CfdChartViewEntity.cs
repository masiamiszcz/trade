namespace TradingPlatform.Data.Entities;

public sealed class CfdChartViewEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConfigurationJson { get; set; } = string.Empty;
    public int ConfigurationVersion { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
