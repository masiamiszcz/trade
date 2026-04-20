using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
        if (string.IsNullOrWhiteSpace(_jwtSettings.Key))
        {
            throw new InvalidOperationException("JWT key is not configured.");
        }
    }

    /// <summary>
    /// Generate standard JWT token for regular users (60 min expiry)
    /// </summary>
    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("sub", user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generate JWT token with custom context for admin operations
    /// isTempToken=true: 5 min (for 2FA setup/verification)
    /// isTempToken=false: 60 min (for normal admin operations)
    /// </summary>
    public string GenerateToken(User user, bool isTempToken, Models.TokenContext? context = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("sub", user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("given_name", user.FirstName),
            new("family_name", user.LastName),
            new("userId", user.Id.ToString()),
            new("baseCurrency", user.BaseCurrency)
        };

        // Add context-specific claims if provided
        if (context != null)
        {
            if (!string.IsNullOrWhiteSpace(context.SessionId))
                claims.Add(new Claim("session_id", context.SessionId));

            if (!string.IsNullOrWhiteSpace(context.AdminRegistrationStep))
                claims.Add(new Claim("registration_step", context.AdminRegistrationStep));

            if (context.TwoFactorRequired)
                claims.Add(new Claim("requires_2fa", "true"));

            if (!string.IsNullOrWhiteSpace(context.TotpSecret))
                claims.Add(new Claim("totp_secret", context.TotpSecret));

            // Add backup codes as JSON if provided
            if (context.BackupCodes?.Count > 0)
            {
                var backupCodesJson = System.Text.Json.JsonSerializer.Serialize(context.BackupCodes);
                claims.Add(new Claim("backup_codes", backupCodesJson));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Expiry: 5 min for temp tokens, 60 min for normal tokens
        var expiryMinutes = isTempToken ? 5 : 60;

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validate JWT token and extract all claims
    /// Used for 2FA verification flows where temp token contains TOTP secret in claims
    /// </summary>
    public Dictionary<string, string>? ValidateTokenAndGetClaims(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var tokenHandler = new JwtSecurityTokenHandler();

            // Validate token signature and expiry
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            // Extract claims into dictionary
            var claims = new Dictionary<string, string>();
            foreach (var claim in principal.Claims)
            {
                // Skip adding duplicate keys, keep first value
                if (!claims.ContainsKey(claim.Type))
                {
                    claims[claim.Type] = claim.Value;
                }
            }

            return claims;
        }
        catch (Exception)
        {
            // Token is invalid (expired, tampered, invalid signature, etc.)
            return null;
        }
    }
}
