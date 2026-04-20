namespace TradingPlatform.Core.Models;



/// <summary>
/// Context information for token generation
/// Used for passing additional data when generating special tokens (temp, 2FA, etc.)
/// </summary>
public sealed class TokenContext
{
    /// <summary>Admin registration step (if applicable)</summary>
    public string? AdminRegistrationStep { get; set; }

    /// <summary>Session ID for correlation</summary>
    public string? SessionId { get; set; }

    /// <summary>Whether 2FA is required for this token</summary>
    public bool TwoFactorRequired { get; set; }

    /// <summary>TOTP secret for 2FA setup (temporarily stored in JWT for validation)</summary>
    public string? TotpSecret { get; set; }
}
