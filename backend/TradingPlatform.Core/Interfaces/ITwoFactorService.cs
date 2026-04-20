using TradingPlatform.Core.Dtos;

namespace TradingPlatform.Core.Interfaces;

public interface ITwoFactorService
{
    /// <summary>Generate new TOTP secret</summary>
    TwoFactorSecretDto GenerateSecret();

    /// <summary>Verify TOTP code</summary>
    bool VerifyCode(string secret, string code);

    /// <summary>Generate backup codes</summary>
    string[] GenerateBackupCodes();

    /// <summary>Hash backup code for storage</summary>
    string HashBackupCode(string code);

    /// <summary>Verify backup code against hashes</summary>
    (bool IsValid, int? MatchedIndex) VerifyBackupCode(string code, string[] hashedCodes);
}
