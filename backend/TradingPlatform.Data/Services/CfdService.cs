using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Entities;
using TradingPlatform.Data.Repositories;
using TradingPlatform.Data.Services.Cfd;

namespace TradingPlatform.Data.Services;

public sealed class CfdService : ICfdService
{
    private const int Day = 1440;
    private const int FourHours = 240;
    private const int SevenDays = 7 * Day;
    private const int Year = 525600;
    private const int MaxRangeMinutes = 20 * Year;
    private static readonly IReadOnlySet<int> SupportedIntervals = new HashSet<int>
    {
        1, 3, 5, 15, 30, 60, 120, 240, 1440
    };

    private readonly IInstrumentService _instrumentService;
    private readonly ICandleRepository _candleRepository;

    public CfdService(
        IInstrumentService instrumentService,
        ICandleRepository candleRepository)
    {
        _instrumentService = instrumentService;
        _candleRepository = candleRepository;
    }

    public async Task<IEnumerable<CfdInstrumentSummaryDto>> GetAvailableCfdInstrumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var activeInstruments = await _instrumentService.GetAllActiveAsync(cancellationToken);
        var cfdType = InstrumentType.Cfd.ToString();

        var availableInstruments = activeInstruments
            .Where(instrument =>
                string.Equals(instrument.Type, cfdType, StringComparison.OrdinalIgnoreCase)
                && CfdMarketDataSourceMap.TryGetSource(instrument.Symbol, out _))
            .OrderBy(instrument => instrument.Symbol);

        var now = DateTime.UtcNow;
        var summaries = await Task.WhenAll(availableInstruments.Select(
            instrument => GetInstrumentSummaryAsync(instrument, now, cancellationToken)));
        return summaries;
    }

    private async Task<CfdInstrumentSummaryDto> GetInstrumentSummaryAsync(
        InstrumentDto instrument,
        DateTime now,
        CancellationToken cancellationToken)
    {
        CfdMarketDataSourceMap.TryGetSource(instrument.Symbol, out var marketDataSource);
        var hourlyCandles = await _candleRepository.GetBySymbolSourceIntervalAsync(
            marketDataSource.MarketSymbol,
            marketDataSource.Provider,
            1,
            now.AddMinutes(-Day),
            now,
            cancellationToken);
        var sparklineCandles = await _candleRepository.GetAggregatedFromOneMinuteSourceAsync(
            marketDataSource.MarketSymbol,
            marketDataSource.Provider,
            FourHours,
            now.AddDays(-7),
            now,
            cancellationToken);

        var orderedHourlyCandles = hourlyCandles.OrderBy(candle => candle.OpenTime).ToList();
        var lastCandle = orderedHourlyCandles.LastOrDefault();

        return new CfdInstrumentSummaryDto(
            Instrument: instrument,
            CurrentPrice: lastCandle?.Close,
            Low1h: GetLow(orderedHourlyCandles, now.AddHours(-1)),
            High1h: GetHigh(orderedHourlyCandles, now.AddHours(-1)),
            ChangePercent1h: GetChangePercent(orderedHourlyCandles, now.AddHours(-1)),
            Low24h: GetLow(orderedHourlyCandles, now.AddHours(-24)),
            High24h: GetHigh(orderedHourlyCandles, now.AddHours(-24)),
            ChangePercent24h: GetChangePercent(orderedHourlyCandles, now.AddHours(-24)),
            Sparkline7d: sparklineCandles
                .OrderBy(candle => candle.OpenTime)
                .Select(candle => candle.Close)
                .ToArray(),
            CandleOpenTimeUtc: lastCandle is null
                ? null
                : DateTime.SpecifyKind(lastCandle.OpenTime, DateTimeKind.Utc));
    }

    private static decimal? GetLow(IEnumerable<CandleEntity> candles, DateTime since)
    {
        var window = candles.Where(candle => candle.OpenTime >= since).ToList();
        return window.Count == 0 ? null : window.Min(candle => candle.Low);
    }

    private static decimal? GetHigh(IEnumerable<CandleEntity> candles, DateTime since)
    {
        var window = candles.Where(candle => candle.OpenTime >= since).ToList();
        return window.Count == 0 ? null : window.Max(candle => candle.High);
    }

    private static decimal? GetChangePercent(IEnumerable<CandleEntity> candles, DateTime since)
    {
        var window = candles.Where(candle => candle.OpenTime >= since)
            .OrderBy(candle => candle.OpenTime)
            .ToList();
        if (window.Count == 0 || window[0].Open == 0)
            return null;

        return (window[^1].Close / window[0].Open - 1m) * 100m;
    }

    public async Task<IEnumerable<CandleDto>> GetChartCandlesAsync(
        string symbol,
        int rangeMinutes,
        int? intervalMinutes = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        await ValidateCfdSymbolAsync(normalizedSymbol, cancellationToken);

        if (!CfdMarketDataSourceMap.TryGetSource(normalizedSymbol, out var marketDataSource))
        {
            throw new KeyNotFoundException(
                $"No market data source is configured for CFD instrument '{normalizedSymbol}'.");
        }

        if (rangeMinutes <= 0)
        {
            throw new ArgumentException("RangeMinutes must be greater than zero.", nameof(rangeMinutes));
        }

        if (intervalMinutes.HasValue && intervalMinutes.Value <= 0)
        {
            throw new ArgumentException("IntervalMinutes must be greater than zero when specified.", nameof(intervalMinutes));
        }

        rangeMinutes = Math.Min(rangeMinutes, MaxRangeMinutes);
        var minimumIntervalMinutes = ResolveIntervalMinutes(rangeMinutes);
        var requestedIntervalMinutes = intervalMinutes ?? minimumIntervalMinutes;

        if (requestedIntervalMinutes > rangeMinutes)
        {
            throw new ArgumentException("IntervalMinutes must be less than or equal to RangeMinutes.", nameof(intervalMinutes));
        }

        if (requestedIntervalMinutes < minimumIntervalMinutes)
        {
            throw new ArgumentException(
                $"IntervalMinutes must be at least {minimumIntervalMinutes} for the requested range of {rangeMinutes} minutes.",
                nameof(intervalMinutes));
        }

        if (!SupportedIntervals.Contains(requestedIntervalMinutes))
        {
            throw new ArgumentException($"IntervalMinutes '{requestedIntervalMinutes}' is not supported.", nameof(intervalMinutes));
        }

        var endTime = to ?? DateTime.UtcNow;
        var startTime = endTime.AddMinutes(-rangeMinutes);
        var candles = requestedIntervalMinutes == 1
            ? await _candleRepository.GetBySymbolSourceIntervalAsync(
                marketDataSource.MarketSymbol,
                marketDataSource.Provider,
                1,
                startTime,
                endTime,
                cancellationToken)
            : await _candleRepository.GetAggregatedFromOneMinuteSourceAsync(
                marketDataSource.MarketSymbol,
                marketDataSource.Provider,
                requestedIntervalMinutes,
                startTime,
                endTime,
                cancellationToken);

        return candles
            .OrderBy(candle => candle.OpenTime)
            .Select(candle => MapToDto(candle, normalizedSymbol));
    }

    private async Task<InstrumentDto> ValidateCfdSymbolAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        InstrumentDto instrument;

        try
        {
            instrument = await _instrumentService.GetBySymbolAsync(
                symbol,
                InstrumentType.Cfd,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new KeyNotFoundException($"CFD instrument '{symbol}' not found.", ex);
        }

        if (instrument.IsBlocked
            || !string.Equals(instrument.Status, InstrumentStatus.Approved.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new KeyNotFoundException($"CFD instrument '{symbol}' is not available.");
        }

        return instrument;
    }

    private static string NormalizeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol is required.", nameof(symbol));
        }

        return symbol.Trim().ToUpperInvariant();
    }

    private static int ResolveIntervalMinutes(int rangeMinutes)
    {
        if (rangeMinutes <= Day)
            return 1;

        if (rangeMinutes <= 7 * Day)
            return 5;

        if (rangeMinutes <= 14 * Day)
            return 15;

        if (rangeMinutes <= 30 * Day)
            return 30;

        if (rangeMinutes <= Year)
            return 60;

        return 1440;
    }

    private static CandleDto MapToDto(CandleEntity candle, string instrumentSymbol)
        => new(
            Symbol: instrumentSymbol,
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
