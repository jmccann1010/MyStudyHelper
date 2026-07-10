using StudyHelper.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text.Json;

namespace StudyHelper.Services;

public class UserService : IUserService, IDisposable
{
    //private readonly IEncryptionService _encryptionService;
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
        //IEncryptionService encryptionService,
        IWebHostEnvironment environment,
        ILogger<UserService> logger,
        IMemoryCache cache)
    {
        //ArgumentNullException.ThrowIfNull(encryptionService);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(cache);

        //_encryptionService = encryptionService;
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

            //if (user.Username == "admin" && user.Password == "admin")
            //{
            //    _logger.LogInformation("Admin user logged in with default credentials");
            //    return user;
            //}
            if (user.Username == "mccannj5" || user.Username == "hoffmanj7")
            {
                if (VerifyPassword(password, user.Password))
                {
                    _logger.LogInformation("User {Username} logged in successfully", username);
                    return user;
                }
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
            var users = await LoadUsersAsync();

            if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Attempt to create duplicate user: {Username}", username);
                return false;
            }

            var user = new User
            {
                Username = username,
                Password = password,
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
            var users = await LoadUsersAsync();
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

    private async Task<List<User>> LoadUsersAsync()
    {
        if (_cache.TryGetValue(CacheKey, out List<User>? cachedUsers) && cachedUsers != null)
        {
            return cachedUsers;
        }

        await _fileLock.WaitAsync();
        try
        {
            // Double-check cache after acquiring lock
            if (_cache.TryGetValue(CacheKey, out cachedUsers) && cachedUsers != null)
            {
                return cachedUsers;
            }

            if (!File.Exists(_userFilePath))
            {
                // Create empty user file
                await File.WriteAllTextAsync(_userFilePath, "[]");
            }

            _logger.LogDebug("Loading user data from encrypted file");
            //var encryptedData = await File.ReadAllBytesAsync(_userFilePath);
            var jsonUsers = await File.ReadAllBytesAsync(_userFilePath);
            var users = new List<User>();
            if (jsonUsers.Length > 0)
            {
                //var json = _encryptionService.Decrypt(encryptedData);
                users = JsonSerializer.Deserialize<List<User>>(jsonUsers) ?? [];
            }

            _cache.Set(CacheKey, users, CacheExpiration);
            return users;
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

    private async Task SaveUsersAsync(List<User> users)
    {
        try
        {
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions 
            { 
                WriteIndented = false 
            });
            //var encryptedData = _encryptionService.Encrypt(json);
            File.WriteAllText(_userFilePath, json);

            _cache.Set(CacheKey, users, CacheExpiration);
            _logger.LogDebug("User data saved to encrypted file");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user data");
            throw;
        }
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

    private static bool VerifyPassword(string password, string hashedPassword)
    {
        if(password == hashedPassword)  // For demonstration purposes only; replace with proper hash verification
        {
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        _fileLock?.Dispose();
        GC.SuppressFinalize(this);
    }
}
