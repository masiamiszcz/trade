
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using QRCoder;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

/// <summary>
/// Service for managing TOTP (Time-based One-Time Password) - Google Authenticator
/// Generates secrets, verifies codes, and manages backup codes
/// RFC 6238 compliant implementation using HMAC-SHA1
/// </summary>
public sealed class TwoFactorService : ITwoFactorService
{
    private readonly TwoFactorSettings _settings;
    private const int SecretKeySize = 20; // 160 bits for TOTP
    private const int CodeLength = 6;
    private const long TimeStep = 30; // 30 seconds per RFC 6238

    public TwoFactorService(IOptions<TwoFactorSettings> options)
    {
        _settings = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Generate a new TOTP secret for an admin
    /// Returns QR code (as data URL) and manual entry key in Base32
    /// </summary>
    public TwoFactorSecretDto GenerateSecret()
    {
        try
        {
            // Generate 20-byte random secret (160 bits)
            byte[] secret = new byte[SecretKeySize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(secret);
            }

            var secretBase32 = Base32Encode(secret);

            // Generate QR code URL (otpauth:// format for Google Authenticator)
            var issuer = _settings.Issuer;
            var qrUrl = $"otpauth://totp/{issuer}?secret={secretBase32}&issuer={issuer}";

            // Generate QR code as PNG image (base64 data URL)
            var qrCodeGenerator = new QRCodeGenerator();
            var qrCodeData = qrCodeGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodePng = qrCode.GetGraphic(_settings.QrCodeSize);
            var qrCodeDataUrl = $"data:image/png;base64,{Convert.ToBase64String(qrCodePng)}";

            return new TwoFactorSecretDto
            {
                Secret = secretBase32,
                QrCodeDataUrl = qrCodeDataUrl
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate TOTP secret", ex);
        }
    }

    /// <summary>
    /// Verify a TOTP code (6 digits from Google Authenticator)
    /// Allows ±1 time window (60 seconds total, since each window is 30 sec)
    /// RFC 6238 compliant
    /// </summary>
    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
            return false;

        try
        {
            // Ensure code is exactly 6 digits
            if (!code.All(char.IsDigit) || code.Length != CodeLength)
                return false;

            // Convert secret from Base32
            byte[] secretBytes = Base32Decode(secret);

            // Get current time counter
            long currentTimeCounter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TimeStep;

            // Check current window and ±1 for clock skew tolerance
            for (int i = -1; i <= 1; i++)
            {
                long timeCounter = currentTimeCounter + i;
                string totp = GenerateTotp(secretBytes, timeCounter);
                
                if (totp == code)
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generate TOTP code for a given time counter
    /// RFC 6238 implementation using HMAC-SHA1
    /// </summary>
    private string GenerateTotp(byte[] secret, long timeCounter)
    {
        byte[] timeCounterBytes = BitConverter.GetBytes(timeCounter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeCounterBytes);

        using (var hmac = new HMACSHA1(secret))
        {
            byte[] hash = hmac.ComputeHash(timeCounterBytes);
            
            // Dynamic truncation - get 4-byte code from offset
            int offset = hash[^1] & 0xf; // Last byte low 4 bits = offset
            int code = ((hash[offset] & 0x7f) << 24)
                     | ((hash[offset + 1] & 0xff) << 16)
                     | ((hash[offset + 2] & 0xff) << 8)
                     | (hash[offset + 3] & 0xff);

            // 6-digit code
            return (code % 1000000).ToString("D6");
        }
    }

    /// <summary>
    /// Encode byte array to Base32 string (RFC 4648)
    /// </summary>
    private string Base32Encode(byte[] input)
    {
        if (input == null || input.Length == 0)
            return string.Empty;

        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var sb = new StringBuilder();
        int bits = 0;
        int value = 0;

        foreach (byte b in input)
        {
            value = (value << 8) | b;
            bits += 8;

            while (bits >= 5)
            {
                bits -= 5;
                sb.Append(alphabet[(value >> bits) & 31]);
            }
        }

        if (bits > 0)
            sb.Append(alphabet[(value << (5 - bits)) & 31]);

        // Padding
        while (sb.Length % 8 != 0)
            sb.Append('=');

        return sb.ToString();
    }

    /// <summary>
    /// Decode Base32 string to byte array (RFC 4648)
    /// </summary>
    private byte[] Base32Decode(string input)
    {
        if (string.IsNullOrEmpty(input))
            return Array.Empty<byte>();

        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        input = input.TrimEnd('=').ToUpper();

        var output = new List<byte>();
        int bits = 0;
        int value = 0;

        foreach (char c in input)
        {
            int charValue = alphabet.IndexOf(c);
            if (charValue < 0)
                throw new FormatException($"Invalid Base32 character: {c}");

            value = (value << 5) | charValue;
            bits += 5;

            if (bits >= 8)
            {
                bits -= 8;
                output.Add((byte)((value >> bits) & 255));
            }
        }

        return output.ToArray();
    }

    /// <summary>
    /// Generate backup codes (8 codes of 8 characters each)
    /// These are returned as plaintext - caller should hash them before storing
    /// </summary>
    public string[] GenerateBackupCodes()
    {
        var codes = new string[_settings.BackupCodeCount];
        
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            for (int i = 0; i < _settings.BackupCodeCount; i++)
            {
                // Generate 6 random bytes and convert to alphanumeric
                byte[] randomBytes = new byte[6];
                rng.GetBytes(randomBytes);
                
                // Create alphanumeric code (A-Z, 0-9, no confusing chars: I, O, L, 0)
                const string validChars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
                var sb = new StringBuilder(8);
                
                for (int j = 0; j < 8; j++)
                {
                    sb.Append(validChars[randomBytes[j % randomBytes.Length] % validChars.Length]);
                }
                
                codes[i] = sb.ToString();
            }
        }
        
        return codes;
    }

    /// <summary>
    /// Hash a backup code using SHA256 (before storing in DB)
    /// </summary>
    public string HashBackupCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentNullException(nameof(code));

        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(code));
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// Verify a backup code against stored hashes
    /// Returns (isValid, matchedHashIndex) so matched code can be removed
    /// </summary>
    public (bool IsValid, int? MatchedIndex) VerifyBackupCode(string code, string[] hashedCodes)
    {
        if (string.IsNullOrWhiteSpace(code) || hashedCodes == null || hashedCodes.Length == 0)
            return (false, null);

        try
        {
            string codeHash = HashBackupCode(code);
            
            for (int i = 0; i < hashedCodes.Length; i++)
            {
                // Constant-time comparison to prevent timing attacks
                if (ConstantTimeEquals(codeHash, hashedCodes[i]))
                {
                    return (true, i);
                }
            }
            
            return (false, null);
        }
        catch
        {
            return (false, null);
        }
    }

    /// <summary>
    /// Constant-time string comparison (prevents timing attacks)
    /// </summary>
    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        
        int result = a.Length ^ b.Length;
        for (int i = 0; i < a.Length && i < b.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }
}
