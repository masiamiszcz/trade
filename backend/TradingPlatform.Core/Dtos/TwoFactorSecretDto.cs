namespace TradingPlatform.Core.Dtos;

public sealed class TwoFactorSecretDto
{
    /// <summary>Secret in Base32 format (for manual entry)</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>QR code as data URL (can be displayed in img tag)</summary>
    public string QrCodeDataUrl { get; set; } = string.Empty;
}
