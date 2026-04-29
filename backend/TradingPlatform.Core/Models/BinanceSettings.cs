namespace TradingPlatform.Core.Models;

public class BinanceSettings
{
    public string WebSocketUrl { get; set; } = "wss://stream.binance.com:9443/ws";
    public string[] StreamSymbols { get; set; } = new[] { "btcusdt" };
    public ReconnectPolicySettings ReconnectPolicy { get; set; } = new();
    public int MessageTimeout { get; set; } = 10000;
    public int HeartbeatIntervalMs { get; set; } = 30000;
    public int PriceCacheTtlSeconds { get; set; } = 30;
}

public sealed class ReconnectPolicySettings
{
    public string Mode { get; set; } = "exponential";
    public int MaxRetries { get; set; } = 5;
    public int DelayMs { get; set; } = 5000;
}
