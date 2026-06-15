using TradingPlatform.Core.Models;

public interface IMarketDataHandler
{
    Task HandleAsync(Trade trade);
}