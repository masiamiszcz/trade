public interface IMarketDataHandler
{
    Task HandleAsync(Trade trade);
}