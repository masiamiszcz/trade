using System;
using System.Collections.Generic;
using System.Security.Claims;
using Moq;

namespace TradingPlatform.Tests.Infrastructure;

/// <summary>
/// Helper for creating mock authentication context
/// USAGE: var claims = AuthenticationMockExtensions.CreateUserClaims(userId);
/// 
/// Prevents copy-paste JWT/claim construction in every test
/// </summary>
public static class AuthenticationMockExtensions
{
    /// <summary>
    /// Creates standard user claims for JWT
    /// Matches JwtTokenGenerator output format
    /// </summary>
    public static IList<Claim> CreateUserClaims(
        Guid userId,
        string userName = "testuser",
        string email = "test@example.com",
        string role = "User")
    {
        return new List<Claim>
        {
            new Claim("userId", userId.ToString()),
            new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", userId.ToString()),
            new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", userName),
            new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", email),
            new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", role),
        };
    }

    /// <summary>
    /// Creates admin user claims
    /// </summary>
    public static IList<Claim> CreateAdminClaims(
        Guid adminId,
        string adminName = "admin",
        string role = "Admin")
    {
        return new List<Claim>
        {
            new Claim("adminId", adminId.ToString()),
            new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", adminId.ToString()),
            new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", adminName),
            new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", role),
        };
    }

    /// <summary>
    /// Creates SuperAdmin claims
    /// </summary>
    public static IList<Claim> CreateSuperAdminClaims(Guid adminId, string adminName = "superadmin")
    {
        return CreateAdminClaims(adminId, adminName, "SuperAdmin");
    }

    /// <summary>
    /// Extracts userId from claims
    /// Returns null if not found
    /// </summary>
    public static Guid? ExtractUserId(ClaimsPrincipal principal)
    {
        var claim = principal?.FindFirst("userId");
        if (claim != null && Guid.TryParse(claim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Extracts adminId from claims
    /// Returns null if not found
    /// </summary>
    public static Guid? ExtractAdminId(ClaimsPrincipal principal)
    {
        var claim = principal?.FindFirst("adminId");
        if (claim != null && Guid.TryParse(claim.Value, out var adminId))
        {
            return adminId;
        }

        return null;
    }

    /// <summary>
    /// Extracts role from claims
    /// </summary>
    public static string? ExtractRole(ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Creates ClaimsPrincipal from claims
    /// Useful for mocking User context in controllers
    /// </summary>
    public static ClaimsPrincipal CreatePrincipal(IList<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, "jwt");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Creates authenticated user principal
    /// </summary>
    public static ClaimsPrincipal CreateUserPrincipal(
        Guid userId,
        string userName = "testuser",
        string email = "test@example.com",
        string role = "User")
    {
        var claims = CreateUserClaims(userId, userName, email, role);
        return CreatePrincipal(claims);
    }

    /// <summary>
    /// Creates authenticated admin principal
    /// </summary>
    public static ClaimsPrincipal CreateAdminPrincipal(
        Guid adminId,
        string adminName = "admin",
        string role = "Admin")
    {
        var claims = CreateAdminClaims(adminId, adminName, role);
        return CreatePrincipal(claims);
    }

    /// <summary>
    /// Creates SuperAdmin principal
    /// </summary>
    public static ClaimsPrincipal CreateSuperAdminPrincipal(
        Guid adminId,
        string adminName = "superadmin")
    {
        var claims = CreateSuperAdminClaims(adminId, adminName);
        return CreatePrincipal(claims);
    }

    /// <summary>
    /// Validates claims structure
    /// Useful for asserting correct claims in tests
    /// </summary>
    public static bool ValidateUserClaims(IList<Claim> claims)
    {
        var hasUserId = claims.Exists(c => c.Type == "userId");
        var hasName = claims.Exists(c => c.Type == ClaimTypes.Name);
        var hasEmail = claims.Exists(c => c.Type == ClaimTypes.Email);
        var hasRole = claims.Exists(c => c.Type == ClaimTypes.Role);

        return hasUserId && hasName && hasEmail && hasRole;
    }

    /// <summary>
    /// Creates mock HttpContext with authenticated user
    /// Can be used with middleware testing
    /// </summary>
    public static Mock<Microsoft.AspNetCore.Http.HttpContext> CreateMockHttpContextWithUser(
        Guid userId,
        string userName = "testuser")
    {
        var mockContext = new Mock<Microsoft.AspNetCore.Http.HttpContext>();
        var principal = CreateUserPrincipal(userId, userName);

        mockContext.Setup(c => c.User).Returns(principal);
        mockContext.Setup(c => c.Items).Returns(new Dictionary<object, object?>());

        return mockContext;
    }

    /// <summary>
    /// Creates mock HttpContext with authenticated admin
    /// </summary>
    public static Mock<Microsoft.AspNetCore.Http.HttpContext> CreateMockHttpContextWithAdmin(
        Guid adminId,
        string adminName = "admin",
        bool isSuperAdmin = false)
    {
        var mockContext = new Mock<Microsoft.AspNetCore.Http.HttpContext>();
        var role = isSuperAdmin ? "SuperAdmin" : "Admin";
        var principal = CreateAdminPrincipal(adminId, adminName, role);

        mockContext.Setup(c => c.User).Returns(principal);
        mockContext.Setup(c => c.Items).Returns(new Dictionary<object, object?>());

        return mockContext;
    }
}
