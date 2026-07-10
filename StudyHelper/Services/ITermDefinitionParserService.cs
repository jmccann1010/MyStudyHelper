using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Interface for parsing term definitions from markdown files.
/// </summary>
public interface ITermDefinitionParserService
{
    /// <summary>
    /// Parses the TermsAndDefinitions.md file and extracts all term/definition pairs.
    /// Uses custom user-uploaded file if available, otherwise falls back to default.
    /// </summary>
    /// <param name="username">Optional username to check for custom uploaded materials. If null, uses default file.</param>
    /// <returns>A list of term definitions.</returns>
    Task<List<TermDefinition>> ParseTermDefinitionsAsync(string? username = null);
}
