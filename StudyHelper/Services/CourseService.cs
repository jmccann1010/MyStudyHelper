using StudyHelper.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StudyHelper.Services;

/// <summary>
/// File-backed course management service.
/// Each user's courses are stored in App_Data/{username}/course_settings.dat as a JSON array.
/// A per-user SemaphoreSlim serialises concurrent writes for the same user.
/// </summary>
public class CourseService(
    IWebHostEnvironment environment,
    IMemoryCache cache,
    ILogger<CourseService> logger) : ICourseService
{
    private const int MaxCourses = 10;
    private const string SettingsFileName = "course_settings.dat";
    private const string CacheKeyPrefix = "CourseSettings_";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    // Valid course name: letters, numbers, hyphens, underscores — no spaces
    private static readonly Regex ValidCourseNamePattern = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    // Per-user write locks — prevents concurrent writes corrupting course_settings.dat
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _userLocks = new();

    /// <inheritdoc/>
    public async Task<List<Course>> GetCoursesAsync(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var cacheKey = CacheKeyPrefix + username;
        if (cache.TryGetValue(cacheKey, out List<Course>? cached) && cached != null)
            return cached;

        var courses = await ReadCoursesFromFileAsync(username);
        cache.Set(cacheKey, courses, CacheExpiration);
        return courses;
    }

    /// <inheritdoc/>
    public async Task<Course?> GetActiveCourseAsync(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var courses = await GetCoursesAsync(username);
        return courses.FirstOrDefault(c => c.IsActive);
    }

    /// <inheritdoc/>
    public async Task<bool> AddCourseAsync(string username, string courseName, string instructor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(courseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(instructor);

        // Reject names that would produce unsafe directory paths
        if (!ValidCourseNamePattern.IsMatch(courseName))
            throw new ArgumentException(
                "Course name may only contain letters, numbers, hyphens, and underscores.",
                nameof(courseName));

        var sem = GetUserLock(username);
        await sem.WaitAsync();
        try
        {
            var courses = await ReadCoursesFromFileAsync(username);

            if (courses.Count >= MaxCourses)
            {
                logger.LogWarning("User {Username} attempted to exceed the {Max}-course limit", username, MaxCourses);
                return false;
            }

            // Case-insensitive duplicate check — prevents Biology / biology colliding on Windows
            if (courses.Any(c => c.CourseName.Equals(courseName, StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogWarning("User {Username} attempted to add duplicate course '{CourseName}'", username, courseName);
                return false;
            }

            var now = DateTime.UtcNow;
            courses.Add(new Course
            {
                Username    = username,
                CourseName  = courseName,
                Instructor  = instructor,
                CreatedDate = now,
                UpdatedDate = now,
                IsActive    = courses.Count == 0  // Auto-activate when this is the user's first course
            });

            await SaveCoursesAsync(username, courses);

            // Create the course directory immediately so file uploads have a target
            var courseDir = GetCourseDirectory(username, courseName);
            Directory.CreateDirectory(courseDir);

            logger.LogInformation("Created course '{CourseName}' for user {Username}", courseName, username);
            return true;
        }
        finally
        {
            sem.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetActiveCourseAsync(string username, string courseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(courseName);

        var sem = GetUserLock(username);
        await sem.WaitAsync();
        try
        {
            var courses = await ReadCoursesFromFileAsync(username);

            // Clear all active flags then set the one matching courseName
            foreach (var c in courses)
                c.IsActive = c.CourseName.Equals(courseName, StringComparison.OrdinalIgnoreCase);

            await SaveCoursesAsync(username, courses);
            logger.LogInformation("User {Username} set active course to '{CourseName}'", username, courseName);
        }
        finally
        {
            sem.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveCourseAsync(string username, string courseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(courseName);

        var sem = GetUserLock(username);
        await sem.WaitAsync();
        try
        {
            var courses = await ReadCoursesFromFileAsync(username);
            var toRemove = courses.FirstOrDefault(c =>
                c.CourseName.Equals(courseName, StringComparison.OrdinalIgnoreCase));

            if (toRemove == null)
            {
                logger.LogWarning("User {Username} attempted to remove non-existent course '{CourseName}'", username, courseName);
                return false;
            }

            courses.Remove(toRemove);
            await SaveCoursesAsync(username, courses);

            // Delete the course directory and all its contents
            var courseDir = GetCourseDirectory(username, courseName);
            if (Directory.Exists(courseDir))
            {
                Directory.Delete(courseDir, recursive: true);
                logger.LogInformation("Deleted course directory '{CourseDir}'", courseDir);
            }

            logger.LogInformation("Removed course '{CourseName}' for user {Username}", courseName, username);
            return true;
        }
        finally
        {
            sem.Release();
        }
    }

    /// <inheritdoc/>
    public string GetCourseDirectory(string username, string courseName)
    {
        // Build the candidate path then assert it is rooted under App_Data/{username}/
        // to guard against any path-traversal attempt that slipped through validation.
        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", username));
        var candidate = Path.GetFullPath(Path.Combine(root, courseName));

        if (!candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !candidate.Equals(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Resolved course directory '{candidate}' is outside the expected root '{root}'.");
        }

        return candidate;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<List<Course>> ReadCoursesFromFileAsync(string username)
    {
        var filePath = GetSettingsFilePath(username);

        if (!File.Exists(filePath))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<Course>>(json) ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize course_settings.dat for user {Username}", username);
            return [];
        }
    }

    /// <summary>
    /// Serialises the course list to disk and invalidates the memory cache.
    /// Callers MUST hold the per-user semaphore before calling this method.
    /// </summary>
    private async Task SaveCoursesAsync(string username, List<Course> courses)
    {
        var filePath = GetSettingsFilePath(username);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var json = JsonSerializer.Serialize(courses, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);

        // Invalidate and immediately refresh the cache
        cache.Remove(CacheKeyPrefix + username);
        cache.Set(CacheKeyPrefix + username, courses, CacheExpiration);
    }

    private string GetSettingsFilePath(string username)
        => Path.Combine(environment.ContentRootPath, "App_Data", username, SettingsFileName);

    private static SemaphoreSlim GetUserLock(string username)
        => _userLocks.GetOrAdd(username, _ => new SemaphoreSlim(1, 1));
}
