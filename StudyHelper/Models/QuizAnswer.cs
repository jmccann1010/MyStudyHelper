namespace StudyHelper.Models;

/// <summary>
/// Represents a user's answer submission to a quiz question.
/// </summary>
public class QuizAnswer
{
    /// <summary>
    /// Gets or sets the zero-based index of the answer selected by the user (0-3).
    /// Maps to answer choices A=0, B=1, C=2, D=3.
    /// </summary>
    public int SelectedAnswerIndex { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the answer was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; }
}
