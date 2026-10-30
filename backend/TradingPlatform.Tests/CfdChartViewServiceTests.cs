using Microsoft.EntityFrameworkCore;
using Moq;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;
using TradingPlatform.Data.Services.Cfd;

namespace TradingPlatform.Tests;

public sealed class CfdChartViewServiceTests
{
    [Fact]
    public async Task CreateAndGetById_PreservesDrawingsAndScopesViewToOwner()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var ownerId = Guid.NewGuid();
        var drawing = new CfdChartDrawingDto(
            "drawing-1",
            "trend",
            new[]
            {
                new CfdChartDrawingPointDto(1_798_000_000, 100.25m),
                new CfdChartDrawingPointDto(1_798_003_600, 101.75m),
            },
            false);

        var created = await service.CreateAsync(ownerId, new SaveCfdChartViewRequestDto(
            "Trend setup",
            CreateConfiguration(new[] { drawing })));

        var retrieved = await service.GetByIdForUserAsync(ownerId, created.Id);
        var hiddenFromOtherUser = await service.GetByIdForUserAsync(Guid.NewGuid(), created.Id);

        Assert.NotNull(retrieved);
        var storedDrawing = Assert.Single(retrieved.Configuration.Drawings);
        Assert.Equal(drawing.Id, storedDrawing.Id);
        Assert.Equal(drawing.Type, storedDrawing.Type);
        Assert.Equal(drawing.Locked, storedDrawing.Locked);
        Assert.Equal(drawing.Points, storedDrawing.Points);
        Assert.Null(hiddenFromOtherUser);
        Assert.Single(await service.GetForUserAsync(ownerId));
        Assert.Empty(await service.GetForUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Create_RejectsDrawingWithWrongNumberOfAnchors()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var invalidDrawing = new CfdChartDrawingDto(
            "drawing-1",
            "rectangle",
            new[] { new CfdChartDrawingPointDto(1_798_000_000, 100m) },
            false);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(Guid.NewGuid(), new SaveCfdChartViewRequestDto(
                "Invalid drawing",
                CreateConfiguration(new[] { invalidDrawing }))));

        Assert.Contains("invalid chart drawing", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetForUser_LegacyConfigurationWithoutDrawingsReturnsEmptyList()
    {
        await using var dbContext = CreateDbContext();
        var ownerId = Guid.NewGuid();
        dbContext.CfdChartViews.Add(new CfdChartViewEntity
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Name = "Legacy view",
            ConfigurationJson = """
                {"version":1,"symbols":["BTCUSDT"],"highlightedSymbol":"BTCUSDT","rangeMinutes":60,"intervalMinutes":1,"chartType":"candles"}
                """,
            ConfigurationVersion = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var views = await CreateService(dbContext).GetForUserAsync(ownerId);

        Assert.Empty(Assert.Single(views).Configuration.Drawings);
    }

    private static TradingPlatformDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TradingPlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TradingPlatformDbContext(options);
    }

    private static CfdChartViewService CreateService(TradingPlatformDbContext dbContext)
    {
        var cfdService = new Mock<ICfdService>();
        cfdService
            .Setup(service => service.GetAvailableCfdInstrumentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new CfdInstrumentSummaryDto(
                    new InstrumentDto(
                        Guid.NewGuid(),
                        "BTCUSDT",
                        "Bitcoin CFD",
                        string.Empty,
                        "Cfd",
                        string.Empty,
                        "BTC",
                        "USDT",
                        "Active",
                        false,
                        Guid.NewGuid(),
                        DateTimeOffset.UtcNow),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    Array.Empty<decimal>(),
                    null),
            });
        return new CfdChartViewService(dbContext, cfdService.Object);
    }

    private static CfdChartViewConfigurationDto CreateConfiguration(
        IReadOnlyList<CfdChartDrawingDto> drawings) =>
        new(1, new[] { "BTCUSDT" }, "BTCUSDT", 60, 1, "candles")
        {
            Drawings = drawings,
        };
}
