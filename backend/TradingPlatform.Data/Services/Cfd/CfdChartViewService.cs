using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Services.Cfd;

public sealed class CfdChartViewService : ICfdChartViewService
{
    private const int CurrentConfigurationVersion = 1;
    private const int MaximumConfigurationJsonLength = 65_536;
    private const int MaximumDrawings = 100;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly TradingPlatformDbContext _dbContext;
    private readonly ICfdService _cfdService;

    public CfdChartViewService(TradingPlatformDbContext dbContext, ICfdService cfdService)
    {
        _dbContext = dbContext;
        _cfdService = cfdService;
    }

    public async Task<IReadOnlyList<CfdChartViewDto>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var savedViews = await _dbContext.CfdChartViews
            .AsNoTracking()
            .Where(view => view.UserId == userId)
            .OrderByDescending(view => view.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        return savedViews.Select(ToDto).ToArray();
    }

    public async Task<CfdChartViewDto?> GetByIdForUserAsync(
        Guid userId,
        Guid viewId,
        CancellationToken cancellationToken = default)
    {
        var savedView = await _dbContext.CfdChartViews
            .AsNoTracking()
            .SingleOrDefaultAsync(view => view.Id == viewId && view.UserId == userId, cancellationToken);
        return savedView is null ? null : ToDto(savedView);
    }

    public async Task<CfdChartViewDto> CreateAsync(
        Guid userId,
        SaveCfdChartViewRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 80)
        {
            throw new ArgumentException("View name must contain between 1 and 80 characters.", nameof(request));
        }

        var configuration = request.Configuration
            ?? throw new ArgumentException("View configuration is required.", nameof(request));
        ValidateConfiguration(configuration);

        var availableSymbols = (await _cfdService.GetAvailableCfdInstrumentsAsync(cancellationToken))
            .Select(summary => summary.Instrument.Symbol)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (configuration.Symbols.Any(symbol => !availableSymbols.Contains(symbol)))
        {
            throw new ArgumentException("View contains an unavailable CFD instrument.", nameof(request));
        }

        var normalizedConfiguration = configuration with
        {
            Symbols = configuration.Symbols
                .Select(symbol => symbol.Trim().ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            HighlightedSymbol = configuration.HighlightedSymbol.Trim().ToUpperInvariant(),
            Version = CurrentConfigurationVersion,
            Drawings = configuration.Drawings ?? Array.Empty<CfdChartDrawingDto>(),
        };
        var configurationJson = JsonSerializer.Serialize(normalizedConfiguration, JsonOptions);
        if (configurationJson.Length > MaximumConfigurationJsonLength)
        {
            throw new ArgumentException("View configuration is too large.", nameof(request));
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new CfdChartViewEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            ConfigurationJson = configurationJson,
            ConfigurationVersion = CurrentConfigurationVersion,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        _dbContext.CfdChartViews.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid viewId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.CfdChartViews
            .SingleOrDefaultAsync(view => view.Id == viewId && view.UserId == userId, cancellationToken);
        if (entity is null) return false;

        _dbContext.CfdChartViews.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateConfiguration(CfdChartViewConfigurationDto configuration)
    {
        if (configuration.Version != CurrentConfigurationVersion)
        {
            throw new ArgumentException("Unsupported view configuration version.", nameof(configuration));
        }
        if (configuration.Symbols is null || configuration.Symbols.Count is < 1 or > 4)
        {
            throw new ArgumentException("A view must contain between one and four instruments.", nameof(configuration));
        }

        var symbols = configuration.Symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (symbols.Length != configuration.Symbols.Count)
        {
            throw new ArgumentException("View symbols must be non-empty and unique.", nameof(configuration));
        }
        if (!symbols.Contains(configuration.HighlightedSymbol, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Highlighted symbol must be one of the selected instruments.", nameof(configuration));
        }
        if (configuration.RangeMinutes is <= 0 or > 525_600
            || configuration.IntervalMinutes is <= 0 or > 1440)
        {
            throw new ArgumentException("View range or interval is invalid.", nameof(configuration));
        }
        if (configuration.ChartType is not ("candles" or "line" or "ohlc"))
        {
            throw new ArgumentException("Chart type must be candles, line, or ohlc.", nameof(configuration));
        }

        var drawings = configuration.Drawings ?? Array.Empty<CfdChartDrawingDto>();
        if (drawings.Count > MaximumDrawings)
        {
            throw new ArgumentException($"A view can contain at most {MaximumDrawings} drawings.", nameof(configuration));
        }

        var drawingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var drawing in drawings)
        {
            if (drawing is null)
            {
                throw new ArgumentException("View contains an invalid chart drawing.", nameof(configuration));
            }
            var expectedPoints = drawing.Type is "horizontal" or "vertical" ? 1 : 2;
            if (string.IsNullOrWhiteSpace(drawing.Id)
                || drawing.Id.Length > 80
                || !drawingIds.Add(drawing.Id)
                || drawing.Type is not ("horizontal" or "vertical" or "trend" or "rectangle" or "ruler")
                || drawing.Points is null
                || drawing.Points.Count != expectedPoints
                || drawing.Points.Any(point => point is null || point.Time <= 0))
            {
                throw new ArgumentException("View contains an invalid chart drawing.", nameof(configuration));
            }
        }
    }

    private static CfdChartViewDto ToDto(CfdChartViewEntity entity)
    {
        var configuration = JsonSerializer.Deserialize<CfdChartViewConfigurationDto>(
            entity.ConfigurationJson,
            JsonOptions)
            ?? throw new InvalidOperationException($"Saved CFD view '{entity.Id}' has invalid configuration JSON.");
        if (configuration.Version != entity.ConfigurationVersion)
        {
            throw new InvalidOperationException($"Saved CFD view '{entity.Id}' has inconsistent configuration version.");
        }

        return new CfdChartViewDto(
            entity.Id,
            entity.Name,
            configuration,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
    }
}
