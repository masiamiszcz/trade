public class Trade
{
    public string Symbol { get; init; }
    public decimal Price { get; init; }
    public decimal Quantity { get; init; }
    public DateTime Timestamp { get; init; }
    public bool IsBuyerMaker { get; init; }
}