namespace StudyHelper.ViewModels;

/// <summary>
/// View model for displaying an equation flashcard.
/// </summary>
public class EquationFlashcardViewModel
{
    /// <summary>
    /// Gets or sets the name of the equation.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the summary description.
    /// </summary>
    public required string Summary { get; set; }

    /// <summary>
    /// Gets or sets the left side (prompt).
    /// </summary>
    public required string LeftSide { get; set; }

    /// <summary>
    /// Gets or sets the right side (answer).
    /// </summary>
    public required string RightSide { get; set; }
}
