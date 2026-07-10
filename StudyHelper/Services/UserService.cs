using StudyHelper.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text.Json;

namespace StudyHelper.Services;

public class UserService : IUserService, IDisposable
{
    private readonly IEncryptionService _encryptionService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UserService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _userFilePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private const string CacheKey = "UserDataCache";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    // Password hashing constants
    private const int SaltSizeBytes = 16;  // 128 bits
    private const int HashSizeBytes = 32;  // 256 bits
    private const int Pbkdf2Iterations = 100_000;

    public UserService(
        IEncryptionService encryptionService,
        IWebHostEnvironment environment,
        ILogger<UserService> logger,
        IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(encryptionService);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(cache);

        _encryptionService = encryptionService;
        _environment = environment;
        _logger = logger;
        _cache = cache;

        var appDataPath = Path.Combine(_environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(appDataPath);
        _userFilePath = Path.Combine(appDataPath, "users.dat");
    }

    public async Task<User?> ValidateUserAsync(string username, string password)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);

        try
        {
            var users = await LoadUsersAsync();
            var user = users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent user: {Username}", username);
                return null;
            }

            // US-003: verify password for every user — no hardcoded allowlist
            if (VerifyPassword(password, user.Password))
            {
                _logger.LogInformation("User {Username} logged in successfully", username);
                return user;
            }

            _logger.LogWarning("Failed login attempt for user {Username}", username);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user {Username}", username);
            return null;
        }
    }

    public async Task<bool> CreateUserAsync(string username, string password)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);

        await _fileLock.WaitAsync();
        try
        {
            // Finding 2: lock is already held here — read from file directly to avoid reentrant deadlock
            if (!_cache.TryGetValue(CacheKey, out List<User>? users) || users == null)
                users = await LoadUsersFromFileAsync();

            if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Attempt to create duplicate user: {Username}", username);
                return false;
            }

            var user = new User
            {
                Username = username,
                Password = HashPassword(password),  // US-001: always store PBKDF2 hash, never plaintext
                CreatedDate = DateTime.UtcNow,
                LastLoginDate = null
            };

            users.Add(user);
            await SaveUsersAsync(users);

            _logger.LogInformation("User {Username} created successfully", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Username}", username);
            return false;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> UserExistsAsync(string username)
    {
        ArgumentNullException.ThrowIfNull(username);

        try
        {
            var users = await LoadUsersAsync();
            return users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user exists: {Username}", username);
            return false;
        }
    }

    public async Task UpdateLastLoginAsync(string username)
    {
        ArgumentNullException.ThrowIfNull(username);

        await _fileLock.WaitAsync();
        try
        {
            // Finding 2: lock is already held here — read from file directly to avoid reentrant deadlock
            if (!_cache.TryGetValue(CacheKey, out List<User>? users) || users == null)
                users = await LoadUsersFromFileAsync();

            var user = users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user != null)
            {
                user.LastLoginDate = DateTime.UtcNow;
                await SaveUsersAsync(users);
                _logger.LogDebug("Updated last login for user {Username}", username);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user {Username}", username);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<User?> GetUserAsync(string username)
    {
        ArgumentNullException.ThrowIfNull(username);

        try
        {
            var users = await LoadUsersAsync();
            return users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {Username}", username);
            return null;
        }
    }

    /// <summary>
    /// Public load path used by read-only callers (ValidateUserAsync, UserExistsAsync, GetUserAsync).
    /// Acquires _fileLock on a cache miss then delegates to LoadUsersFromFileAsync.
    /// Finding 2: callers that already hold _fileLock must NOT call this method — use
    /// LoadUsersFromFileAsync directly (after a cache check) to avoid a reentrant deadlock.
    /// </summary>
    private async Task<List<User>> LoadUsersAsync()
    {
        // Fast path: return from cache without acquiring the lock
        if (_cache.TryGetValue(CacheKey, out List<User>? cachedUsers) && cachedUsers != null)
            return cachedUsers;

        await _fileLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock in case another thread populated the cache
            if (_cache.TryGetValue(CacheKey, out cachedUsers) && cachedUsers != null)
                return cachedUsers;

            return await LoadUsersFromFileAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user data");
            return [];
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Internal file-read implementation. Callers MUST hold _fileLock before calling this method.
    /// Decrypts the user file, runs the plaintext-password migration if needed, and populates
    /// the cache. Finding 1: if migration saves the file, SaveUsersAsync has already set the
    /// cache, so this method skips the redundant outer cache-set.
    /// </summary>
    private async Task<List<User>> LoadUsersFromFileAsync()
    {
        if (!File.Exists(_userFilePath))
        {
            // Seed an empty plaintext JSON file on first run
            await File.WriteAllTextAsync(_userFilePath, JsonSerializer.Serialize(new List<UserRecord>()));
        }

        _logger.LogDebug("Loading user data from file");
        var fileBytes = await File.ReadAllBytesAsync(_userFilePath);
        var users = new List<User>();

        if (fileBytes.Length > 0)
        {
            users = await ReadUsersFromBytesAsync(fileBytes);
        }

        // US-004: re-hash any plaintext passwords left in the file before caching.
        // Finding 1: if migration ran, SaveUsersAsync already set the cache — skip the
        // outer set to avoid overwriting it with a stale reference.
        var migrated = await MigratePasswordsAsync(users);
        if (!migrated)
            _cache.Set(CacheKey, users, CacheExpiration);

        return users;
    }

    /// <summary>
    /// Encrypts and persists the user list then updates the cache.
    /// Finding 4: callers MUST hold _fileLock before calling this method to prevent
    /// concurrent writes that could corrupt the encrypted file.
    /// </summary>
    private async Task SaveUsersAsync(List<User> users)
    {
        try
        {
            // Usernames are stored as plaintext; only the PBKDF2 password hash is AES-GCM encrypted.
            var records = users.Select(u => new UserRecord
            {
                Username    = u.Username,
                Password    = Convert.ToBase64String(_encryptionService.Encrypt(u.Password)),
                CreatedDate = u.CreatedDate,
                LastLoginDate = u.LastLoginDate
            }).ToList();

            var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = false });
            await File.WriteAllTextAsync(_userFilePath, json);

            _cache.Set(CacheKey, users, CacheExpiration);
            _logger.LogDebug("User data saved (usernames plaintext, passwords encrypted)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user data");
            throw;
        }
    }

    /// <summary>
    /// Reads raw file bytes into a <see cref="User"/> list.
    /// Handles two formats:
    ///   1. New format — plaintext JSON array of <see cref="UserRecord"/> where Password is
    ///      Base64(AES-GCM ciphertext of the PBKDF2 hash). Usernames are clear text.
    ///   2. Legacy format — entire file was AES-GCM encrypted bytes produced by the old
    ///      SaveUsersAsync. Detected when the bytes cannot be parsed as UTF-8 JSON.
    /// </summary>
    private async Task<List<User>> ReadUsersFromBytesAsync(byte[] fileBytes)
    {
        // --- Try new plaintext-JSON format first ---
        try
        {
            var text = System.Text.Encoding.UTF8.GetString(fileBytes);
            if (text.TrimStart().StartsWith('[') || text.TrimStart().StartsWith('{'))
            {
                var records = JsonSerializer.Deserialize<List<UserRecord>>(text) ?? [];
                return records.Select(r => new User
                {
                    Username      = r.Username,
                    // Decrypt the per-user encrypted PBKDF2 hash back to the Base64 hash string
                    Password      = _encryptionService.Decrypt(Convert.FromBase64String(r.Password)),
                    CreatedDate   = r.CreatedDate,
                    LastLoginDate = r.LastLoginDate
                }).ToList();
            }
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            // Fall through to legacy format handling below
            _logger.LogDebug("File not in plaintext-JSON format, trying legacy encrypted format");
        }

        // --- Legacy format: entire file was AES-GCM encrypted ---
        _logger.LogInformation("Detected legacy fully-encrypted users.dat — migrating to new format");
        var legacyJson = _encryptionService.Decrypt(fileBytes);
        var legacyUsers = JsonSerializer.Deserialize<List<User>>(legacyJson) ?? [];
        // Persist immediately in the new format so next load uses the new path
        await SaveUsersAsync(legacyUsers);
        return legacyUsers;
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Pbkdf2Iterations,
            numBytesRequested: HashSizeBytes);

        var hashBytes = new byte[salt.Length + hash.Length];
        Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
        Buffer.BlockCopy(hash, 0, hashBytes, salt.Length, hash.Length);

        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// US-002: re-derives the PBKDF2 hash from the supplied plaintext and compares it
    /// against the stored hash using a constant-time comparison to prevent timing attacks.
    /// </summary>
    private static bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(hashedPassword);

            // Stored hash must be exactly salt + hash bytes; anything else is not a valid hash
            if (hashBytes.Length != SaltSizeBytes + HashSizeBytes)
            {
                return false;
            }

            // Extract the salt from the first SaltSizeBytes bytes
            var salt = new byte[SaltSizeBytes];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSizeBytes);

            // Re-derive the hash using the same parameters as HashPassword
            var computedHash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Pbkdf2Iterations,
                numBytesRequested: HashSizeBytes);

            // Extract the stored hash portion (bytes after the salt)
            var storedHash = new byte[HashSizeBytes];
            Buffer.BlockCopy(hashBytes, SaltSizeBytes, storedHash, 0, HashSizeBytes);

            // Constant-time comparison prevents timing-based side-channel attacks
            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
        catch (FormatException)
        {
            // hashedPassword was not valid Base64 — treat as no match
            return false;
        }
    }

    /// <summary>
    /// US-004: detects any plaintext passwords remaining in the loaded user list and
    /// replaces them with PBKDF2 hashes. Runs at startup before the list is cached.
    /// Idempotent — already-hashed passwords are skipped.
    /// Finding 1: returns true when migration ran so that LoadUsersFromFileAsync can skip
    /// the redundant outer cache-set (SaveUsersAsync already sets it on the migrated list).
    /// </summary>
    private async Task<bool> MigratePasswordsAsync(List<User> users)
    {
        var migrated = false;

        foreach (var user in users)
        {
            if (IsPasswordHashed(user.Password))
                continue;

            _logger.LogInformation(
                "Migrating plaintext password for user {Username} to PBKDF2 hash", user.Username);

            user.Password = HashPassword(user.Password);
            migrated = true;
        }

        if (migrated)
        {
            // SaveUsersAsync encrypts, persists, and sets the cache — no separate cache
            // invalidation needed here.
            await SaveUsersAsync(users);
            _logger.LogInformation("Password migration complete");
        }

        return migrated;
    }

    /// <summary>
    /// Returns true when the stored password value is a valid PBKDF2 hash produced by
    /// HashPassword: valid Base64 decoding to exactly SaltSizeBytes + HashSizeBytes bytes.
    /// </summary>
    private static bool IsPasswordHashed(string password)
    {
        try
        {
            var bytes = Convert.FromBase64String(password);
            return bytes.Length == SaltSizeBytes + HashSizeBytes;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _fileLock?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// On-disk representation of a user entry. Username is stored as clear text.
/// Password contains Base64(AES-GCM ciphertext of the PBKDF2 hash) — not the raw hash.
/// </summary>
internal sealed class UserRecord
{
    public required string Username      { get; set; }
    public required string Password      { get; set; }
    public DateTime  CreatedDate         { get; set; }
    public DateTime? LastLoginDate       { get; set; }
}
