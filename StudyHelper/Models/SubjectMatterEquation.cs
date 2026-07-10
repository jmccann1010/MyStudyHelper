namespace StudyHelper.Models;

/// <summary>
/// Represents a parsed equation from LaTeX markdown.
/// </summary>
public class SubjectMatterEquation
{
    /// <summary>
    /// Unique identifier generated from equation content.
    /// </summary>
    public string EquationId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the equation (e.g., "Assets = Liabilities + Equity").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly name of the equation from the markdown (e.g., "Net Income", "Current Ratio").
    /// For plain-text format, this is the first line. For LaTeX format, derived from DisplayName.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Module/section from markdown (e.g., "Fundamental Identity").
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// All variables in the equation.
    /// </summary>
    public List<string> Variables { get; set; } = new();

    /// <summary>
    /// The cleaned equation formula (LaTeX notation removed).
    /// </summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>
    /// Original LaTeX formula for reference.
    /// </summary>
    public string LatexFormula { get; set; } = string.Empty;

    /// <summary>
    /// Equation type classification.
    /// </summary>
    public EquationType Type { get; set; }

    /// <summary>
    /// Left side variable (what equation solves for by default).
    /// </summary>
    public string LeftSide { get; set; } = string.Empty;

    /// <summary>
    /// Right side expression components.
    /// </summary>
    public List<EquationTerm> RightSideTerms { get; set; } = new();

    /// <summary>
    /// Context/explanation from markdown.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Source tag: [Source] or [Inferred].
    /// </summary>
    public string SourceTag { get; set; } = string.Empty;
}
