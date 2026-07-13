using System.Security.Cryptography;
using System.Text;

namespace StudyHelper.Services;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        var keyString = configuration["Authentication:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(keyString))
        {
            // No key in configuration — generate a temporary in-memory key.
            // This only happens if the configuration entry is completely absent.
            // appsettings.json always supplies a key, so this is a last-resort fallback.
            _logger.LogWarning("Authentication:EncryptionKey not found in configuration; generating a temporary in-memory key. Data encrypted with this key will not survive a restart.");
            _key = RandomNumberGenerator.GetBytes(32);
        }
        else
        {
            try
            {
                _key = Convert.FromBase64String(keyString);
                if (_key.Length != 32)
                    throw new InvalidOperationException("Encryption key must be 32 bytes (256 bits)");
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
