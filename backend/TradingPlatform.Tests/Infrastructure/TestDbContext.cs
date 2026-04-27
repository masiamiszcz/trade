using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Context;
using System;
using System.Collections.Generic;

namespace TradingPlatform.Tests.Infrastructure;

/// <summary>
/// In-memory database context for integration tests
/// USAGE: var dbContext = TestDbContext.CreateInMemoryContext();
/// 
/// Key features:
/// - Isolated per test (fresh DB instance)
/// - No external DB needed
/// - Fast execution
/// - Can be reset between tests
/// </summary>
public class TestDbContext
{
    /// <summary>
    /// Creates fresh InMemory DbContext for each test
    /// Each call creates completely isolated DB
    /// </summary>
    public static TradingPlatformContext CreateInMemoryContext(
        string? databaseName = null)
    {
        var dbName = databaseName ?? $"TestDb_{Guid.NewGuid()}";

        var options = new DbContextOptionsBuilder<TradingPlatformContext>()
            .UseInMemoryDatabase(dbName)
            .EnableSensitiveDataLogging()  // For debugging in tests
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new TradingPlatformContext(options);

        // Ensure database is created
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Creates context with pre-seeded data
    /// Useful for tests that need existing users/admins
    /// </summary>
    public static TradingPlatformContext CreateInMemoryContextWithSeeding(
        Action<TradingPlatformContext>? seedAction = null,
        string? databaseName = null)
    {
        var context = CreateInMemoryContext(databaseName);

        // Apply custom seeding if provided
        seedAction?.Invoke(context);

        // Save changes to ensure data is persisted
        context.SaveChanges();

        return context;
    }

    /// <summary>
    /// Seeds context with standard test data
    /// Creates: 3 users (active, blocked, deleted), 2 admins, sample requests
    /// </summary>
    public static void SeedStandardTestData(TradingPlatformContext context)
    {
        var activeUser = TestDataBuilder.CreateActiveUser();
        var blockedUser = TestDataBuilder.CreateBlockedUser();
        var deletedUser = TestDataBuilder.CreateDeletedUser();

        var regularAdmin = TestDataBuilder.CreateRegularAdmin();
        var superAdmin = TestDataBuilder.CreateSuperAdmin();

        context.Users.AddRange(activeUser, blockedUser, deletedUser);
        context.Admins.AddRange(regularAdmin, superAdmin);

        // Create sample approval requests
        var deleteRequest = TestDataBuilder.CreateDeleteApprovalRequest(
            activeUser.Id,
            requestedByAdminId: regularAdmin.Id);

        context.AdminRequests.Add(deleteRequest);

        context.SaveChanges();
    }

    /// <summary>
    /// Clears all data from context (for manual cleanup between tests)
    /// Useful if reusing same context instance
    /// </summary>
    public static void ClearDatabase(TradingPlatformContext context)
    {
        // Delete in dependency order
        context.AuditLogs.RemoveRange(context.AuditLogs);
        context.AdminRequests.RemoveRange(context.AdminRequests);
        context.Users.RemoveRange(context.Users);
        context.Admins.RemoveRange(context.Admins);

        context.SaveChanges();
    }

    /// <summary>
    /// Creates context for specific test scenario
    /// Example: Users with specific statuses
    /// </summary>
    public static TradingPlatformContext CreateContextForScenario(
        int activeUserCount = 1,
        int blockedUserCount = 0,
        int deletedUserCount = 0,
        string? databaseName = null)
    {
        var context = CreateInMemoryContext(databaseName);

        for (int i = 0; i < activeUserCount; i++)
        {
            context.Users.Add(TestDataBuilder.CreateActiveUser());
        }

        for (int i = 0; i < blockedUserCount; i++)
        {
            context.Users.Add(TestDataBuilder.CreateBlockedUser());
        }

        for (int i = 0; i < deletedUserCount; i++)
        {
            context.Users.Add(TestDataBuilder.CreateDeletedUser());
        }

        context.SaveChanges();
        return context;
    }
}

/// <summary>
/// Database disposer helper
/// Ensures InMemory DB is properly cleaned up after tests
/// USAGE: using (var context = TestDbContext.CreateInMemoryContext()) { ... }
/// </summary>
public class TestDbContextDisposer : IDisposable
{
    private readonly TradingPlatformContext _context;

    public TestDbContextDisposer(TradingPlatformContext context)
    {
        _context = context;
    }

    public void Dispose()
    {
        _context?.Database?.EnsureDeleted();
        _context?.Dispose();
    }
}
