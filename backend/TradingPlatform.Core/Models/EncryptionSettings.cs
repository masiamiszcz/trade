namespace TradingPlatform.Core.Models;

/// <summary>
/// Configuration settings for encryption service
/// Read from appsettings.json section "Encryption"
/// Used for AES-256-GCM encryption of sensitive data (e.g., TOTP secrets)
/// </summary>
public sealed class EncryptionSettings
{
    /// <summary>
    /// Master key for encryption (minimum 32 characters)
    /// Will be hashed with PBKDF2 to derive 256-bit key
    /// IMPORTANT: Keep this secret and rotate periodically
    /// </summary>
    public string MasterKey { get; set; } = string.Empty;
}
