using StudyHelper.Models;

namespace StudyHelper.ViewModels;

/// <summary>
/// ViewModel for displaying a graded quiz question with score tracking.
/// Supports bidirectional questions (term→definition and definition→term).
/// </summary>
public class GradedQuizQuestionViewModel
{
    /// <summary>
    /// Current question number (1-based for display).
    /// </summary>
    public int QuestionNumber { get; set; }

    /// <summary>
    /// Total questions in the graded quiz.
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// The question text to display.
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Four answer options for the question.
    /// </summary>
    public List<string> AnswerOptions { get; set; } = new();

    /// <summary>
    /// Number of questions answered correctly so far.
    /// </summary>
    public int CorrectCount { get; set; }

    /// <summary>
    /// Number of questions answered incorrectly so far.
    /// </summary>
    public int IncorrectCount { get; set; }

    /// <summary>
    /// Gets or sets the direction of the question (term→definition or definition→term).
    /// </summary>
    public QuestionDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets the display label for the question direction.
    /// </summary>
    public string DirectionLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets the quiz completion percentage (0-100).
    /// </summary>
    public decimal ProgressPercentage => TotalQuestions > 0
        ? (decimal)(QuestionNumber - 1) / TotalQuestions * 100
        : 0;
}
