namespace StudyHelper.ViewModels;

/// <summary>
/// View model for displaying a flashcard.
/// </summary>
public class FlashcardViewModel
{
    /// <summary>
    /// Gets or sets the term.
    /// </summary>
    public required string Term { get; set; }

    /// <summary>
    /// Gets or sets the definition of the term.
    /// </summary>
    public required string Definition { get; set; }

    /// <summary>
    /// Gets or sets the section this term belongs to (optional).
    /// </summary>
    public string? Section { get; set; }
}
