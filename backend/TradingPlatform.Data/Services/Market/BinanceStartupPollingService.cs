using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingPlatform.Data.Entities;
using TradingPlatform.Data.External;
using TradingPlatform.Data.Repositories;

namespace TradingPlatform.Data.Services.Market;

public sealed class BinanceStartupPollingService : BackgroundService
{
    private const int BatchSize = 100000;
    private const int IntervalMinutes = 1;
    private const int ApiFetchLimit = 1000;
    private const int DefaultHistoricalDays = 7;
    private const string Source = "binance";

    private readonly string _csvFolderPath;
    private readonly IReadOnlyList<string> _configuredSymbols;
    private readonly IBinanceApiClient _apiClient;
    private readonly ICandleRepository _candleRepository;
    private readonly ICandleBulkInserter _candleBulkInserter;
    private readonly IStartupLoadCoordinator _startupLoadCoordinator;
    private readonly ILogger<BinanceStartupPollingService> _logger;

    public BinanceStartupPollingService(
        IBinanceApiClient apiClient,
        ICandleRepository candleRepository,
        ICandleBulkInserter candleBulkInserter,
        IStartupLoadCoordinator startupLoadCoordinator,
        ILogger<BinanceStartupPollingService> logger,
        IConfiguration configuration)
    {
        _apiClient = apiClient;
        _candleRepository = candleRepository;
        _candleBulkInserter = candleBulkInserter;
        _startupLoadCoordinator = startupLoadCoordinator;
        _logger = logger;

        var configuredPath = configuration["BinanceCsv:FolderPath"];
        _csvFolderPath = !string.IsNullOrWhiteSpace(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, "CSV");

        var configuredSymbols = configuration.GetSection("BinanceCsv:Symbols").Get<string[]>();
        _configuredSymbols = configuredSymbols != null && configuredSymbols.Any()
            ? configuredSymbols
            : new string[0];

        _logger.LogInformation("Binance CSV import folder configured as: {CsvFolderPath}", _csvFolderPath);
        if (_configuredSymbols.Count > 0)
        {
            _logger.LogInformation("Binance CSV symbols configured: {Symbols}", string.Join(", ", _configuredSymbols));
        }
    }

    private IReadOnlyList<string> GetSymbolsToProcess()
    {
        if (_configuredSymbols.Count > 0)
        {
            return _configuredSymbols;
        }

        if (!Directory.Exists(_csvFolderPath))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateDirectories(_csvFolderPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name!, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string GetSymbolFolderPath(string baseFolderPath, string symbol)
        => Path.Combine(baseFolderPath, symbol);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Startup Binance polling service initialising...");
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        try
        {
            var symbols = GetSymbolsToProcess();
            if (!symbols.Any())
            {
                _logger.LogWarning("No Binance symbol folders configured or discovered under {CsvFolderPath}. Skipping startup load.", _csvFolderPath);
                _startupLoadCoordinator.MarkReady();
                return;
            }

            foreach (var symbol in symbols)
            {
                await ProcessSymbolAsync(symbol, stoppingToken);
            }

            _startupLoadCoordinator.MarkReady();
            _logger.LogInformation("Startup data load completed for {SymbolCount} Binance symbol(s). Binance WebSocket can begin.", symbols.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Binance startup data load failed: {Message}", ex.Message);
            _startupLoadCoordinator.MarkFailed(ex);
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessSymbolAsync(string symbol, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting startup load for Binance symbol {Symbol}.", symbol);

        var symbolCsvFolderPath = GetSymbolFolderPath(_csvFolderPath, symbol);
        var lastTimestamp = await _candleRepository.GetLastCandleTimestampAsync(symbol, Source, IntervalMinutes, cancellationToken);
        var latestCsvInfo = await GetLatestCsvFileLastOpenTimeAsync(symbolCsvFolderPath, cancellationToken);

        if (!lastTimestamp.HasValue)
        {
            _logger.LogInformation("No existing candle data found for {Symbol}. Importing CSV history first.", symbol);
            await ImportCsvFolderAsync(symbolCsvFolderPath, symbol, cancellationToken);
        }
        else if (latestCsvInfo is not null)
        {
            var latestCsvOpenTime = latestCsvInfo.OpenTime;
            var latestCsvOpenTimeExists = await _candleRepository.GetExistingOpenTimesAsync(
                symbol,
                Source,
                IntervalMinutes,
                new[] { latestCsvOpenTime },
                cancellationToken);

            if (!latestCsvOpenTimeExists.Any())
            {
                _logger.LogInformation("Latest CSV file '{CsvFile}' for {Symbol} is not yet imported. Importing CSV files.", latestCsvInfo.FilePath, symbol);
                await ImportCsvFolderAsync(symbolCsvFolderPath, symbol, cancellationToken);
            }
            else
            {
                _logger.LogInformation("Latest CSV file '{CsvFile}' for {Symbol} already imported. Skipping CSV import.", latestCsvInfo.FilePath, symbol);
            }
        }
        else
        {
            _logger.LogInformation("No CSV files found to import for {Symbol}. Skipping CSV import.", symbol);
        }

        var dbLastTimestamp = await _candleRepository.GetLastCandleTimestampAsync(symbol, Source, IntervalMinutes, cancellationToken);
        DateTime startTime;
        var currentMinuteUtc = TruncateToMinute(DateTime.UtcNow);

        if (!dbLastTimestamp.HasValue)
        {
            startTime = DateTime.UtcNow.AddDays(-DefaultHistoricalDays);
            _logger.LogInformation("No existing candle data found after CSV import for {Symbol}. Fetching the last {Days} days from {StartTime:O}.", symbol, DefaultHistoricalDays, startTime);
        }
        else if (dbLastTimestamp.Value >= currentMinuteUtc)
        {
            _logger.LogInformation("Database already contains the latest candle for {Symbol} at {LastTimestamp:O}. Skipping Binance API sync.", symbol, dbLastTimestamp.Value);
            startTime = DateTime.MaxValue;
        }
        else
        {
            startTime = await DetermineBinanceApiStartTimeAsync(dbLastTimestamp.Value, latestCsvInfo?.OpenTime, symbol, cancellationToken);
            _logger.LogInformation("Last candle in DB for {Symbol} is {LastTimestamp:O}. Fetching missing candles from {StartTime:O}.", symbol, dbLastTimestamp.Value, startTime);
        }

        var totalFetched = startTime == DateTime.MaxValue
            ? 0
            : await FetchAndSaveMissingBinanceKlinesAsync(startTime, symbol, cancellationToken);

        if (totalFetched > 0)
        {
            _logger.LogInformation("Binance incremental sync for {Symbol} complete. Retrieved and persisted {Count} new klines.", symbol, totalFetched);
        }
    }

    private async Task<CsvFileLastOpenTime?> GetLatestCsvFileLastOpenTimeAsync(string folderPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return null;
        }

        var csvFiles = Directory.EnumerateFiles(folderPath, "*.csv", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .ThenByDescending(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var fileInfo in csvFiles)
        {
            var lastOpenTime = await ReadLastValidCsvOpenTimeAsync(fileInfo.FullName, cancellationToken);
            if (lastOpenTime.HasValue)
            {
                return new CsvFileLastOpenTime(fileInfo.FullName, lastOpenTime.Value);
            }
        }

        return null;
    }

    private static async Task<DateTime?> ReadLastValidCsvOpenTimeAsync(string filePath, CancellationToken cancellationToken)
    {
        string? lastLine = null;
        using var reader = new StreamReader(filePath);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(line))
            {
                lastLine = line;
            }
        }

        if (string.IsNullOrWhiteSpace(lastLine) || !TryParseCsvLine(lastLine, out var dto, out _))
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(dto.OpenTimeMs).UtcDateTime;
    }

    private static DateTime TruncateToMinute(DateTime dateTime)
        => new(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, DateTimeKind.Utc);

    private async Task<DateTime> DetermineBinanceApiStartTimeAsync(DateTime dbLastTimestamp, DateTime? latestCsvOpenTime, string symbol, CancellationToken cancellationToken)
    {
        if (latestCsvOpenTime.HasValue)
        {
            var expectedNext = latestCsvOpenTime.Value.AddMinutes(IntervalMinutes);
            var nextExists = await _candleRepository.GetExistingOpenTimesAsync(
                symbol,
                Source,
                IntervalMinutes,
                new[] { expectedNext },
                cancellationToken);

            if (!nextExists.Any())
            {
                _logger.LogInformation("Next candle after latest CSV ({ExpectedNext:O}) is missing from DB for {Symbol}. Fetching from this missing CSV boundary.", expectedNext, symbol);
                return expectedNext;
            }

            _logger.LogInformation("Next candle after latest CSV ({ExpectedNext:O}) exists in DB for {Symbol}. Confirming API sync from latest DB record.", expectedNext, symbol);
        }

        return dbLastTimestamp.AddMinutes(IntervalMinutes);
    }

    private sealed record CsvFileLastOpenTime(string FilePath, DateTime OpenTime);

    private async Task<int> FetchAndSaveMissingBinanceKlinesAsync(DateTime? startTime, string symbol, CancellationToken cancellationToken)
    {
        var totalSaved = 0;
        var nextStart = startTime;

        while (!cancellationToken.IsCancellationRequested)
        {
            var klines = await FetchMissingBinanceKlinesPageAsync(nextStart, symbol, cancellationToken);
            if (klines == null || !klines.Any())
            {
                break;
            }

            await SaveKlinesAsync(klines, symbol, cancellationToken);
            totalSaved += klines.Count();

            if (klines.Count() < ApiFetchLimit)
            {
                break;
            }

            nextStart = klines.Last().OpenTime.AddMinutes(IntervalMinutes);
        }

        return totalSaved;
    }

    private async Task<IEnumerable<BinanceKline>> FetchMissingBinanceKlinesPageAsync(DateTime? startTime, string symbol, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var startTimeLabel = startTime.HasValue ? startTime.Value.ToString("o") : "none";
                _logger.LogInformation("Fetching Binance klines for {Symbol} starting at {StartTime} (attempt {Attempt}/{MaxAttempts})", symbol, startTimeLabel, attempt, maxAttempts);
                return await _apiClient.GetHistoricalKlinesAsync(symbol, "1m", ApiFetchLimit, startTime, cancellationToken);
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "Binance API fetch failed for {Symbol} on attempt {Attempt}/{MaxAttempts}. Retrying in {DelaySeconds}s.", symbol, attempt, maxAttempts, attempt * 5);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 5), cancellationToken);
            }
        }

        _logger.LogWarning("Exceeded Binance API retry attempts for {Symbol}. No klines were retrieved.", symbol);
        return Enumerable.Empty<BinanceKline>();
    }

    private async Task SaveKlinesAsync(IEnumerable<BinanceKline> klines, string symbol, CancellationToken cancellationToken)
    {
        var entities = klines.Select(k => MapToEntity(k, symbol)).ToList();
        if (entities.Count == 0)
        {
            _logger.LogInformation("No Binance klines were available to save after fetching for {Symbol}.", symbol);
            return;
        }

        await _candleBulkInserter.BulkInsertAsync(entities, cancellationToken);
        _logger.LogInformation("Bulk inserted {Count} Binance klines into the database for {Symbol}.", entities.Count, symbol);
    }

    private static CandleEntity MapToEntity(BinanceKline kline, string symbol)
        => new CandleEntity
        {
            Symbol = symbol,
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

    private async Task ImportCsvFolderAsync(string folderPath, string symbol, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            _logger.LogWarning("CSV import folder path is not set for {Symbol}. Skipping CSV import.", symbol);
            return;
        }

        if (!Directory.Exists(folderPath))
        {
            _logger.LogWarning("CSV import folder does not exist for {Symbol}: {FolderPath}", symbol, folderPath);
            return;
        }

        var csvFiles = Directory.EnumerateFiles(folderPath, "*.csv", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderBy(file => file.CreationTimeUtc)
            .ThenBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .Select(file => file.FullName);

        foreach (var filePath in csvFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ProcessCsvFileAsync(filePath, symbol, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing CSV file {FilePath} for {Symbol}: {Message}", filePath, symbol, ex.Message);
            }
        }
    }

    private async Task ProcessCsvFileAsync(string filePath, string symbol, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting import of CSV file {FilePath} for {Symbol} with target batch size {BatchSize}", filePath, symbol, BatchSize);

        using var reader = new StreamReader(filePath);
        var batch = new List<CandleEntity>(BatchSize);
        var rowsProcessed = 0;
        var rowsSkipped = 0;
        var lineNumber = 0;

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

            try
            {
                batch.Add(MapToEntity(dto, symbol));
                rowsProcessed++;
            }
            catch (Exception ex)
            {
                rowsSkipped++;
                _logger.LogWarning(ex, "Skipping CSV line {LineNumber} in {FilePath} for {Symbol} due to conversion error: {Message}", lineNumber, filePath, symbol, ex.Message);
                continue;
            }

            if (batch.Count >= BatchSize)
            {
                await SaveBatchAsync(batch, filePath, rowsProcessed, symbol, cancellationToken);
                batch.Clear();
            }

            if (rowsProcessed > 0 && rowsProcessed % BatchSize == 0)
            {
                _logger.LogInformation("Reading CSV file {FilePath}: {RowsProcessed} rows processed", filePath, rowsProcessed);
            }
        }

        if (batch.Count > 0)
        {
            await SaveBatchAsync(batch, filePath, rowsProcessed, symbol, cancellationToken);
        }

        if (rowsProcessed == 0)
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

        if (!TryParseCsvTimestamp(parts[0], out var openTimeMs, out errorMessage))
        {
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

        if (!TryParseCsvTimestamp(parts[6], out var closeTimeMs, out errorMessage))
        {
            return false;
        }

        dto = new BinanceCsvCandleDto(openTimeMs, open, high, low, close, volume, closeTimeMs);
        return true;
    }

    private static bool TryParseCsvTimestamp(string rawValue, out long normalizedMilliseconds, out string errorMessage)
    {
        normalizedMilliseconds = 0;
        errorMessage = string.Empty;

        if (!long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rawTimestamp))
        {
            errorMessage = "Invalid timestamp value.";
            return false;
        }

        if (!TryNormalizeUnixTimestamp(rawTimestamp, out normalizedMilliseconds))
        {
            errorMessage = "Timestamp value is outside valid Unix epoch range.";
            return false;
        }

        return true;
    }

    private static bool TryNormalizeUnixTimestamp(long unixTimeValue, out long normalizedMilliseconds)
    {
        const long minMs = -62135596800000L;
        const long maxMs = 253402300799999L;

        normalizedMilliseconds = unixTimeValue;
        if (normalizedMilliseconds >= minMs && normalizedMilliseconds <= maxMs)
        {
            return true;
        }

        if (Math.Abs(unixTimeValue) < 1_000_000_000_000L)
        {
            var candidateMs = unixTimeValue * 1000;
            if (candidateMs >= minMs && candidateMs <= maxMs)
            {
                normalizedMilliseconds = candidateMs;
                return true;
            }
        }

        var candidateFromMicroseconds = unixTimeValue / 1_000;
        if (candidateFromMicroseconds >= minMs && candidateFromMicroseconds <= maxMs)
        {
            normalizedMilliseconds = candidateFromMicroseconds;
            return true;
        }

        var candidateFromNanoseconds = unixTimeValue / 1_000_000;
        if (candidateFromNanoseconds >= minMs && candidateFromNanoseconds <= maxMs)
        {
            normalizedMilliseconds = candidateFromNanoseconds;
            return true;
        }

        return false;
    }

    private static CandleEntity MapToEntity(BinanceCsvCandleDto dto, string symbol)
        => new CandleEntity
        {
            Symbol = symbol,
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

    private async Task SaveBatchAsync(List<CandleEntity> batch, string filePath, int rowsProcessed, string symbol, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return;
        }

        var openTimes = batch
            .Select(c => c.OpenTime)
            .Distinct()
            .ToList();

        var existingOpenTimes = await _candleRepository.GetExistingOpenTimesAsync(
            symbol,
            Source,
            IntervalMinutes,
            openTimes,
            cancellationToken);

        if (existingOpenTimes.Any())
        {
            var existingSet = existingOpenTimes.ToHashSet();
            var deduplicatedBatch = batch.Where(c => !existingSet.Contains(c.OpenTime)).ToList();
            var skippedCount = batch.Count - deduplicatedBatch.Count;

            if (skippedCount > 0)
            {
                _logger.LogInformation("Skipping {SkippedCount} duplicate CSV records from {FilePath}.", skippedCount, filePath);
            }

            batch = deduplicatedBatch;
        }

        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            await _candleBulkInserter.BulkInsertAsync(batch, cancellationToken);
            _logger.LogInformation("Bulk inserted {BatchCount} records for {FilePath}. Total rows processed so far: {RowsProcessed}", batch.Count, filePath, rowsProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk insert batch from {FilePath} after processing {RowsProcessed} rows: {Message}", filePath, rowsProcessed, ex.Message);
            throw;
        }
    }
}
