namespace StudyHelper.Models;

/// <summary>
/// Represents the score and performance metrics for a graded exercise session.
/// </summary>
public class ExerciseScore
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
    /// Total number of problems in the exercise.
    /// </summary>
    public int TotalProblems { get; set; }

    /// <summary>
    /// Score as a percentage (0-100).
    /// </summary>
    public decimal Percentage => TotalProblems > 0 
        ? Math.Round((decimal)CorrectCount / TotalProblems * 100, 2) 
        : 0;

    /// <summary>
    /// Performance rating based on percentage.
    /// </summary>
    public string PerformanceRating { get; set; } = string.Empty;
}
