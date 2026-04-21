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
    /// Generate JWT token with custom context for 2FA operations
    /// isTempToken=true: 5 min (for 2FA verification - contains only sessionId + requires_2fa)
    /// isTempToken=false: 60 min (for normal authenticated operations)
    /// 
    /// SECURITY: TOTP secrets are NEVER included in JWT
    /// They are stored server-side in Redis and retrieved via sessionId
    /// </summary>
    public string GenerateToken(User user, bool isTempToken, Models.TokenContext? context = null)
    {
        return GenerateToken(user, isTempToken, context, isSuperAdmin: false);
    }

    /// <summary>
    /// Generate JWT token with custom context for 2FA operations
    /// isTempToken=true: 5 min (for 2FA verification - contains only sessionId + requires_2fa)
    /// isTempToken=false: 60 min (for normal authenticated operations)
    /// 
    /// SECURITY: TOTP secrets are NEVER included in JWT
    /// They are stored server-side in Redis and retrieved via sessionId
    /// </summary>
    public string GenerateToken(User user, bool isTempToken, Models.TokenContext? context, bool isSuperAdmin)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("sub", user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("userId", user.Id.ToString())
        };

        // Add context-specific claims (only safe ones)
        if (context != null)
        {
            // Add sessionId - used to lookup secrets in Redis
            if (!string.IsNullOrWhiteSpace(context.SessionId))
                claims.Add(new Claim("session_id", context.SessionId));

            // Add 2FA requirement flag
            if (context.TwoFactorRequired)
                claims.Add(new Claim("requires_2fa", "true"));

            // Add admin registration step (for bootstrap/invite flow)
            if (!string.IsNullOrWhiteSpace(context.AdminRegistrationStep))
                claims.Add(new Claim("registration_step", context.AdminRegistrationStep));

            // ✅ SECURITY: NEVER add TotpSecret to JWT claims
            // TOTP secret is stored in Redis indexed by sessionId
            // Client sends back sessionId, backend looks up secret from Redis
            // This prevents exposure if JWT is intercepted
        }

        // ✨ Add is_super_admin claim (only for admins)
        // Frontend uses this to show "Dodaj Admina" button
        if (isSuperAdmin)
        {
            claims.Add(new Claim("is_super_admin", "true"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiry = isTempToken ? 5 : 60;

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
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
