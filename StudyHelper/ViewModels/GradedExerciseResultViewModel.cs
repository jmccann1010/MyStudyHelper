using StudyHelper.Models;

namespace StudyHelper.ViewModels;

/// <summary>
/// ViewModel for displaying graded exercise results with score and problem review.
/// </summary>
public class GradedExerciseResultViewModel
{
    /// <summary>
    /// Number of problems answered correctly.
    /// </summary>
    public int CorrectCount { get; set; }

    /// <summary>
    /// Number of problems answered incorrectly.
    /// </summary>
    public int IncorrectCount { get; set; }

    /// <summary>
    /// Total problems in the exercise.
    /// </summary>
    public int TotalProblems { get; set; }

    /// <summary>
    /// Percentage score (0-100).
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Performance rating (Excellent, Good, Fair, Poor, Needs Improvement).
    /// </summary>
    public string PerformanceRating { get; set; } = string.Empty;

    /// <summary>
    /// All problems from the exercise for review.
    /// </summary>
    public List<ExerciseProblem> Problems { get; set; } = new();

    /// <summary>
    /// User's answers (problem index to answer mapping).
    /// </summary>
    public Dictionary<int, decimal> UserAnswers { get; set; } = new();
}
