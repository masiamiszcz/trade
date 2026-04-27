using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradingPlatform.Api;
using TradingPlatform.Data.Context;
using System;

namespace TradingPlatform.Tests.Infrastructure;

/// <summary>
/// WebApplicationFactory for integration tests
/// Provides TestHttpClient with in-memory database
/// 
/// USAGE:
/// public class IntegrationTests
/// {
///     private readonly TestWebApplicationFactory _factory;
///     
///     public IntegrationTests()
///     {
///         _factory = new TestWebApplicationFactory();
///     }
///     
///     [Fact]
///     public async Task TestEndpoint()
///     {
///         var client = _factory.CreateClient();
///         var response = await client.GetAsync("/api/endpoint");
///     }
/// }
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public TestWebApplicationFactory(string? databaseName = null)
    {
        _databaseName = databaseName ?? $"TestDb_{Guid.NewGuid()}";
    }

    /// <summary>
    /// Configures dependency injection for tests
    /// Replaces SQL Server with InMemory database
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TradingPlatformContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Add InMemory DbContext
            services.AddDbContext<TradingPlatformContext>(
                options => options.UseInMemoryDatabase(_databaseName));

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create database schema
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TradingPlatformContext>();
                db.Database.EnsureCreated();
            }
        });
    }

    /// <summary>
    /// Gets DbContext for seeding test data
    /// </summary>
    public TradingPlatformContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TradingPlatformContext>();
    }

    /// <summary>
    /// Seeds database with standard test data
    /// </summary>
    public void SeedStandardData()
    {
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TradingPlatformContext>();
            TestDbContext.SeedStandardTestData(db);
        }
    }

    /// <summary>
    /// Clears database between tests
    /// </summary>
    public void ClearDatabase()
    {
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TradingPlatformContext>();
            TestDbContext.ClearDatabase(db);
        }
    }

    /// <summary>
    /// Resets database (clears all data)
    /// Call between independent tests
    /// </summary>
    public void ResetDatabase()
    {
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TradingPlatformContext>();
            
            // Ensure database exists
            db.Database.EnsureCreated();
            
            // Clear all data
            TestDbContext.ClearDatabase(db);
        }
    }
}

/// <summary>
/// Base class for integration tests
/// Provides factory, client, and DbContext
/// 
/// USAGE:
/// public class UserEndpointTests : IntegrationTestBase
/// {
///     [Fact]
///     public async Task GetUser_ReturnsOk()
///     {
///         var response = await Client.GetAsync("/api/users/1");
///         Assert.True(response.IsSuccessStatusCode);
///     }
/// }
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected TestWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    protected TradingPlatformContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new TestWebApplicationFactory();
        Client = Factory.CreateClient();
        DbContext = Factory.GetDbContext();
        
        // Call test-specific setup
        await SetupAsync();
    }

    public async Task DisposeAsync()
    {
        // Call test-specific cleanup
        await TeardownAsync();

        // Cleanup
        DbContext?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
    }

    /// <summary>
    /// Override to add test-specific setup
    /// Example: seed data
    /// </summary>
    protected virtual Task SetupAsync() => Task.CompletedTask;

    /// <summary>
    /// Override to add test-specific cleanup
    /// </summary>
    protected virtual Task TeardownAsync() => Task.CompletedTask;

    /// <summary>
    /// Seeds database with standard data
    /// Call in SetupAsync if needed
    /// </summary>
    protected void SeedStandardData()
    {
        Factory.SeedStandardData();
    }

    /// <summary>
    /// Clears all data from database
    /// </summary>
    protected void ClearDatabase()
    {
        Factory.ClearDatabase();
    }
}
