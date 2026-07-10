namespace StudyHelper.Models;

/// <summary>
/// Represents the result of validating a user's answer to a quiz question.
/// </summary>
public class QuizResult
{
    /// <summary>
    /// Gets or sets whether the user's answer was correct.
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Gets or sets the zero-based index of the correct answer (0-3).
    /// </summary>
    public int CorrectAnswerIndex { get; set; }

    /// <summary>
    /// Gets or sets the text of the correct answer.
    /// </summary>
    public string CorrectAnswerText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the explanation for why the correct answer is correct.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the zero-based index of the user's selected answer (0-3).
    /// </summary>
    public int UserAnswerIndex { get; set; }

    /// <summary>
    /// Gets or sets the text of the user's selected answer.
    /// </summary>
    public string UserAnswerText { get; set; } = string.Empty;
}
