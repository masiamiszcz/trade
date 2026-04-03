namespace TradingPlatform.Core.Models;

public class HealthStatus
{
    public string Status { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public bool DatabaseHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}