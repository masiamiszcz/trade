using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using TradingPlatform.Core.Services;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Core.Enums;

namespace TradingPlatform.Tests;

/// <summary>
/// Minimal JWT security tests - only critical scenarios
/// Verifies that sensitive data is NOT exposed in JWT claims
/// </summary>
public class JwtSecurityTests
{
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly JwtSettings _jwtSettings;
    private readonly IOptions<JwtSettings> _jwtSettingsOptions;

    public JwtSecurityTests()
    {
        _jwtSettings = new JwtSettings
        {
            Key = "this_is_a_very_long_secret_key_that_is_at_least_32_bytes_long_12345",
            Issuer = "TradingPlatform",
            Audience = "TradingPlatformUsers",
            ExpiryMinutes = 60
        };

        _jwtSettingsOptions = Options.Create(_jwtSettings);
        _tokenGenerator = new JwtTokenGenerator(_jwtSettingsOptions);
    }

    // ============ CRITICAL SECURITY TESTS ============

    /// <summary>
    /// TEST 1: Verify TOTP secret is NOT in JWT claims
    /// CRITICAL: If this fails, 2FA is completely compromised
    /// </summary>
    [Fact]
    public void GenerateToken_2FA_Does_NOT_Include_TOTP_Secret()
    {
        // Arrange
        var user = CreateTestUser();
        var context = new TradingPlatform.Core.Models.TokenContext
        {
            SessionId = Guid.NewGuid().ToString(),
            TwoFactorRequired = true,
            TotpSecret = "JBSWY3DPEBLW64TMMQ======", // Base32 encoded secret
            BackupCodes = new List<string> { "12345678", "87654321" },
            Password = "SuperSecretPassword123!"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user, isTempToken: true, context);
        var claims = ExtractClaims(token);

        // Assert: TOTP secret should NOT be in claims
        Assert.False(claims.ContainsKey("totp_secret"), 
            "❌ SECURITY FAILURE: TOTP secret should NOT be in JWT claims!");
        
        // Verify sessionId IS in claims (it's safe to transmit)
        Assert.True(claims.ContainsKey("session_id"), 
            "SessionId must be present for Redis lookup");
        Assert.Equal(context.SessionId, claims["session_id"]);
    }

    /// <summary>
    /// TEST 2: Verify password is NOT in JWT claims
    /// CRITICAL: Password should never leave server
    /// </summary>
    [Fact]
    public void GenerateToken_2FA_Does_NOT_Include_Password()
    {
        // Arrange
        var user = CreateTestUser();
        var context = new TradingPlatform.Core.Models.TokenContext
        {
            SessionId = Guid.NewGuid().ToString(),
            TwoFactorRequired = true,
            TotpSecret = string.Empty, // Shouldn't matter
            BackupCodes = new List<string>(),
            Password = "SuperSecretPassword123!"
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user, isTempToken: true, context);
        var claims = ExtractClaims(token);

        // Assert
        Assert.False(claims.ContainsKey("password"), 
            "❌ SECURITY FAILURE: Password should NEVER be in JWT claims!");
        
        // Verify password field is NOT in token at all (even empty)
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
        var passwordClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == "password");
        Assert.Null(passwordClaim);
    }

    /// <summary>
    /// TEST 3: Verify backup codes are NOT in JWT claims
    /// CRITICAL: All 8 backup codes should never be transmitted together
    /// </summary>
    [Fact]
    public void GenerateToken_2FA_Does_NOT_Include_BackupCodes()
    {
        // Arrange
        var user = CreateTestUser();
        var backupCodes = new List<string>
        {
            "11111111", "22222222", "33333333", "44444444",
            "55555555", "66666666", "77777777", "88888888"
        };

        var context = new TradingPlatform.Core.Models.TokenContext
        {
            SessionId = Guid.NewGuid().ToString(),
            TwoFactorRequired = true,
            TotpSecret = string.Empty,
            BackupCodes = backupCodes,
            Password = string.Empty
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user, isTempToken: true, context);
        var claims = ExtractClaims(token);

        // Assert
        Assert.False(claims.ContainsKey("backup_codes"), 
            "❌ SECURITY FAILURE: Backup codes should NOT be in JWT claims!");
        
        // Verify no individual backup codes are leaked
        foreach (var code in backupCodes)
        {
            var hasBackupCode = claims.Values.Any(v => v?.Contains(code) ?? false);
            Assert.False(hasBackupCode, 
                $"❌ SECURITY FAILURE: Backup code '{code}' leaked in JWT!");
        }
    }

    // ============ TOKEN EXPIRY TESTS ============

    /// <summary>
    /// TEST 4: Verify temp token (2FA) expires in 5 minutes
    /// </summary>
    [Fact]
    public void GenerateToken_TempToken_Has_5MinuteExpiry()
    {
        // Arrange
        var user = CreateTestUser();
        var context = new TradingPlatform.Core.Models.TokenContext { SessionId = Guid.NewGuid().ToString() };
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _tokenGenerator.GenerateToken(user, isTempToken: true, context);
        var afterGeneration = DateTime.UtcNow;
        var expiry = ExtractExpiry(token);

        // Assert: Should be approximately 5 minutes from now
        var expectedMinExpiry = beforeGeneration.AddMinutes(4).AddSeconds(50);
        var expectedMaxExpiry = afterGeneration.AddMinutes(5).AddSeconds(10);
        
        Assert.True(expiry > expectedMinExpiry && expiry < expectedMaxExpiry,
            $"❌ Temp token expiry incorrect. Expected ~5 min, got {(expiry - DateTime.UtcNow).TotalMinutes:F1} min");
    }

    /// <summary>
    /// TEST 5: Verify final token (normal auth) expires in 60 minutes
    /// </summary>
    [Fact]
    public void GenerateToken_FinalToken_Has_60MinuteExpiry()
    {
        // Arrange
        var user = CreateTestUser();
        var context = new TradingPlatform.Core.Models.TokenContext { SessionId = Guid.NewGuid().ToString() };
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _tokenGenerator.GenerateToken(user, isTempToken: false, context);
        var afterGeneration = DateTime.UtcNow;
        var expiry = ExtractExpiry(token);

        // Assert: Should be approximately 60 minutes from now
        var expectedMinExpiry = beforeGeneration.AddMinutes(59).AddSeconds(50);
        var expectedMaxExpiry = afterGeneration.AddMinutes(60).AddSeconds(10);
        
        Assert.True(expiry > expectedMinExpiry && expiry < expectedMaxExpiry,
            $"❌ Final token expiry incorrect. Expected ~60 min, got {(expiry - DateTime.UtcNow).TotalMinutes:F1} min");
    }

    // ============ TOKEN VALIDATION TESTS ============

    /// <summary>
    /// TEST 6: Verify valid token is accepted
    /// </summary>
    [Fact]
    public void ValidateToken_ValidToken_ReturnsClaimsSuccessfully()
    {
        // Arrange
        var user = CreateTestUser();
        var token = _tokenGenerator.GenerateToken(user, isTempToken: true);

        // Act
        var claims = _tokenGenerator.ValidateTokenAndGetClaims(token);

        // Assert
        Assert.NotNull(claims);
        Assert.NotEmpty(claims);
        // Check for either 'sub' or userId claim (might be indexed differently)
        Assert.True(claims.ContainsKey("sub") || claims.ContainsKey("userId") || 
                    claims.Keys.Any(k => k.Contains("userId", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// TEST 7: Verify expired token is rejected
    /// </summary>
    [Fact]
    public void ValidateToken_ExpiredToken_ReturnsNull()
    {
        // Arrange: Create a token that expires immediately
        var user = CreateTestUser();
        var claimsForExpired = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("sub", user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiredToken = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claimsForExpired,
            expires: DateTime.UtcNow.AddSeconds(-1), // Expired 1 second ago
            signingCredentials: creds);

        var token = new JwtSecurityTokenHandler().WriteToken(expiredToken);

        // Act
        var result = _tokenGenerator.ValidateTokenAndGetClaims(token);

        // Assert: Should reject expired token
        Assert.Null(result);
    }

    /// <summary>
    /// TEST 8: Verify tampered token is rejected
    /// </summary>
    [Fact]
    public void ValidateToken_TamperedToken_ReturnsNull()
    {
        // Arrange: Create valid token then tamper with it
        var user = CreateTestUser();
        var token = _tokenGenerator.GenerateToken(user, isTempToken: true);

        // Tamper: Flip a byte in the signature
        var tokenParts = token.Split('.');
        if (tokenParts.Length == 3)
        {
            var signaturBytes = Base64UrlDecode(tokenParts[2]);
            if (signaturBytes.Length > 0)
            {
                signaturBytes[0] = (byte)(signaturBytes[0] ^ 0xFF); // Flip bits
                tokenParts[2] = Base64UrlEncode(signaturBytes);
            }
        }

        var tamperedToken = string.Join(".", tokenParts);

        // Act
        var result = _tokenGenerator.ValidateTokenAndGetClaims(tamperedToken);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// TEST 9: Verify sessionId claim is present and correct in 2FA token
    /// </summary>
    [Fact]
    public void GenerateToken_2FA_ContainsCorrectSessionId()
    {
        // Arrange
        var user = CreateTestUser();
        var sessionId = Guid.NewGuid().ToString();
        var context = new TradingPlatform.Core.Models.TokenContext
        {
            SessionId = sessionId,
            TwoFactorRequired = true
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user, isTempToken: true, context);
        var claims = ExtractClaims(token);

        // Assert
        Assert.True(claims.ContainsKey("session_id"));
        Assert.Equal(sessionId, claims["session_id"]);
    }

    /// <summary>
    /// TEST 10: Verify requires_2fa flag is present in 2FA token
    /// </summary>
    [Fact]
    public void GenerateToken_2FA_Contains_Requires2FA_Flag()
    {
        // Arrange
        var user = CreateTestUser();
        var context = new TradingPlatform.Core.Models.TokenContext
        {
            SessionId = Guid.NewGuid().ToString(),
            TwoFactorRequired = true
        };

        // Act
        var token = _tokenGenerator.GenerateToken(user, isTempToken: true, context);
        var claims = ExtractClaims(token);

        // Assert
        Assert.True(claims.ContainsKey("requires_2fa"));
        Assert.Equal("true", claims["requires_2fa"]);
    }

    // ============ HELPER METHODS ============

    private Dictionary<string, string> ExtractClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
        
        var claims = new Dictionary<string, string>();
        foreach (var claim in jwtToken?.Claims ?? Enumerable.Empty<Claim>())
        {
            if (!claims.ContainsKey(claim.Type))
            {
                claims[claim.Type] = claim.Value;
            }
        }
        
        return claims;
    }

    private DateTime ExtractExpiry(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
        return jwtToken?.ValidTo ?? DateTime.MinValue;
    }

    private User CreateTestUser()
    {
        return new User(
            Id: Guid.NewGuid(),
            UserName: "testuser",
            Email: "test@example.com",
            FirstName: "Test",
            LastName: "User",
            Role: UserRole.User,
            EmailConfirmed: false,
            TwoFactorEnabled: false,
            TwoFactorSecret: string.Empty,
            BackupCodes: string.Empty,
            Status: UserStatus.Active,
            BaseCurrency: "PLN",
            CreatedAtUtc: DateTimeOffset.UtcNow
        );
    }

    private string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace("-", "+").Replace("_", "/");
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        return Convert.FromBase64String(output);
    }
}
