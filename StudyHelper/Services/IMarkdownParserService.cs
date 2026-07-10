using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service interface for parsing markdown files and extracting structured content.
/// </summary>
public interface IMarkdownParserService
{
    /// <summary>
    /// Parses all markdown files in the InputDocuments directory and extracts structured sections.
    /// Uses custom user-uploaded file if available, otherwise falls back to default.
    /// </summary>
    /// <param name="username">Optional username to check for custom uploaded materials.</param>
    /// <returns>A list of parsed markdown sections containing headings, content, and bullet points.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the InputDocuments directory is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no markdown files are found.</exception>
    Task<List<MarkdownSection>> ParseMarkdownFilesAsync(string? username = null);
}
