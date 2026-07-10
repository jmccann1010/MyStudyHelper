namespace StudyHelper.Models;

/// <summary>
/// Represents an equation flashcard with name, summary, and equation sides.
/// </summary>
public class EquationFlashcard
{
    /// <summary>
    /// Gets or sets the name of the equation.
    /// Example: "The Equation"
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the summary description of the equation.
    /// Example: "Fundamental balance sheet identity..."
    /// </summary>
    public required string Summary { get; set; }

    /// <summary>
    /// Gets or sets the left side of the equation (the prompt).
    /// Example: "Assets"
    /// </summary>
    public required string LeftSide { get; set; }

    /// <summary>
    /// Gets or sets the right side of the equation (the answer).
    /// Example: "Liabilities + Equity"
    /// </summary>
    public required string RightSide { get; set; }
}
