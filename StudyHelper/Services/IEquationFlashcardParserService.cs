using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Interface for parsing equation flashcards from markdown files.
/// </summary>
public interface IEquationFlashcardParserService
{
    /// <summary>
    /// Parses the Equations.md file and extracts all equations for flashcard display.
    /// Uses custom user-uploaded file if available, otherwise falls back to default.
    /// </summary>
    /// <param name="username">Optional username to check for custom uploaded materials. If null, uses default file.</param>
    /// <returns>A list of equation flashcards.</returns>
    Task<List<EquationFlashcard>> ParseEquationsAsync(string? username = null);
}
