using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service for validating uploaded study material files.
/// </summary>
public interface IFileValidationService
{
    /// <summary>
    /// Validate that the file is a proper markdown file.
    /// </summary>
    Task<FileValidationResult> ValidateMarkdownFileAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Validate that content contains only plain text/ASCII characters.
    /// </summary>
    Task<FileValidationResult> ValidatePlainTextAsync(string content);

    /// <summary>
    /// Scan file content for potentially malicious patterns.
    /// </summary>
    Task<FileValidationResult> ScanForMaliciousContentAsync(string content);

    /// <summary>
    /// Validate that Terms content follows expected markdown structure.
    /// </summary>
    Task<FileValidationResult> ValidateTermsFormatAsync(string content);

    /// <summary>
    /// Validate that Equations content follows expected LaTeX markdown structure.
    /// </summary>
    Task<FileValidationResult> ValidateEquationsFormatAsync(string content);
}
