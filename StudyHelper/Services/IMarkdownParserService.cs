using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service interface for parsing markdown files and extracting structured content.
/// </summary>
public interface IMarkdownParserService
{
    /// <summary>
    /// Parses markdown files for a user, using the course-specific file when both
    /// username and courseName are supplied, then falling back through legacy and
    /// global defaults.
    /// </summary>
    Task<List<MarkdownSection>> ParseMarkdownFilesAsync(string? username = null, string? courseName = null);
}
