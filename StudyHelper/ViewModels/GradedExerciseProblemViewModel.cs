namespace StudyHelper.ViewModels;

/// <summary>
/// ViewModel for displaying a graded exercise problem with score tracking.
/// </summary>
public class GradedExerciseProblemViewModel
{
    /// <summary>
    /// Current problem number (1-based for display).
    /// </summary>
    public int ProblemNumber { get; set; }

    /// <summary>
    /// Total problems in the graded exercise session.
    /// </summary>
    public int TotalProblems { get; set; }

    /// <summary>
    /// The problem text to display.
    /// </summary>
    public string ProblemText { get; set; } = string.Empty;

    /// <summary>
    /// Given values for the problem (variable name to value mapping).
    /// </summary>
    public Dictionary<string, decimal> GivenValues { get; set; } = new();

    /// <summary>
    /// The variable the user needs to solve for.
    /// </summary>
    public string SolveForVariable { get; set; } = string.Empty;

    /// <summary>
    /// Number of problems answered correctly so far.
    /// </summary>
    public int CorrectCount { get; set; }

    /// <summary>
    /// Number of problems answered incorrectly so far.
    /// </summary>
    public int IncorrectCount { get; set; }

    /// <summary>
    /// Gets the exercise completion percentage (0-100).
    /// </summary>
    public decimal ProgressPercentage => TotalProblems > 0
        ? (decimal)(ProblemNumber - 1) / TotalProblems * 100
        : 0;
}
