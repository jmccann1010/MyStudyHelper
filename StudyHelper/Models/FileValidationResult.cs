namespace StudyHelper.Models;

/// <summary>
/// Result of file validation, including error/warning messages and parsed-content counts.
/// Counts are populated by the format-specific validation methods and flow up to the
/// controller so it can build a detailed success/warning message without re-parsing.
/// </summary>
public class FileValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];

    // US-001 / US-003 — populated by ValidateTermsFormatAsync
    /// <summary>Number of ## section headings found in the Terms file.</summary>
    public int ParsedSectionCount { get; set; }

    /// <summary>Number of valid Term: Definition pairs found in the Terms file.</summary>
    public int ParsedTermCount { get; set; }

    // US-002 / US-004 — populated by ValidateEquationsFormatAsync
    /// <summary>Number of complete, well-formed equation blocks found in the Equations file.</summary>
    public int ParsedEquationCount { get; set; }
}
