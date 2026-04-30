using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingPlatform.Data.Entities;
using TradingPlatform.Data.External;
using TradingPlatform.Data.Repositories;

namespace TradingPlatform.Data.Services.Market;

public sealed class BinanceStartupPollingService : BackgroundService
{
    private const string CsvFolderPath = "C:\\Users\\kubac\\Desktop\\Studia\\CSV_BTCUSDT";
    private const int BatchSize = 2000;
    private const string Symbol = "BTCUSDT";
    private const int IntervalMinutes = 1;
    private const int ApiFetchLimit = 1000;
    private const string Source = "binance";

    private readonly IBinanceApiClient _apiClient;
    private readonly ICandleRepository _candleRepository;
    private readonly ILogger<BinanceStartupPollingService> _logger;

    public BinanceStartupPollingService(
        IBinanceApiClient apiClient,
        ICandleRepository candleRepository,
        ILogger<BinanceStartupPollingService> logger)
    {
        _apiClient = apiClient;
        _candleRepository = candleRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Startup Binance polling service initialising...");
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        await ImportBinanceCsvFilesAsync(stoppingToken);

        var lastTimestamp = await _candleRepository.GetLastCandleTimestampAsync(Symbol, Source, IntervalMinutes, stoppingToken);
        if (lastTimestamp.HasValue)
        {
            _logger.LogInformation("Last candle in DB for {Symbol} is {LastTimestamp:O}", Symbol, lastTimestamp.Value);
        }
        else
        {
            _logger.LogInformation("No existing candle data found for {Symbol}. Will fetch from Binance starting from the earliest available candles.", Symbol);
        }

        try
        {
            DateTime? startTime = lastTimestamp.HasValue
                ? lastTimestamp.Value.AddMinutes(IntervalMinutes)
                : null;
            var klines = await FetchMissingBinanceKlinesAsync(startTime, stoppingToken);
            _logger.LogInformation("Binance incremental sync complete. Retrieved {Count} klines.", klines?.Count() ?? 0);

            if (klines is not null && klines.Any())
            {
                await SaveKlinesAsync(klines, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Binance incremental sync failed: {Message}", ex.Message);
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task ImportBinanceCsvFilesAsync(CancellationToken cancellationToken)
        => ImportCsvFolderAsync(CsvFolderPath, cancellationToken);

    private async Task<IEnumerable<BinanceKline>> FetchMissingBinanceKlinesAsync(DateTime? startTime, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var startTimeLabel = startTime.HasValue ? startTime.Value.ToString("o") : "none";
                _logger.LogInformation("Fetching Binance klines for {Symbol} starting at {StartTime} (attempt {Attempt}/{MaxAttempts})", Symbol, startTimeLabel, attempt, maxAttempts);
                return await _apiClient.GetHistoricalKlinesAsync(Symbol, "1m", ApiFetchLimit, startTime, cancellationToken);
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "Binance API fetch failed on attempt {Attempt}/{MaxAttempts}. Retrying in {DelaySeconds}s.", attempt, maxAttempts, attempt * 5);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 5), cancellationToken);
            }
        }

        _logger.LogWarning("Exceeded Binance API retry attempts for {Symbol}. No klines were retrieved.", Symbol);
        return Enumerable.Empty<BinanceKline>();
    }

    private async Task SaveKlinesAsync(IEnumerable<BinanceKline> klines, CancellationToken cancellationToken)
    {
        var entities = klines.Select(MapToEntity).ToList();
        if (entities.Count == 0)
        {
            _logger.LogInformation("No Binance klines were available to save after fetching.");
            return;
        }

        var existingOpenTimes = await _candleRepository.GetExistingOpenTimesAsync(
            Symbol,
            Source,
            IntervalMinutes,
            entities.Select(e => e.OpenTime),
            cancellationToken);

        var existingSet = existingOpenTimes.ToHashSet();
        var newEntities = entities.Where(e => !existingSet.Contains(e.OpenTime)).ToList();

        if (newEntities.Count == 0)
        {
            _logger.LogInformation("All fetched Binance klines already exist in the database; skipping persistence.");
            return;
        }

        await _candleRepository.AddRangeAsync(newEntities, cancellationToken);
        _logger.LogInformation("Saved {Count} new Binance klines to the database.", newEntities.Count);
    }

    private static CandleEntity MapToEntity(BinanceKline kline)
        => new CandleEntity
        {
            Symbol = Symbol,
            Source = Source,
            IntervalMinutes = IntervalMinutes,
            OpenTime = kline.OpenTime,
            CloseTime = kline.CloseTime,
            Open = kline.Open,
            High = kline.High,
            Low = kline.Low,
            Close = kline.Close,
            Volume = kline.Volume,
            CreatedAt = DateTime.UtcNow,
        };

    private async Task ImportCsvFolderAsync(string folderPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            _logger.LogWarning("CSV import folder path is not set. Skipping CSV import.");
            return;
        }

        if (!Directory.Exists(folderPath))
        {
            _logger.LogWarning("CSV import folder does not exist: {FolderPath}", folderPath);
            return;
        }

        var csvFiles = Directory.EnumerateFiles(folderPath, "*.csv", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in csvFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ProcessCsvFileAsync(filePath, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing CSV file {FilePath}: {Message}", filePath, ex.Message);
            }
        }
    }

    private async Task ProcessCsvFileAsync(string filePath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting import of CSV file {FilePath}", filePath);

        using var reader = new StreamReader(filePath);
        var batch = new List<CandleEntity>(BatchSize);
        var rowsProcessed = 0;
        var rowsSkipped = 0;
        var lineNumber = 0;
        bool hasFirstValidRecord = false;

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            lineNumber++;

            if (lineNumber == 1)
            {
                continue; // skip header row
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                rowsSkipped++;
                continue;
            }

            if (!TryParseCsvLine(line, out var dto, out var errorMessage))
            {
                rowsSkipped++;
                _logger.LogWarning("Malformed CSV line {LineNumber} in {FilePath}: {Error}", lineNumber, filePath, errorMessage);
                continue;
            }

            if (!hasFirstValidRecord)
            {
                hasFirstValidRecord = true;
                if (await _candleRepository.ExistsAsync(Source, IntervalMinutes, DateTimeOffset.FromUnixTimeMilliseconds(dto.OpenTimeMs).UtcDateTime, cancellationToken))
                {
                    _logger.LogInformation("CSV file appears already imported, skipping file: {FilePath}", filePath);
                    return;
                }
            }

            batch.Add(MapToEntity(dto));
            rowsProcessed++;

            if (batch.Count >= BatchSize)
            {
                await SaveBatchAsync(batch, filePath, rowsProcessed, cancellationToken);
                batch.Clear();
            }

            if (rowsProcessed > 0 && rowsProcessed % 10000 == 0)
            {
                _logger.LogInformation("Still importing {FilePath}: {RowsProcessed} rows processed", filePath, rowsProcessed);
            }
        }

        if (batch.Count > 0)
        {
            await SaveBatchAsync(batch, filePath, rowsProcessed, cancellationToken);
        }

        if (!hasFirstValidRecord)
        {
            _logger.LogWarning("CSV file contains no valid data rows: {FilePath}", filePath);
            return;
        }

        _logger.LogInformation("Finished importing CSV file {FilePath}. Rows processed: {RowsProcessed}, rows skipped: {RowsSkipped}",
            filePath,
            rowsProcessed,
            rowsSkipped);
    }

    private static bool TryParseCsvLine(string line, out BinanceCsvCandleDto dto, out string errorMessage)
    {
        dto = default!;
        errorMessage = string.Empty;

        var parts = line.Split(',');
        if (parts.Length < 7)
        {
            errorMessage = "CSV row has insufficient fields.";
            return false;
        }

        if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var openTimeMs))
        {
            errorMessage = "Invalid openTime value.";
            return false;
        }

        if (!decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var open))
        {
            errorMessage = "Invalid open value.";
            return false;
        }

        if (!decimal.TryParse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var high))
        {
            errorMessage = "Invalid high value.";
            return false;
        }

        if (!decimal.TryParse(parts[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var low))
        {
            errorMessage = "Invalid low value.";
            return false;
        }

        if (!decimal.TryParse(parts[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var close))
        {
            errorMessage = "Invalid close value.";
            return false;
        }

        if (!decimal.TryParse(parts[5], NumberStyles.Number, CultureInfo.InvariantCulture, out var volume))
        {
            errorMessage = "Invalid volume value.";
            return false;
        }

        if (!long.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var closeTimeMs))
        {
            errorMessage = "Invalid closeTime value.";
            return false;
        }

        dto = new BinanceCsvCandleDto(openTimeMs, open, high, low, close, volume, closeTimeMs);
        return true;
    }

    private static CandleEntity MapToEntity(BinanceCsvCandleDto dto)
        => new CandleEntity
        {
            Symbol = Symbol,
            Source = Source,
            IntervalMinutes = IntervalMinutes,
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(dto.OpenTimeMs).UtcDateTime,
            CloseTime = DateTimeOffset.FromUnixTimeMilliseconds(dto.CloseTimeMs).UtcDateTime,
            Open = dto.Open,
            High = dto.High,
            Low = dto.Low,
            Close = dto.Close,
            Volume = dto.Volume,
            CreatedAt = DateTime.UtcNow,
        };

    private async Task SaveBatchAsync(List<CandleEntity> batch, string filePath, int rowsProcessed, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            var existingOpenTimes = await _candleRepository.GetExistingOpenTimesAsync(
                Symbol,
                Source,
                IntervalMinutes,
                batch.Select(c => c.OpenTime),
                cancellationToken);

            var existingSet = existingOpenTimes.ToHashSet();
            var newBatch = batch.Where(c => !existingSet.Contains(c.OpenTime)).ToList();

            if (newBatch.Count == 0)
            {
                _logger.LogInformation("Skipped {BatchSize} duplicate records while importing {FilePath}. Total processed so far: {RowsProcessed}", batch.Count, filePath, rowsProcessed);
                return;
            }

            await _candleRepository.AddRangeAsync(newBatch, cancellationToken);
            _logger.LogDebug("Saved batch of {BatchSize} records for {FilePath}. Total rows processed so far: {RowsProcessed}", newBatch.Count, filePath, rowsProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save batch from {FilePath} after processing {RowsProcessed} rows: {Message}", filePath, rowsProcessed, ex.Message);
            throw;
        }
    }
}
