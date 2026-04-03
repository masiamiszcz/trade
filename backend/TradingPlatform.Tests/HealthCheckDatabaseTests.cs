using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Context;

namespace TradingPlatform.Tests;

public class HealthCheckDatabaseTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public HealthCheckDatabaseTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetHealthStatusAsync_ShouldReturnHealthy_WhenDatabaseIsConnected()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var healthService = scope.ServiceProvider.GetRequiredService<IHealthService>();

        // Act
        var result = await healthService.GetHealthStatusAsync();

        // Assert
        Assert.Equal("Healthy", result.Status);
        Assert.True(result.IsReady);
        Assert.True(result.DatabaseHealthy);
        Assert.Contains("running and database is accessible", result.Message);
        Assert.True(result.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetHealthStatusAsync_ShouldReturnUnhealthy_WhenDatabaseConnectionFails()
    {
        // Arrange - create a service with invalid connection string
        var services = new ServiceCollection();
        services.AddDbContext<TradingPlatformDbContext>(options =>
            options.UseSqlServer("Server=invalid;Database=invalid;Trusted_Connection=True;"));
        services.AddScoped<IHealthService, TradingPlatform.Data.Services.HealthService>();

        using var scope = services.BuildServiceProvider().CreateScope();
        var healthService = scope.ServiceProvider.GetRequiredService<IHealthService>();

        // Act
        var result = await healthService.GetHealthStatusAsync();

        // Assert
        Assert.Equal("Unhealthy", result.Status);
        Assert.False(result.IsReady);
        Assert.False(result.DatabaseHealthy);
        Assert.Contains("Database connection failed", result.Message);
        Assert.True(result.Timestamp <= DateTime.UtcNow);
    }
}
