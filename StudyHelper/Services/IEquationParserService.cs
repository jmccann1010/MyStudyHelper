using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service for parsing equations from Equations.md with LaTeX notation.
/// </summary>
public interface IEquationParserService
{
    /// <summary>
    /// Parses the Equations.md file and extracts all parseable equations.
    /// Uses custom user-uploaded file if available, otherwise falls back to default.
    /// Results are cached for performance.
    /// </summary>
    /// <param name="username">Optional username to check for custom uploaded materials. If null, uses default file.</param>
    /// <returns>A list of parsed equations.</returns>
    Task<List<SubjectMatterEquation>> ParseEquationsAsync(string? username = null);
}
