namespace StudyHelper.Models;

/// <summary>
/// Represents the progress state of a graded quiz.
/// </summary>
public class QuizProgress
{
    /// <summary>
    /// Current question number (1-based for display).
    /// </summary>
    public int CurrentQuestionNumber { get; set; }

    /// <summary>
    /// Total questions in the quiz.
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Number of correct answers so far.
    /// </summary>
    public int CorrectCount { get; set; }

    /// <summary>
    /// Number of incorrect answers so far.
    /// </summary>
    public int IncorrectCount { get; set; }

    /// <summary>
    /// Gets the quiz completion percentage (0-100).
    /// </summary>
    public decimal ProgressPercentage => TotalQuestions > 0
        ? (decimal)(CurrentQuestionNumber - 1) / TotalQuestions * 100
        : 0;
}
