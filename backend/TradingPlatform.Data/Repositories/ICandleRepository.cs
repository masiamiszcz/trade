using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Repositories;

public interface ICandleRepository
{
    Task AddAsync(CandleEntity candle, CancellationToken cancellationToken = default);
    Task<List<CandleEntity>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
    Task<List<CandleEntity>> GetBySymbolSourceIntervalAsync(
        string symbol,
        string source,
        int intervalMinutes,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<List<CandleEntity>> GetAggregatedFromOneMinuteSourceAsync(
        string symbol,
        string source,
        int targetIntervalMinutes,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<List<CandleEntity>> GetBySourceIntervalOpenTimeAsync(
        string source,
        int intervalMinutes,
        DateTime openTime,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string source, int intervalMinutes, DateTime openTime, CancellationToken cancellationToken = default);

    Task<DateTime?> GetLastCandleTimestampAsync(string symbol, string source, int intervalMinutes, CancellationToken cancellationToken = default);

    Task<List<DateTime>> GetExistingOpenTimesAsync(
        string symbol,
        string source,
        int intervalMinutes,
        IEnumerable<DateTime> openTimes,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<CandleEntity> candles, CancellationToken cancellationToken = default);

}
