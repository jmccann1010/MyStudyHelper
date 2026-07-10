using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;

namespace StudyHelper.Services;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(environment);

        _logger = logger;

        var keyString = configuration["Authentication:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(keyString))
        {
            // In production a missing key is a startup-blocking error, not a warning.
            if (!environment.IsDevelopment())
                throw new InvalidOperationException(
                    "Authentication:EncryptionKey is not configured. " +
                    "Supply the key via an environment variable or secrets vault.");

            _logger.LogWarning("Encryption key not found in configuration, generating temporary key");
            _key = RandomNumberGenerator.GetBytes(32);
        }
        else
        {
            try
            {
                _key = Convert.FromBase64String(keyString);
                if (_key.Length != 32)
                    throw new InvalidOperationException("Encryption key must be 32 bytes (256 bits)");

                // Reject the all-zero placeholder key in non-development environments.
                // appsettings.json ships with AAAAAAAAAA...= (32 zero bytes) as a safe default;
                // a real key MUST be injected via environment variable or secrets vault before deployment.
                if (!environment.IsDevelopment() && _key.All(b => b == 0))
                    throw new InvalidOperationException(
                        "Authentication:EncryptionKey is set to the default zero-byte placeholder. " +
                        "Replace it with a cryptographically random 32-byte key before deploying.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse encryption key from configuration");
                throw new InvalidOperationException("Invalid encryption key in configuration", ex);
            }
        }
    }

    public byte[] Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        try
        {
            using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];

            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            // Combine nonce + tag + ciphertext
            var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed");
            throw;
        }
    }

    public string Decrypt(byte[] ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        try
        {
            using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);

            var nonceSize = AesGcm.NonceByteSizes.MaxSize;
            var tagSize = AesGcm.TagByteSizes.MaxSize;

            if (ciphertext.Length < nonceSize + tagSize)
            {
                throw new CryptographicException("Invalid ciphertext length");
            }

            var nonce = new byte[nonceSize];
            var tag = new byte[tagSize];
            var ciphertextBytes = new byte[ciphertext.Length - nonceSize - tagSize];

            Buffer.BlockCopy(ciphertext, 0, nonce, 0, nonceSize);
            Buffer.BlockCopy(ciphertext, nonceSize, tag, 0, tagSize);
            Buffer.BlockCopy(ciphertext, nonceSize + tagSize, ciphertextBytes, 0, ciphertextBytes.Length);

            var plaintext = new byte[ciphertextBytes.Length];
            aes.Decrypt(nonce, ciphertextBytes, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed");
            throw;
        }
    }
}
