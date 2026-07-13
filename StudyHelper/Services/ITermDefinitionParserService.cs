using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Interface for parsing term definitions from markdown files.
/// </summary>
public interface ITermDefinitionParserService
{
    /// <summary>
    /// Parses TermsAndDefinitions.md for a user, using the course-specific file when both
    /// username and courseName are supplied, then falling back through legacy and global defaults.
    /// </summary>
    Task<List<TermDefinition>> ParseTermDefinitionsAsync(string? username = null, string? courseName = null);
}
