using StudyHelper.Models;

namespace StudyHelper.ViewModels;

/// <summary>
/// ViewModel for displaying quiz results to the user.
/// Supports bidirectional questions (term→definition and definition→term).
/// </summary>
public class QuizResultViewModel
{
    /// <summary>
    /// Gets or sets whether the user's answer was correct.
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Gets or sets the feedback message ("Correct!" or "Incorrect").
    /// </summary>
    public string FeedbackMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text of the correct answer.
    /// </summary>
    public string CorrectAnswerText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text of the user's selected answer.
    /// </summary>
    public string UserAnswerText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the explanation for why the correct answer is correct.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the module name.
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the topic or heading.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the direction of the question (term→definition or definition→term).
    /// </summary>
    public QuestionDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets the display label for the question direction.
    /// </summary>
    public string DirectionLabel { get; set; } = string.Empty;
}
