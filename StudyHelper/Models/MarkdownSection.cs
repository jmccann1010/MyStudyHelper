namespace StudyHelper.Models;

/// <summary>
/// Represents a structured section of content parsed from a markdown file.
/// </summary>
public class MarkdownSection
{
    /// <summary>
    /// Gets or sets the module name from which this section was extracted (e.g., "Module1").
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the heading text that defines this section.
    /// </summary>
    public string Heading { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content lines associated with this section.
    /// </summary>
    public List<string> ContentLines { get; set; } = new();

    /// <summary>
    /// Gets or sets the bullet points extracted from this section.
    /// </summary>
    public List<string> BulletPoints { get; set; } = new();

    /// <summary>
    /// Gets or sets the term-definition pairs extracted from this section (e.g., "Term: Definition" format).
    /// Key = term, Value = complete definition.
    /// </summary>
    public Dictionary<string, string> TermDefinitions { get; set; } = new();
}
