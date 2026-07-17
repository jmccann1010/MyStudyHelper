using StudyHelper.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text.Json;

namespace StudyHelper.Services;

/// <summary>
/// Service for managing user-uploaded study materials.
/// </summary>
public class UserStudyMaterialService : IUserStudyMaterialService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IFileValidationService _validationService;
    private readonly ILogger<UserStudyMaterialService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    private const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const string DefaultStudyMaterialsFolder = "StudyMaterials";

    private readonly long _maxFileSizeBytes;
    private readonly string _storageFolderName;

    public UserStudyMaterialService(
        IWebHostEnvironment environment,
        IFileValidationService validationService,
        ILogger<UserStudyMaterialService> logger,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        _maxFileSizeBytes = _configuration.GetValue<long>("StudyMaterials:MaxFileSizeBytes", DefaultMaxFileSizeBytes);
        _storageFolderName = _configuration.GetValue<string>("StudyMaterials:StorageFolder", DefaultStudyMaterialsFolder) ?? DefaultStudyMaterialsFolder;
    }

    public async Task<FileValidationResult> UploadTermsAsync(string username, IFormFile file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentNullException.ThrowIfNull(file);

        return await UploadMaterialAsync(username, file, StudyMaterialType.TermsAndDefinitions);
    }

    public async Task<FileValidationResult> UploadEquationsAsync(string username, IFormFile file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentNullException.ThrowIfNull(file);

        return await UploadMaterialAsync(username, file, StudyMaterialType.Equations);
    }

    private async Task<FileValidationResult> UploadMaterialAsync(string username, IFormFile file, StudyMaterialType materialType)
    {
        try
        {
            // 1. Validate file size
            if (file.Length > _maxFileSizeBytes)
            {
                _logger.LogWarning("File too large for user {Username}: {Size} bytes (max: {MaxSize})",
                    username, file.Length, _maxFileSizeBytes);
                return new FileValidationResult { IsValid = false, Errors = ["File exceeds the maximum allowed size."] };
            }

            if (file.Length == 0)
            {
                _logger.LogWarning("Empty file uploaded by user {Username}", username);
                return new FileValidationResult { IsValid = false, Errors = ["File is empty."] };
            }

            // 2. Validate markdown structure (extension + readability)
            using var stream = file.OpenReadStream();
            var markdownResult = await _validationService.ValidateMarkdownFileAsync(stream, file.FileName);
            if (!markdownResult.IsValid)
            {
                _logger.LogWarning("File validation failed for user {Username}: {Errors}",
                    username, string.Join(", ", markdownResult.Errors));
                return markdownResult;
            }

            // 3. Read full content
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            // ValidatePlainTextAsync is intentionally disabled — see design doc.

            // 4. Format validation (populates counts; errors cause rejection)
            var formatResult = materialType == StudyMaterialType.TermsAndDefinitions
                ? await _validationService.ValidateTermsFormatAsync(content)
                : await _validationService.ValidateEquationsFormatAsync(content);

            if (!formatResult.IsValid)
            {
                _logger.LogWarning("{MaterialType} format validation failed for user {Username}: {Errors}",
                    materialType, username, string.Join(", ", formatResult.Errors));
                return formatResult;
            }

            if (formatResult.Warnings.Count > 0)
                _logger.LogInformation("{MaterialType} format warnings for user {Username}: {Warnings}",
                    materialType, username, string.Join(", ", formatResult.Warnings));

            // 5. Security scan
            var securityResult = await _validationService.ScanForMaliciousContentAsync(content);
            if (!securityResult.IsValid)
            {
                _logger.LogError("Security scan failed for user {Username}: {Errors}",
                    username, string.Join(", ", securityResult.Errors));
                return securityResult;
            }

            // 6. Save as plain text
            var userFolder = GetUserFolder(username);
            Directory.CreateDirectory(userFolder);

            var fileName = GetFileName(materialType);
            var filePath = Path.Combine(userFolder, fileName);
            await File.WriteAllTextAsync(filePath, content);

            // 7. Update metadata
            await SaveMetadataAsync(username, materialType, file, content);

            // 8. Invalidate cache
            InvalidateCache(username, materialType);

            _logger.LogInformation("User {Username} uploaded {MaterialType} ({Size} bytes)",
                username, materialType, file.Length);

            // Return formatResult so counts (ParsedSectionCount, ParsedTermCount,
            // ParsedEquationCount) are available to the controller.
            return formatResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading {MaterialType} for user {Username}", materialType, username);
            return new FileValidationResult { IsValid = false, Errors = ["An unexpected error occurred during upload."] };
        }
    }

    public async Task<List<UserStudyMaterial>> GetUserMaterialsAsync(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        try
        {
            var metadata = await LoadMetadataAsync(username);
            return metadata.Materials;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading materials metadata for user {Username}", username);
            return new List<UserStudyMaterial>();
        }
    }

    public async Task<bool> DeleteUserMaterialAsync(string username, StudyMaterialType materialType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        try
        {
            var filePath = GetCustomFilePath(username, materialType);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted {MaterialType} for user {Username}", materialType, username);
            }

            // Update metadata
            var materials = await GetUserMaterialsAsync(username);
            materials.RemoveAll(m => m.MaterialType == materialType);
            await SaveMetadataInternalAsync(username, materials);

            // Invalidate cache
            InvalidateCache(username, materialType);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {MaterialType} for user {Username}", materialType, username);
            return false;
        }
    }

    public async Task<string> GetEffectiveFilePathAsync(string username, StudyMaterialType materialType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var customPath = GetCustomFilePath(username, materialType);

        if (File.Exists(customPath))
        {
            _logger.LogDebug("Using custom {MaterialType} for user {Username}", materialType, username);
            return customPath;
        }

        // Return default path from InputDocuments
        var defaultPath = GetDefaultFilePath(materialType);
        _logger.LogDebug("Using default {MaterialType} for user {Username}", materialType, username);
        return defaultPath;
    }

    public Task<bool> HasCustomMaterialAsync(string username, StudyMaterialType materialType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var customPath = GetCustomFilePath(username, materialType);
        return Task.FromResult(File.Exists(customPath));
    }

    public async Task<string?> GetDecryptedContentAsync(string username, StudyMaterialType materialType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var cacheKey = GetCacheKey(username, materialType);

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out string? cachedContent))
        {
            _logger.LogDebug("Returning cached content for {Username}/{MaterialType}", username, materialType);
            return cachedContent;
        }

        var customPath = GetCustomFilePath(username, materialType);

        if (!File.Exists(customPath))
        {
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(customPath);

            // Cache for 1 hour
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));
            _cache.Set(cacheKey, content, cacheOptions);

            _logger.LogDebug("Read and cached content for {Username}/{MaterialType}", username, materialType);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading {MaterialType} for user {Username}", materialType, username);
            return null;
        }
    }

    private async Task SaveMetadataAsync(string username, StudyMaterialType materialType, IFormFile file, string content)
    {
        var materials = await GetUserMaterialsAsync(username);

        // Remove existing entry for this material type
        materials.RemoveAll(m => m.MaterialType == materialType);

        // Add new entry
        var material = new UserStudyMaterial
        {
            Username = username,
            MaterialType = materialType,
            FileName = file.FileName,
            FileSizeBytes = file.Length,
            UploadedDate = DateTime.UtcNow,
            FilePath = GetCustomFilePath(username, materialType),
            FileHash = ComputeHash(content)
        };

        materials.Add(material);

        await SaveMetadataInternalAsync(username, materials);
    }

    private async Task SaveMetadataInternalAsync(string username, List<UserStudyMaterial> materials)
    {
        var existingMetadata = await LoadMetadataAsync(username);

        var metadata = new UserStudyMaterialMetadata
        {
            Username = username,
            Materials = materials,
            EquationsEnabled = existingMetadata.EquationsEnabled // Preserve existing preference
        };

        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var metadataPath = GetMetadataPath(username);
        var metadataFolder = Path.GetDirectoryName(metadataPath);

        if (!string.IsNullOrEmpty(metadataFolder))
        {
            Directory.CreateDirectory(metadataFolder);
        }

        await File.WriteAllTextAsync(metadataPath, json);
    }

    private string GetUserFolder(string username)
    {
        var safeUsername = SanitizePathSegment(username);
        var folder = Path.GetFullPath(Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            _storageFolderName,
            safeUsername
        ));
        AssertUnderRoot(folder);
        return folder;
    }

    private string GetCustomFilePath(string username, StudyMaterialType materialType)
    {
        var fileName = GetFileName(materialType);
        return Path.Combine(GetUserFolder(username), fileName);
    }

    private string GetDefaultFilePath(StudyMaterialType materialType)
    {
        var fileName = GetFileName(materialType);
        return Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            fileName
        );
    }

    private string GetMetadataPath(string username)
    {
        return Path.Combine(GetUserFolder(username), "metadata.json");
    }

    private static string GetFileName(StudyMaterialType materialType)
    {
        return materialType switch
        {
            StudyMaterialType.TermsAndDefinitions => "TermsAndDefinitions.md",
            StudyMaterialType.Equations => "Equations.md",
            StudyMaterialType.QuizContent => "QuizContent.md",
            _ => throw new ArgumentException($"Unknown material type: {materialType}", nameof(materialType))
        };
    }

    private static string GetCacheKey(string username, StudyMaterialType materialType)
    {
        return $"{materialType}_{username}";
    }

    private void InvalidateCache(string username, StudyMaterialType materialType)
    {
        var cacheKey = GetCacheKey(username, materialType);
        _cache.Remove(cacheKey);
        _logger.LogDebug("Cache invalidated for {Username}/{MaterialType}", username, materialType);
    }

    private static string ComputeHash(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private async Task<UserStudyMaterialMetadata> LoadMetadataAsync(string username)
    {
        var metadataPath = GetMetadataPath(username);

        if (!File.Exists(metadataPath))
        {
            _logger.LogInformation("Metadata file not found for user {Username}, creating new", username);
            return new UserStudyMaterialMetadata
            {
                Username = username,
                Materials = new(),
                EquationsEnabled = true // Default for new users
            };
        }

        try
        {
            var json = await File.ReadAllTextAsync(metadataPath);
            var metadata = JsonSerializer.Deserialize<UserStudyMaterialMetadata>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (metadata == null)
            {
                _logger.LogWarning("Metadata deserialization returned null for user {Username}", username);
                return new UserStudyMaterialMetadata
                {
                    Username = username,
                    Materials = new(),
                    EquationsEnabled = true
                };
            }

            // Backward compatibility: If Materials exist but EquationsEnabled is false,
            // this is likely an old metadata file without the property
            if (metadata.Materials.Any() && !metadata.EquationsEnabled)
            {
                _logger.LogInformation("Migrating old metadata for user {Username} to include EquationsEnabled", username);
                metadata.EquationsEnabled = true;
                // Save updated metadata
                var updatedJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(metadataPath, updatedJson);
            }

            return metadata;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize metadata for user {Username}", username);
            // Return default metadata on error (fail open)
            return new UserStudyMaterialMetadata
            {
                Username = username,
                Materials = new(),
                EquationsEnabled = true
            };
        }
    }

    public async Task<bool> GetEquationsEnabledAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }

        try
        {
            var metadata = await LoadMetadataAsync(username);
            _logger.LogDebug("Retrieved equations enabled status: {Enabled} for user {Username}", 
                metadata.EquationsEnabled, username);
            return metadata.EquationsEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving equations enabled status for user {Username}", username);
            // Fail open - return true (enabled) on error
            return true;
        }
    }

    public async Task SetEquationsEnabledAsync(string username, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }

        try
        {
            var metadata = await LoadMetadataAsync(username);
            metadata.EquationsEnabled = enabled;

            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var metadataPath = GetMetadataPath(username);
            var metadataFolder = Path.GetDirectoryName(metadataPath);

            if (!string.IsNullOrEmpty(metadataFolder))
            {
                Directory.CreateDirectory(metadataFolder);
            }

            await File.WriteAllTextAsync(metadataPath, json);

            _logger.LogInformation("User {Username} updated equations enabled to {Enabled}", username, enabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting equations enabled for user {Username}", username);
            throw new InvalidOperationException($"Failed to update equations setting for user {username}", ex);
        }
    }

    // -------------------------------------------------------------------------
    // Course-aware overloads (US-005, US-006)
    // All file I/O targets App_Data/{username}/{courseName}/ instead of the
    // legacy App_Data/StudyMaterials/{username}/ path.
    // -------------------------------------------------------------------------

    public Task<FileValidationResult> UploadTermsAsync(string username, string courseName, IFormFile file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(courseName);
        ArgumentNullException.ThrowIfNull(file);
        return UploadCourseMaterialAsync(username, courseName, file, StudyMaterialType.TermsAndDefinitions);
    }

    public Task<FileValidationResult> UploadEquationsAsync(string username, string courseName, IFormFile file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(courseName);
        ArgumentNullException.ThrowIfNull(file);
        return UploadCourseMaterialAsync(username, courseName, file, StudyMaterialType.Equations);
    }

    public async Task<List<UserStudyMaterial>> GetUserMaterialsAsync(string username, string courseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(courseName);

        try
        {
            var metadata = await LoadCourseMetadataAsync(username, courseName);
            return metadata.Materials;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading materials metadata for {Username}/{CourseName}", username, courseName);
            return [];
        }
    }

    public async Task<bool> DeleteUserMaterialAsync(string username, string courseName, StudyMaterialType materialType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(courseName);

        try
        {
            var filePath = GetCourseFilePath(username, courseName, materialType);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted {MaterialType} for {Username}/{CourseName}", materialType, username, courseName);
            }

            var materials = await GetUserMaterialsAsync(username, courseName);
            materials.RemoveAll(m => m.MaterialType == materialType);
            await SaveCourseMetadataAsync(username, courseName, materials);

            InvalidateCourseCache(username, courseName, materialType);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {MaterialType} for {Username}/{CourseName}", materialType, username, courseName);
            return false;
        }
    }

    public async Task<string> GetEffectiveFilePathAsync(string username, string courseName, StudyMaterialType materialType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(courseName);

        // 1. Course-specific upload
        var coursePath = GetCourseFilePath(username, courseName, materialType);
        if (File.Exists(coursePath))
        {
            _logger.LogDebug("Using course file for {Username}/{CourseName}/{MaterialType}", username, courseName, materialType);
            return coursePath;
        }

        // 2. Legacy upload (StudyMaterials folder) — migration window; read-only
        var legacyPath = GetCustomFilePath(username, materialType);
        if (File.Exists(legacyPath))
        {
            _logger.LogDebug("Falling back to legacy file for {Username}/{MaterialType}", username, materialType);
            return legacyPath;
        }

        // 3. Global default
        _logger.LogDebug("Using global default for {Username}/{MaterialType}", username, materialType);
        return GetDefaultFilePath(materialType);
    }

    public Task<bool> HasCustomMaterialAsync(string username, string courseName, StudyMaterialType materialType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(courseName);

        return Task.FromResult(File.Exists(GetCourseFilePath(username, courseName, materialType)));
    }

    public async Task<string?> GetDecryptedContentAsync(string username, string courseName, StudyMaterialType materialType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(courseName);

        var cacheKey = GetCourseCacheKey(username, courseName, materialType);

        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        var filePath = GetCourseFilePath(username, courseName, materialType);
        if (!File.Exists(filePath))
            return null;

        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            _cache.Set(cacheKey, content, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1)));
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading {MaterialType} for {Username}/{CourseName}", materialType, username, courseName);
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Course path helpers
    // -------------------------------------------------------------------------

    private string GetCourseFolder(string username, string courseName)
    {
        var safeUsername   = SanitizePathSegment(username);
        var safeCourseName = SanitizePathSegment(courseName);
        var folder = Path.GetFullPath(Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            safeUsername,
            safeCourseName
        ));
        AssertUnderRoot(folder);
        return folder;
    }

    private string GetCourseFilePath(string username, string courseName, StudyMaterialType materialType)
        => Path.Combine(GetCourseFolder(username, courseName), GetFileName(materialType));

    private string GetCourseMetadataPath(string username, string courseName)
        => Path.Combine(GetCourseFolder(username, courseName), "metadata.json");

    private static string GetCourseCacheKey(string username, string courseName, StudyMaterialType materialType)
        => $"{materialType}_{username}_{courseName}";

    private void InvalidateCourseCache(string username, string courseName, StudyMaterialType materialType)
        => _cache.Remove(GetCourseCacheKey(username, courseName, materialType));

    // -------------------------------------------------------------------------
    // Course-scoped upload / metadata helpers
    // -------------------------------------------------------------------------

    private async Task<FileValidationResult> UploadCourseMaterialAsync(
        string username, string courseName, IFormFile file, StudyMaterialType materialType)
    {
        try
        {
            if (file.Length > _maxFileSizeBytes)
            {
                _logger.LogWarning("File too large for {Username}/{CourseName}: {Size} bytes", username, courseName, file.Length);
                return new FileValidationResult { IsValid = false, Errors = ["File exceeds the maximum allowed size."] };
            }

            if (file.Length == 0)
            {
                _logger.LogWarning("Empty file uploaded by {Username}/{CourseName}", username, courseName);
                return new FileValidationResult { IsValid = false, Errors = ["File is empty."] };
            }

            using var stream = file.OpenReadStream();
            var markdownResult = await _validationService.ValidateMarkdownFileAsync(stream, file.FileName);
            if (!markdownResult.IsValid)
            {
                _logger.LogWarning("File validation failed for {Username}/{CourseName}: {Errors}",
                    username, courseName, string.Join(", ", markdownResult.Errors));
                return markdownResult;
            }

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            var formatResult = materialType == StudyMaterialType.TermsAndDefinitions
                ? await _validationService.ValidateTermsFormatAsync(content)
                : await _validationService.ValidateEquationsFormatAsync(content);

            if (!formatResult.IsValid)
            {
                _logger.LogWarning("{MaterialType} format validation failed for {Username}/{CourseName}: {Errors}",
                    materialType, username, courseName, string.Join(", ", formatResult.Errors));
                return formatResult;
            }

            if (formatResult.Warnings.Count > 0)
                _logger.LogInformation("{MaterialType} format warnings for {Username}/{CourseName}: {Warnings}",
                    materialType, username, courseName, string.Join(", ", formatResult.Warnings));

            var securityResult = await _validationService.ScanForMaliciousContentAsync(content);
            if (!securityResult.IsValid)
            {
                _logger.LogError("Security scan failed for {Username}/{CourseName}: {Errors}",
                    username, courseName, string.Join(", ", securityResult.Errors));
                return securityResult;
            }

            var courseFolder = GetCourseFolder(username, courseName);
            Directory.CreateDirectory(courseFolder);

            var filePath = GetCourseFilePath(username, courseName, materialType);
            await File.WriteAllTextAsync(filePath, content);

            await SaveCourseUploadMetadataAsync(username, courseName, materialType, file, content);
            InvalidateCourseCache(username, courseName, materialType);

            _logger.LogInformation("{Username}/{CourseName} uploaded {MaterialType} ({Size} bytes)",
                username, courseName, materialType, file.Length);

            // Return formatResult so counts are available to the controller.
            return formatResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading {MaterialType} for {Username}/{CourseName}", materialType, username, courseName);
            return new FileValidationResult { IsValid = false, Errors = ["An unexpected error occurred during upload."] };
        }
    }

    private async Task SaveCourseUploadMetadataAsync(
        string username, string courseName, StudyMaterialType materialType, IFormFile file, string content)
    {
        var materials = await GetUserMaterialsAsync(username, courseName);
        materials.RemoveAll(m => m.MaterialType == materialType);
        materials.Add(new UserStudyMaterial
        {
            Username       = username,
            MaterialType   = materialType,
            FileName       = file.FileName,
            FileSizeBytes  = file.Length,
            UploadedDate   = DateTime.UtcNow,
            FilePath       = GetCourseFilePath(username, courseName, materialType),
            FileHash       = ComputeHash(content)
        });
        await SaveCourseMetadataAsync(username, courseName, materials);
    }

    private async Task<UserStudyMaterialMetadata> LoadCourseMetadataAsync(string username, string courseName)
    {
        var metadataPath = GetCourseMetadataPath(username, courseName);

        if (!File.Exists(metadataPath))
            return new UserStudyMaterialMetadata { Username = username, Materials = [], EquationsEnabled = true };

        try
        {
            var json = await File.ReadAllTextAsync(metadataPath);
            return JsonSerializer.Deserialize<UserStudyMaterialMetadata>(json)
                ?? new UserStudyMaterialMetadata { Username = username, Materials = [], EquationsEnabled = true };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize course metadata for {Username}/{CourseName}", username, courseName);
            return new UserStudyMaterialMetadata { Username = username, Materials = [], EquationsEnabled = true };
        }
    }

    private async Task SaveCourseMetadataAsync(string username, string courseName, List<UserStudyMaterial> materials)
    {
        var existing = await LoadCourseMetadataAsync(username, courseName);
        existing.Materials = materials;

        var json = JsonSerializer.Serialize(existing, new JsonSerializerOptions { WriteIndented = true });
        var metadataPath = GetCourseMetadataPath(username, courseName);
        Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
        await File.WriteAllTextAsync(metadataPath, json);
    }

    // -------------------------------------------------------------------------
    // Path-safety helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Strips any directory-traversal characters from a single path segment
    /// (username or course name) so it cannot escape the App_Data root.
    /// Replaces path separators, null bytes, and leading dots/spaces; rejects
    /// empty results with an ArgumentException.
    /// </summary>
    private static string SanitizePathSegment(string segment)
    {
        // Remove directory-separator characters, null bytes, and colon (Windows drive letters)
        var safe = segment
            .Replace("/",  string.Empty)
            .Replace("\\", string.Empty)
            .Replace("\0", string.Empty)
            .Replace(":",  string.Empty)
            .Trim();

        // Collapse any remaining leading dots to prevent ".." after trim
        safe = safe.TrimStart('.');

        if (string.IsNullOrWhiteSpace(safe))
            throw new ArgumentException(
                $"Path segment '{segment}' is invalid or resolves to an empty value after sanitization.",
                nameof(segment));

        return safe;
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if <paramref name="resolvedPath"/>
    /// does not start with the expected App_Data root, preventing any residual
    /// traversal that slipped past <see cref="SanitizePathSegment"/>.
    /// </summary>
    private void AssertUnderRoot(string resolvedPath)
    {
        var appDataRoot = Path.GetFullPath(
            Path.Combine(_environment.ContentRootPath, "App_Data"));

        if (!resolvedPath.StartsWith(appDataRoot, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Path traversal attempt blocked. Resolved path '{Path}' is outside App_Data root '{Root}'.",
                resolvedPath, appDataRoot);
            throw new InvalidOperationException(
                "Resolved path is outside the permitted storage root.");
        }
    }
}
