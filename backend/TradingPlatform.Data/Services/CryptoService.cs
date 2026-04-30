using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Entities;
using TradingPlatform.Data.Repositories;

namespace TradingPlatform.Data.Services;

public sealed class CryptoService : ICryptoService
{
    private readonly IInstrumentService _instrumentService;
    private readonly ICandleRepository _candleRepository;

    public CryptoService(
        IInstrumentService instrumentService,
        ICandleRepository candleRepository)
    {
        _instrumentService = instrumentService;
        _candleRepository = candleRepository;
    }

    private const int DAY = 1440;
    private const int YEAR = 525600;
    private const int MAX_RANGE_MINUTES = 20 * YEAR;
    private const string BinanceSource = "binance";

    private static readonly IReadOnlySet<int> SupportedIntervals = new HashSet<int>
    {
        1, 3, 5, 15, 30, 60, 120, 240, 1440
    };

    public async Task<IEnumerable<InstrumentDto>> GetAvailableCryptoInstrumentsAsync(CancellationToken cancellationToken = default)
    {
        var activeInstruments = await _instrumentService.GetAllActiveAsync(cancellationToken);
        var supportedType = InstrumentType.Crypto.ToString();

        return activeInstruments
            .Where(i => string.Equals(i.Type, supportedType, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Symbol);
    }

    public async Task<IEnumerable<CandleDto>> GetCandlesBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        InstrumentDto instrument = await ValidateCryptoSymbolAsync(normalizedSymbol, cancellationToken);

        var candles = await _candleRepository.GetBySymbolAsync(normalizedSymbol, cancellationToken);

        return candles
            .OrderBy(c => c.OpenTime)
            .Select(c => MapToDto(c));
    }

    public async Task<IEnumerable<CandleDto>> GetChartCandlesAsync(
        string symbol,
        int rangeMinutes,
        int? intervalMinutes = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        await ValidateCryptoSymbolAsync(normalizedSymbol, cancellationToken);

        if (rangeMinutes <= 0)
        {
            throw new ArgumentException("RangeMinutes must be greater than zero.", nameof(rangeMinutes));
        }

        if (intervalMinutes.HasValue && intervalMinutes.Value <= 0)
        {
            throw new ArgumentException("IntervalMinutes must be greater than zero.", nameof(intervalMinutes));
        }

        rangeMinutes = Math.Min(rangeMinutes, MAX_RANGE_MINUTES);
        var minimalIntervalMinutes = ResolveIntervalMinutes(rangeMinutes);
        var requestedIntervalMinutes = intervalMinutes ?? minimalIntervalMinutes;

        if (requestedIntervalMinutes > rangeMinutes)
        {
            throw new ArgumentException("IntervalMinutes must be less than or equal to RangeMinutes.", nameof(intervalMinutes));
        }

        if (requestedIntervalMinutes < minimalIntervalMinutes)
        {
            throw new ArgumentException(
                $"IntervalMinutes must be at least {minimalIntervalMinutes} for the requested range of {rangeMinutes} minutes.",
                nameof(intervalMinutes));
        }

        if (!IsSupportedInterval(requestedIntervalMinutes))
        {
            throw new ArgumentException($"IntervalMinutes '{requestedIntervalMinutes}' is not supported.", nameof(intervalMinutes));
        }

        var source = BinanceSource;
        var querySymbol = normalizedSymbol;

        var endTime = to ?? DateTime.UtcNow;
        var rangeStart = endTime.AddMinutes(-rangeMinutes);

        var candles = requestedIntervalMinutes == 1
            ? await _candleRepository.GetBySymbolSourceIntervalAsync(
                querySymbol,
                source,
                1,
                rangeStart,
                endTime,
                cancellationToken)
            : await _candleRepository.GetAggregatedFromOneMinuteSourceAsync(
                querySymbol,
                source,
                requestedIntervalMinutes,
                rangeStart,
                endTime,
                cancellationToken);

        return candles
            .OrderBy(c => c.OpenTime)
            .Select(c => MapToDto(c));
    }


    private static int ResolveIntervalMinutes(int rangeMinutes)
    {
        if (rangeMinutes <= DAY)
            return 1;

        if (rangeMinutes <= 7 * DAY)
            return 5;

        if (rangeMinutes <= 14 * DAY)
            return 15;

        if (rangeMinutes <= 30 * DAY)
            return 30;

        if (rangeMinutes <= YEAR)
            return 60;

        return 1440;
    }

    private static bool IsSupportedInterval(int intervalMinutes)
        => SupportedIntervals.Contains(intervalMinutes);

    private async Task<InstrumentDto> ValidateCryptoSymbolAsync(string symbol, CancellationToken cancellationToken)
    {
        InstrumentDto instrument;

        try
        {
            instrument = await _instrumentService.GetBySymbolAsync(symbol, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new KeyNotFoundException($"Crypto instrument '{symbol}' not found.", ex);
        }

        var supportedType = InstrumentType.Crypto.ToString();
        if (!string.Equals(instrument.Type, supportedType, StringComparison.OrdinalIgnoreCase)
            || instrument.IsBlocked
            || !string.Equals(instrument.Status, InstrumentStatus.Approved.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new KeyNotFoundException($"Crypto instrument '{symbol}' is not available.");
        }

        return instrument;
    }


    private static CandleDto MapToDto(CandleEntity candle)
        => new(
            Symbol: candle.Symbol,
            OpenTime: candle.OpenTime,
            CloseTime: candle.CloseTime,
            Open: candle.Open,
            High: candle.High,
            Low: candle.Low,
            Close: candle.Close,
            Volume: candle.Volume,
            Interval: candle.IntervalMinutes switch
            {
                1 => "1m",
                3 => "3m",
                5 => "5m",
                15 => "15m",
                30 => "30m",
                60 => "1h",
                120 => "2h",
                240 => "4h",
                1440 => "1d",
                _ => $"{candle.IntervalMinutes}m"
            });
}
