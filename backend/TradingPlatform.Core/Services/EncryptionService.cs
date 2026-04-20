using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data (like 2FA secrets)
/// Uses AES-256-GCM (Authenticated Encryption with Associated Data)
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    private readonly byte[] _masterKey;
    private const int NonceSizeInBytes = 12;  // 96 bits for GCM
    private const int AuthTagSizeInBytes = 16; // 128 bits
    private const int SaltSizeInBytes = 16;    // For key derivation

    /// <summary>
    /// Initialize encryption service with master key from configuration
    /// </summary>
    public EncryptionService(IOptions<EncryptionSettings> options)
    {
        var settings = options.Value ?? throw new ArgumentNullException(nameof(options));
        
        if (string.IsNullOrWhiteSpace(settings.MasterKey))
            throw new InvalidOperationException("Master key not configured in appsettings.json");
        
        if (settings.MasterKey.Length < 32)
            throw new InvalidOperationException("Master key must be at least 32 characters");

        // Use PBKDF2 to derive a proper 256-bit key from master key
        using (var pbkdf2 = new Rfc2898DeriveBytes(settings.MasterKey, Encoding.UTF8.GetBytes("TradingPlatform"), 10000, HashAlgorithmName.SHA256))
        {
            _masterKey = pbkdf2.GetBytes(32); // 256 bits
        }
    }

    /// <summary>
    /// Encrypt plaintext using AES-256-GCM
    /// Returns format: "IV:Ciphertext:AuthTag" (all base64 encoded)
    /// </summary>
    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            throw new ArgumentNullException(nameof(plaintext));

        try
        {
            using (var aes = new AesGcm(_masterKey, AuthTagSizeInBytes))
            {
                // Generate random nonce (IV)
                byte[] nonce = new byte[NonceSizeInBytes];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(nonce);
                }

                // Convert plaintext to bytes
                byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                
                // Reserve space for ciphertext and auth tag
                byte[] ciphertext = new byte[plaintextBytes.Length];
                byte[] authTag = new byte[AuthTagSizeInBytes];

                // Encrypt
                aes.Encrypt(nonce, plaintextBytes, ciphertext, authTag);

                // Format: "nonce:ciphertext:authTag" all base64 encoded
                string nonceB64 = Convert.ToBase64String(nonce);
                string ciphertextB64 = Convert.ToBase64String(ciphertext);
                string authTagB64 = Convert.ToBase64String(authTag);

                return $"{nonceB64}:{ciphertextB64}:{authTagB64}";
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    /// <summary>
    /// Decrypt ciphertext using AES-256-GCM
    /// Expects format: "IV:Ciphertext:AuthTag" (base64 encoded)
    /// </summary>
    public string Decrypt(string encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData))
            throw new ArgumentNullException(nameof(encryptedData));

        try
        {
            // Parse format: "nonce:ciphertext:authTag"
            var parts = encryptedData.Split(':');
            if (parts.Length != 3)
                throw new InvalidOperationException("Invalid encrypted data format");

            byte[] nonce = Convert.FromBase64String(parts[0]);
            byte[] ciphertext = Convert.FromBase64String(parts[1]);
            byte[] authTag = Convert.FromBase64String(parts[2]);

            using (var aes = new AesGcm(_masterKey, AuthTagSizeInBytes))
            {
                // Reserve space for plaintext
                byte[] plaintext = new byte[ciphertext.Length];

                // Decrypt (will throw if authTag doesn't match)
                aes.Decrypt(nonce, ciphertext, authTag, plaintext);

                return Encoding.UTF8.GetString(plaintext);
            }
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Decryption failed - data may be corrupted or modified", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }
}
