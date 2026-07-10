namespace StudyHelper.Services;

public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plaintext data using AES-256-GCM
    /// </summary>
    byte[] Encrypt(string plaintext);

    /// <summary>
    /// Decrypts encrypted data using AES-256-GCM
    /// </summary>
    string Decrypt(byte[] ciphertext);
}
