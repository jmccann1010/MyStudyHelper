using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service for parsing equations from Equations.md with LaTeX notation.
/// </summary>
public interface IEquationParserService
{
    /// <summary>
    /// Parses Equations.md for a user, using the course-specific file when both
    /// username and courseName are supplied, then falling back through legacy and
    /// global defaults.
    /// </summary>
    Task<List<SubjectMatterEquation>> ParseEquationsAsync(string? username = null, string? courseName = null);
}
