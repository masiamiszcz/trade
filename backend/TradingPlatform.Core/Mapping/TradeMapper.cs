using System.Globalization;
using TradingPlatform.Core.Dtos;

public static class TradeMapper
{
    public static Trade Map(BinanceTradeDto dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.s))
            throw new ArgumentException("Binance trade payload is missing symbol.", nameof(dto));

        if (!decimal.TryParse(dto.p, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
            throw new FormatException($"Binance trade payload contains invalid price: '{dto.p}'");

        if (!decimal.TryParse(dto.q, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity))
            throw new FormatException($"Binance trade payload contains invalid quantity: '{dto.q}'");

        return new Trade
        {
            Symbol = dto.s,
            Price = price,
            Quantity = quantity,
            Timestamp = DateTimeOffset
                .FromUnixTimeMilliseconds(dto.T)
                .UtcDateTime,
            IsBuyerMaker = dto.m
        };
    }
}