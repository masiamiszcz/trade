namespace TradingPlatform.Core.Models;

/// <summary>
/// Configuration settings for Two-Factor Authentication
/// Read from appsettings.json section "TwoFactor"
/// </summary>
public sealed class TwoFactorSettings
{
    /// <summary>Issuer name for TOTP QR codes (appears in Google Authenticator)</summary>
    public string Issuer { get; set; } = "TradingPlatform";

    /// <summary>QR code size in pixels</summary>
    public int QrCodeSize { get; set; } = 10;

    /// <summary>Time window in seconds (RFC 6238 default is 30)</summary>
    public int TimeWindowSeconds { get; set; } = 30;

    /// <summary>Number of backup codes to generate</summary>
    public int BackupCodeCount { get; set; } = 8;
}
