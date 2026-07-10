using StudyHelper.Models;

namespace StudyHelper.ViewModels;

/// <summary>
/// ViewModel for displaying graded quiz results with score and question review.
/// </summary>
public class GradedQuizResultViewModel
{
    /// <summary>
    /// Number of questions answered correctly.
    /// </summary>
    public int CorrectCount { get; set; }

    /// <summary>
    /// Number of questions answered incorrectly.
    /// </summary>
    public int IncorrectCount { get; set; }

    /// <summary>
    /// Total questions in the quiz.
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Percentage score (0-100).
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Performance rating (Excellent, Good, Fair, Poor, Needs Improvement).
    /// </summary>
    public string PerformanceRating { get; set; } = string.Empty;

    /// <summary>
    /// All questions from the quiz for review.
    /// </summary>
    public List<QuizQuestion> Questions { get; set; } = new();

    /// <summary>
    /// User's answers (question index to answer index mapping).
    /// </summary>
    public Dictionary<int, int> UserAnswers { get; set; } = new();
}
