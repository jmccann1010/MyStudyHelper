using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service for managing user-uploaded study materials.
/// </summary>
public interface IUserStudyMaterialService
{
    /// <summary>
    /// Upload a TermsAndDefinitions.md file for a user.
    /// Returns a <see cref="FileValidationResult"/> containing counts and any errors.
    /// </summary>
    Task<FileValidationResult> UploadTermsAsync(string username, IFormFile file);

    /// <summary>
    /// Upload an Equations.md file for a user.
    /// Returns a <see cref="FileValidationResult"/> containing counts and any errors.
    /// </summary>
    Task<FileValidationResult> UploadEquationsAsync(string username, IFormFile file);

    /// <summary>
    /// Get metadata for all user-uploaded materials.
    /// </summary>
    Task<List<UserStudyMaterial>> GetUserMaterialsAsync(string username);

    /// <summary>
    /// Delete a user's uploaded study material.
    /// </summary>
    Task<bool> DeleteUserMaterialAsync(string username, StudyMaterialType materialType);

    /// <summary>
    /// Get the effective file path for a material type (custom if exists, otherwise default).
    /// </summary>
    Task<string> GetEffectiveFilePathAsync(string username, StudyMaterialType materialType);

    /// <summary>
    /// Check if user has uploaded a specific material type.
    /// </summary>
    Task<bool> HasCustomMaterialAsync(string username, StudyMaterialType materialType);

    /// <summary>
    /// Get content for a custom material if it exists.
    /// </summary>
    Task<string?> GetDecryptedContentAsync(string username, StudyMaterialType materialType);

    /// <summary>
    /// Gets whether equation-based features are enabled for the specified user.
    /// Returns true if the preference is not set (default/backward compatible behavior).
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <returns>True if equations are enabled, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown if username is null or empty.</exception>
    Task<bool> GetEquationsEnabledAsync(string username);

    /// <summary>
    /// Sets whether equation-based features are enabled for the specified user.
    /// Updates the user's metadata file with the new preference.
    /// </summary>
    /// <param name="username">The username to update.</param>
    /// <param name="enabled">True to enable equations, false to disable.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if username is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if metadata cannot be saved.</exception>
    Task SetEquationsEnabledAsync(string username, bool enabled);

    // -------------------------------------------------------------------------
    // Course-aware overloads (US-005, US-006)
    // When courseName is supplied, all file I/O targets App_Data/{username}/{courseName}/.
    // The username-only overloads above remain for legacy/fallback use.
    // -------------------------------------------------------------------------

    /// <summary>Upload a TermsAndDefinitions.md file into a specific course directory.</summary>
    Task<FileValidationResult> UploadTermsAsync(string username, string courseName, IFormFile file);

    /// <summary>Upload an Equations.md file into a specific course directory.</summary>
    Task<FileValidationResult> UploadEquationsAsync(string username, string courseName, IFormFile file);

    /// <summary>Get metadata for all materials uploaded to a specific course.</summary>
    Task<List<UserStudyMaterial>> GetUserMaterialsAsync(string username, string courseName);

    /// <summary>Delete a material from a specific course directory.</summary>
    Task<bool> DeleteUserMaterialAsync(string username, string courseName, StudyMaterialType materialType);

    /// <summary>
    /// Returns the effective file path for a material within a course:
    ///   1. App_Data/{username}/{courseName}/{file}  (course upload)
    ///   2. App_Data/StudyMaterials/{username}/{file} (legacy upload)
    ///   3. App_Data/{file}                           (global default)
    /// </summary>
    Task<string> GetEffectiveFilePathAsync(string username, string courseName, StudyMaterialType materialType);

    /// <summary>Returns true when a course-specific upload exists for the material type.</summary>
    Task<bool> HasCustomMaterialAsync(string username, string courseName, StudyMaterialType materialType);

    /// <summary>Returns the content of a course-specific material, or null when absent.</summary>
    Task<string?> GetDecryptedContentAsync(string username, string courseName, StudyMaterialType materialType);
}
