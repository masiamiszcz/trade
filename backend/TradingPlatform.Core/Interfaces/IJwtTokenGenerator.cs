using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generate standard JWT token for regular users
    /// Expiry: 60 minutes
    /// </summary>
    string GenerateToken(User user);

    /// <summary>
    /// Generate JWT token with custom context (for admins with 2FA, temporary tokens, etc.)
    /// </summary>
    /// <param name="user">User to generate token for</param>
    /// <param name="isTempToken">If true: 5 min expiry, if false: 60 min expiry</param>
    /// <param name="context">Custom claims context (session_id, totp_secret, registration_step, etc.)</param>
    /// <returns>JWT token</returns>
    string GenerateToken(User user, bool isTempToken, TradingPlatform.Core.Models.TokenContext? context = null);

    /// <summary>
    /// Validate JWT token and extract all claims
    /// Used for 2FA verification flows where temp token contains TOTP secret in claims
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>Dictionary of claim names to values, or null if validation fails</returns>
    System.Collections.Generic.Dictionary<string, string>? ValidateTokenAndGetClaims(string token);
}
