using Moq;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Services;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace TradingPlatform.Tests.Infrastructure;

/// <summary>
/// Base fixture for all tests
/// Provides common setup: DbContext, Repositories, Services, Mocks
/// 
/// USAGE: 
/// public class MyTest : TestFixture
/// {
///     [Fact]
///     public async Task TestSomething()
///     {
///         var user = await DbContext.Users.FirstOrDefaultAsync();
///     }
/// }
/// </summary>
public abstract class TestFixture : IDisposable
{
    // ========== DATABASE ==========
    protected TradingPlatformContext DbContext { get; private set; }

    // ========== REAL REPOSITORIES ==========
    protected SqlUserRepository UserRepository { get; private set; }
    protected SqlAdminRequestRepository AdminRequestRepository { get; private set; }
    protected SqlAuditLogRepository AuditLogRepository { get; private set; }
    protected SqlAdminAuthRepository AdminAuthRepository { get; private set; }

    // ========== MOCKS ==========
    protected Mock<ILogger<UserApprovalHandler>> MockUserHandlerLogger { get; private set; }
    protected Mock<ILogger<ApprovalService>> MockApprovalServiceLogger { get; private set; }
    protected Mock<ILogger<AdminUsersService>> MockAdminUsersServiceLogger { get; private set; }
    protected Mock<ILogger<UserAuthService>> MockUserAuthServiceLogger { get; private set; }

    // ========== SERVICES (REAL) ==========
    protected AdminUsersService AdminUsersService { get; private set; }
    protected UserApprovalHandler UserApprovalHandler { get; private set; }
    protected ApprovalService ApprovalService { get; private set; }

    public TestFixture()
    {
        // Initialize database
        DbContext = TestDbContext.CreateInMemoryContext($"TestDb_{Guid.NewGuid()}");

        // Initialize real repositories using DbContext
        UserRepository = new SqlUserRepository(DbContext);
        AdminRequestRepository = new SqlAdminRequestRepository(DbContext);
        AuditLogRepository = new SqlAuditLogRepository(DbContext);
        AdminAuthRepository = new SqlAdminAuthRepository(DbContext);

        // Initialize mocks for loggers
        MockUserHandlerLogger = new Mock<ILogger<UserApprovalHandler>>();
        MockApprovalServiceLogger = new Mock<ILogger<ApprovalService>>();
        MockAdminUsersServiceLogger = new Mock<ILogger<AdminUsersService>>();
        MockUserAuthServiceLogger = new Mock<ILogger<UserAuthService>>();

        // Initialize services with real repositories
        UserApprovalHandler = new UserApprovalHandler(
            UserRepository,
            AuditLogRepository,
            MockUserHandlerLogger.Object);

        AdminUsersService = new AdminUsersService(
            UserRepository,
            Mock.Of<IApprovalService>(),
            AuditLogRepository,
            MockAdminUsersServiceLogger.Object);
    }

    /// <summary>
    /// Seeds database with standard test data
    /// Call in test setup if needed
    /// </summary>
    protected void SeedStandardData()
    {
        TestDbContext.SeedStandardTestData(DbContext);
    }

    /// <summary>
    /// Clears all data from database
    /// Call in test cleanup if reusing context
    /// </summary>
    protected void ClearDatabase()
    {
        TestDbContext.ClearDatabase(DbContext);
    }

    /// <summary>
    /// Creates fresh context for test isolation
    /// Useful if test modifies shared state
    /// </summary>
    protected void ResetDatabase()
    {
        Dispose();
        DbContext = TestDbContext.CreateInMemoryContext($"TestDb_{Guid.NewGuid()}");

        // Re-initialize repositories
        UserRepository = new SqlUserRepository(DbContext);
        AdminRequestRepository = new SqlAdminRequestRepository(DbContext);
        AuditLogRepository = new SqlAuditLogRepository(DbContext);
        AdminAuthRepository = new SqlAdminAuthRepository(DbContext);

        UserApprovalHandler = new UserApprovalHandler(
            UserRepository,
            AuditLogRepository,
            MockUserHandlerLogger.Object);
    }

    /// <summary>
    /// Saves changes to DbContext
    /// Useful for multi-step tests
    /// </summary>
    protected async Task SaveAsync()
    {
        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Gets user from database by ID
    /// Ensures fresh read (not from context cache)
    /// </summary>
    protected async Task<Core.Models.User?> GetUserAsync(Guid userId)
    {
        return await UserRepository.GetUserByIdIncludingDeletedAsync(userId);
    }

    /// <summary>
    /// Gets admin request from database
    /// </summary>
    protected async Task<Core.Models.AdminRequest?> GetAdminRequestAsync(Guid requestId)
    {
        return await AdminRequestRepository.GetByIdAsync(requestId);
    }

    /// <summary>
    /// Gets audit logs for specific admin
    /// Useful for verifying audit trail
    /// </summary>
    protected async Task<System.Collections.Generic.List<Core.Models.AuditLog>> GetAuditLogsAsync(Guid adminId)
    {
        return await AuditLogRepository.GetByAdminIdAsync(adminId);
    }

    // ========== DISPOSAL ==========
    public virtual void Dispose()
    {
        DbContext?.Dispose();
    }
}

/// <summary>
/// Fixture with seeded standard data
/// Extends TestFixture with pre-populated database
/// 
/// USAGE:
/// public class MyTest : TestFixtureWithData
/// {
///     [Fact]
///     public async Task TestWithExistingUsers()
///     {
///         // Database already has 3 users, 2 admins
///     }
/// }
/// </summary>
public abstract class TestFixtureWithData : TestFixture
{
    public TestFixtureWithData() : base()
    {
        SeedStandardData();
    }
}
