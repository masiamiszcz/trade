# 🏗️ TEST INFRASTRUCTURE GUIDE

## Architecture Overview

```
TradingPlatform.Tests/
├── /Infrastructure/          ← Core test infrastructure
│   ├── TestDbContext.cs      ← InMemory DB factory
│   ├── TestFixture.cs        ← Base fixture with repositories
│   ├── TestWebApplicationFactory.cs ← Integration test factory
│   └── AuthenticationMockExtensions.cs ← Mock auth helpers
├── /Helpers/
│   ├── TestDataBuilder.cs    ← Factory for test entities
│   └── AssertionHelpers.cs   ← Assertion helpers
└── /[Test Files]
    ├── /Unit/Users/
    ├── /Unit/Approval/
    ├── /Unit/Auth/
    └── /Integration/
```

---

## 1️⃣ TEST DATA BUILDER

**Purpose:** Factory pattern for consistent test data

### Usage Examples

#### Basic User Creation
```csharp
var user = TestDataBuilder.CreateUser();
var user = TestDataBuilder.CreateActiveUser();
var user = TestDataBuilder.CreateBlockedUser();
var user = TestDataBuilder.CreateDeletedUser();
```

#### Custom User
```csharp
var user = TestDataBuilder.CreateUser(
    id: myUserId,
    username: "custom_user",
    status: UserStatus.Blocked,
    role: UserRole.Admin,
    blockedUntilUtc: DateTimeOffset.UtcNow.AddDays(7),
    blockReason: "Custom reason"
);
```

#### Admin Creation
```csharp
var admin = TestDataBuilder.CreateAdmin();
var admin = TestDataBuilder.CreateRegularAdmin();
var admin = TestDataBuilder.CreateSuperAdmin();
```

#### Approval Requests
```csharp
var deleteRequest = TestDataBuilder.CreateDeleteApprovalRequest(userId);
var restoreRequest = TestDataBuilder.CreateRestoreApprovalRequest(userId);
var customRequest = TestDataBuilder.CreateApprovalRequest(
    "Instrument",
    instrumentId,
    AdminRequestActionType.Create
);
```

#### Audit Logs
```csharp
var log = TestDataBuilder.CreateAuditLog(
    "USER_DELETE",
    adminId,
    "User deleted by admin"
);
```

### Benefits
✅ Avoids copy-paste of entity construction
✅ Ensures consistent test data
✅ Easy to add defaults or variations
✅ Single place to update entity creation logic

---

## 2️⃣ TEST DATABASE CONTEXT

**Purpose:** InMemory database isolated per test

### Usage Examples

#### Fresh Database Per Test
```csharp
var dbContext = TestDbContext.CreateInMemoryContext();
// Fresh isolated DB
```

#### Database with Pre-seeded Data
```csharp
var dbContext = TestDbContext.CreateInMemoryContextWithSeeding(
    ctx => {
        ctx.Users.Add(TestDataBuilder.CreateUser());
        ctx.SaveChanges();
    }
);
```

#### Standard Seed Data (3 users, 2 admins)
```csharp
var dbContext = TestDbContext.CreateInMemoryContext();
TestDbContext.SeedStandardTestData(dbContext);
// Now has: 1 active, 1 blocked, 1 deleted user + 2 admins
```

#### Scenario-Specific Context
```csharp
var dbContext = TestDbContext.CreateContextForScenario(
    activeUserCount: 5,
    blockedUserCount: 2,
    deletedUserCount: 1
);
```

#### Cleanup
```csharp
TestDbContext.ClearDatabase(dbContext);
```

### Benefits
✅ No external database dependency
✅ Each test gets fresh DB (no pollution)
✅ Fast execution
✅ Easy cleanup between tests

---

## 3️⃣ TEST FIXTURE BASE CLASS

**Purpose:** Common setup for unit/integration tests

### Usage

#### Simple Test Class
```csharp
public class UserServiceTests : TestFixture
{
    [Fact]
    public async Task DeleteUser_ShouldRemoveFromDb()
    {
        // Arrange
        var user = TestDataBuilder.CreateActiveUser();
        await DbContext.Users.AddAsync(user);
        await SaveAsync();

        // Act
        await UserRepository.DeleteUserAsync(user.Id, "Test delete", adminId);
        await SaveAsync();

        // Assert
        var deleted = await GetUserAsync(user.Id);
        AssertionHelpers.AssertUserIsDeleted(deleted);
    }
}
```

#### With Pre-seeded Data
```csharp
public class ApprovalServiceTests : TestFixtureWithData
{
    // Database already has 3 users, 2 admins
    // Inherit from TestFixtureWithData instead of TestFixture
}
```

### Available Properties

```csharp
DbContext                      // TradingPlatformContext
UserRepository                 // Real SqlUserRepository
AdminRequestRepository         // Real SqlAdminRequestRepository  
AuditLogRepository            // Real SqlAuditLogRepository
AdminAuthRepository           // Real SqlAdminAuthRepository
AdminUsersService             // Real AdminUsersService
UserApprovalHandler           // Real UserApprovalHandler
```

### Available Methods

```csharp
SeedStandardData()             // Seeds 3 users, 2 admins
ClearDatabase()                // Wipes all data
ResetDatabase()                // Fresh context + clear
SaveAsync()                    // await DbContext.SaveChangesAsync()
GetUserAsync(userId)           // Loads user from DB
GetAdminRequestAsync(requestId) // Loads request from DB
GetAuditLogsAsync(adminId)     // Gets audit logs for admin
```

---

## 4️⃣ AUTHENTICATION MOCKS

**Purpose:** Quick authentication context creation for tests

### Usage Examples

#### User Claims
```csharp
var claims = AuthenticationMockExtensions.CreateUserClaims(userId);
var principal = AuthenticationMockExtensions.CreateUserPrincipal(userId);
```

#### Admin Claims
```csharp
var claims = AuthenticationMockExtensions.CreateAdminClaims(adminId);
var principal = AuthenticationMockExtensions.CreateAdminPrincipal(adminId);
var principal = AuthenticationMockExtensions.CreateSuperAdminPrincipal(adminId);
```

#### Extract from Claims
```csharp
var userId = AuthenticationMockExtensions.ExtractUserId(principal);
var adminId = AuthenticationMockExtensions.ExtractAdminId(principal);
var role = AuthenticationMockExtensions.ExtractRole(principal);
```

#### Mock HttpContext
```csharp
var mockContext = AuthenticationMockExtensions.CreateMockHttpContextWithUser(userId);
var mockContext = AuthenticationMockExtensions.CreateMockHttpContextWithAdmin(adminId);
```

### Benefits
✅ Matches JwtTokenGenerator format
✅ Avoid JWT generation in tests
✅ Quick claim construction
✅ Used for middleware/controller testing

---

## 5️⃣ INTEGRATION TEST FACTORY

**Purpose:** Full application factory with InMemory DB for integration tests

### Usage

#### Simple Integration Test
```csharp
public class UserEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task GetUsers_ReturnsOk()
    {
        // Client, DbContext, Factory already initialized
        var response = await Client.GetAsync("/api/admin/users");
        Assert.True(response.IsSuccessStatusCode);
    }
}
```

#### With Custom Setup
```csharp
public class ApprovalEndpointTests : IntegrationTestBase
{
    protected override async Task SetupAsync()
    {
        // Seed custom data
        SeedStandardData();
    }

    [Fact]
    public async Task ApproveRequest_ExecutesHandler()
    {
        // DbContext has pre-seeded data
    }
}
```

### Available in IntegrationTestBase

```csharp
Factory           // TestWebApplicationFactory
Client            // HttpClient
DbContext         // TradingPlatformContext
SeedStandardData() // Seed 3 users, 2 admins
ClearDatabase()   // Wipe all data
```

---

## 6️⃣ ASSERTION HELPERS

**Purpose:** Common assertions to avoid repetition

### User Status Assertions
```csharp
AssertionHelpers.AssertUserIsActive(user);
AssertionHelpers.AssertUserIsDeleted(user);
AssertionHelpers.AssertUserIsBlocked(user, blockedUntil: expectedDate);
AssertionHelpers.AssertUserIsSuspended(user);
AssertionHelpers.AssertUserIsPending(user);
```

### Request Assertions
```csharp
AssertionHelpers.AssertRequestIsPending(request);
AssertionHelpers.AssertRequestIsApproved(request);
AssertionHelpers.AssertRequestIsRejected(request);
AssertionHelpers.AssertRequestAction(request, AdminRequestActionType.Delete);
AssertionHelpers.AssertRequestEntity(request, "User", userId);
```

### Audit Assertions
```csharp
AssertionHelpers.AssertAuditLogExists(logs, "USER_DELETE", adminId);
AssertionHelpers.AssertAuditLogContains(log, "USER_DELETE", "userId");
```

### Role Assertions
```csharp
AssertionHelpers.AssertUserRole(user, UserRole.Admin);
AssertionHelpers.AssertIsSuperAdmin(admin);
AssertionHelpers.AssertIsRegularAdmin(admin);
```

### Collection Assertions
```csharp
AssertionHelpers.AssertUserCount(users, 5);
AssertionHelpers.AssertContainsUser(users, userId);
AssertionHelpers.AssertNoDeletedUsers(users);
AssertionHelpers.AssertAllActive(users);
```

### Timestamp Assertions
```csharp
AssertionHelpers.AssertRecentTimestamp(user.CreatedAtUtc);
AssertionHelpers.AssertFutureTimestamp(user.BlockedUntilUtc);
```

---

## 🧪 PRACTICAL TEST EXAMPLE

```csharp
public class UserDeleteFlowTests : TestFixture
{
    [Fact]
    public async Task Delete_Request_Then_Approve_Should_MarkUserDeleted()
    {
        // Arrange
        var user = TestDataBuilder.CreateActiveUser();
        var admin = TestDataBuilder.CreateRegularAdmin();
        
        await DbContext.Users.AddAsync(user);
        await DbContext.Admins.AddAsync(admin);
        await SaveAsync();

        var deleteRequest = TestDataBuilder.CreateDeleteApprovalRequest(
            user.Id,
            requestedByAdminId: admin.Id);

        await DbContext.AdminRequests.AddAsync(deleteRequest);
        await SaveAsync();

        // Act
        var result = await UserApprovalHandler.ExecuteAsync(
            deleteRequest,
            admin.Id,
            CancellationToken.None);

        await SaveAsync();

        // Assert
        var deleted = await GetUserAsync(user.Id);
        AssertionHelpers.AssertUserIsDeleted(deleted);

        var logs = await GetAuditLogsAsync(admin.Id);
        AssertionHelpers.AssertAuditLogExists(logs, "USER_DELETE", admin.Id);
    }
}
```

---

## ⚡ TEST PATTERNS

### Pattern 1: Isolation with Fresh DB
```csharp
// Each test gets fresh DB - no pollution
// Use: Default TestFixture
```

### Pattern 2: Seeded Data
```csharp
// Test with pre-populated DB
// Use: TestFixtureWithData
```

### Pattern 3: Scenario-Specific Setup
```csharp
protected override Task SetupAsync()
{
    Factory.SeedStandardData();
    // Add custom setup
}
```

### Pattern 4: Unit Test with Mocks
```csharp
// Test one class in isolation
// Use: Plain Mock<> + TestDataBuilder
```

### Pattern 5: Integration Test with Real Services
```csharp
// Test full HTTP flow
// Use: IntegrationTestBase
```

---

## 🚀 EXECUTION

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter FullyQualifiedName~UserDeleteFlowTests
```

### Run With Coverage
```bash
dotnet test /p:CollectCoverage=true
```

### Run Specific Test
```bash
dotnet test --filter Name=Delete_Request_Then_Approve_Should_MarkUserDeleted
```

---

## ⚠️ COMMON PITFALLS

### ❌ Forgetting SaveAsync()
```csharp
// WRONG - changes not persisted
await DbContext.Users.AddAsync(user);
var loaded = await GetUserAsync(user.Id); // Returns null!

// RIGHT
await DbContext.Users.AddAsync(user);
await SaveAsync();
var loaded = await GetUserAsync(user.Id); // Works!
```

### ❌ Mixing DbContext Instances
```csharp
// WRONG - different contexts, changes not visible
var user = TestDataBuilder.CreateUser();
var anotherContext = TestDbContext.CreateInMemoryContext();
await anotherContext.Users.AddAsync(user); // Different DB!

// RIGHT - use fixture's DbContext
await DbContext.Users.AddAsync(user);
```

### ❌ Not Isolating Tests
```csharp
// WRONG - test 2 depends on test 1 data
[Fact] public void Test1() { /* seed data */ }
[Fact] public void Test2() { /* depends on test 1 */ } // Fails if test 1 doesn't run first

// RIGHT - each test fully isolated
// Use TestFixture (fresh DB per test)
```

### ❌ Forgetting to Reset Counters
```csharp
// WRONG - counter keeps incrementing across tests
[Fact] public void Test1() { var u = TestDataBuilder.CreateUser(); } // username: testuser1
[Fact] public void Test2() { var u = TestDataBuilder.CreateUser(); } // username: testuser2 (oops, expected testuser1)

// RIGHT
public override void Dispose()
{
    TestDataBuilder.ResetCounters();
    base.Dispose();
}
```

---

## ✅ BEST PRACTICES

1. **Use TestFixture for unit tests** - Real services, isolated DB
2. **Use TestFixtureWithData for integration** - Pre-seeded data
3. **Use IntegrationTestBase for HTTP tests** - Full application factory
4. **Always SaveAsync() after writes** - Explicit persistence
5. **Use TestDataBuilder** - Avoid entity construction chaos
6. **Use AssertionHelpers** - Clear, readable assertions
7. **One assertion type per test** - Either behavioral OR state
8. **Test isolation first** - Then integration
9. **Clear naming** - Test names describe exact scenario
10. **Reset between tests** - Never rely on test order

---

This infrastructure is **production-grade** for test stabilization.
