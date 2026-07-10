namespace StudyHelper.Models;

/// <summary>
/// Represents a single term in an equation's right side.
/// </summary>
public class EquationTerm
{
    /// <summary>
    /// The variable name in this term.
    /// </summary>
    public string Variable { get; set; } = string.Empty;

    /// <summary>
    /// The operator preceding this term (+, -, *, /).
    /// </summary>
    public string Operator { get; set; } = "+";

    /// <summary>
    /// Indicates if this term is in the numerator of a fraction.
    /// </summary>
    public bool IsNumerator { get; set; } = true;

    /// <summary>
    /// Indicates if this term is in the denominator of a fraction.
    /// </summary>
    public bool IsDenominator { get; set; } = false;
}
