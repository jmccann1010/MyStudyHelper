using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Interface for parsing equation flashcards from markdown files.
/// </summary>
public interface IEquationFlashcardParserService
{
    /// <summary>
    /// Parses Equations.md for a user, using the course-specific file when both
    /// username and courseName are supplied, then falling back through legacy and global defaults.
    /// </summary>
    Task<List<EquationFlashcard>> ParseEquationsAsync(string? username = null, string? courseName = null);
}
