using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Data.Services.Market;

public class BinanceWebSocketService : BackgroundService
{
    private readonly ILogger<BinanceWebSocketService> _logger;
    private readonly MarketProcessingService _marketProcessor;
    private readonly Channel<Trade> _channel;
    private readonly BinanceSettings _settings;

    private decimal _lastPrice;

    public BinanceWebSocketService(
        ILogger<BinanceWebSocketService> logger,
        MarketProcessingService marketProcessor,
        Channel<Trade> channel,
        IOptions<BinanceSettings> settings)
    {
        _logger = logger;
        _marketProcessor = marketProcessor;
        _channel = channel;
        _settings = settings.Value;
    }
    

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Binance WS Service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunWebSocketAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ WS ERROR: {Message}", ex.Message);

                // mały delay żeby nie robić pętli crashów
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    private async Task RunWebSocketAsync(CancellationToken token)
    {
        using var socket = new ClientWebSocket();

        socket.Options.KeepAliveInterval = TimeSpan.FromMilliseconds(_settings.HeartbeatIntervalMs);

        var streams = string.Join('/', _settings.StreamSymbols.Select(s => s.Trim().ToLowerInvariant() + "@trade"));
        var baseUrl = _settings.WebSocketUrl.TrimEnd('/');

        if (baseUrl.EndsWith("/ws", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = baseUrl[..^3] + "/stream";
        }

        var url = baseUrl.Contains("/stream", StringComparison.OrdinalIgnoreCase)
            ? $"{baseUrl}?streams={streams}"
            : $"{baseUrl}/stream?streams={streams}";

        _logger.LogInformation("🔌 Connecting to Binance: {Url}", url);

        await socket.ConnectAsync(new Uri(url), token);

        _logger.LogInformation("✅ Connected to Binance WS");

        var buffer = new byte[8192];

        while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
        {
            using var receiveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            receiveCts.CancelAfter(_settings.MessageTimeout);

            try
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), receiveCts.Token);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var wrapper = JsonSerializer.Deserialize<BinanceStreamResponseDto>(json);
                    var dto = wrapper?.data ?? JsonSerializer.Deserialize<BinanceTradeDto>(json);

                    if (dto is null)
                    {
                        _logger.LogWarning("⚠️ Binance trade payload could not be parsed: {Json}", json);
                        continue;
                    }

                    var trade = TradeMapper.Map(dto);
                    await _marketProcessor.HandleAsync(trade);
                    await _channel.Writer.WriteAsync(trade);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning("⚠️ Binance closed connection");
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", token);
                }
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                _logger.LogWarning("⌛ Binance WS read timeout reached after {Timeout}ms", _settings.MessageTimeout);
                return;
            }
        }
    }
}