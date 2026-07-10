namespace StudyHelper.Models;

/// <summary>
/// Represents the current progress through a graded exercise session.
/// </summary>
public class ExerciseProgress
{
    /// <summary>
    /// Current problem index (0-based).
    /// </summary>
    public int CurrentProblemIndex { get; set; }

    /// <summary>
    /// Total problems in the session.
    /// </summary>
    public int TotalProblems { get; set; }

    /// <summary>
    /// Number of problems answered correctly so far.
    /// </summary>
    public int CorrectCount { get; set; }

    /// <summary>
    /// Number of problems answered incorrectly so far.
    /// </summary>
    public int IncorrectCount { get; set; }

    /// <summary>
    /// Whether the session is complete.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Progress as a percentage (0-100).
    /// </summary>
    public decimal ProgressPercentage => TotalProblems > 0 
        ? Math.Round((decimal)CurrentProblemIndex / TotalProblems * 100, 2) 
        : 0;
}
