namespace StudyHelper.ViewModels;

/// <summary>
/// ViewModel for displaying an exercise problem.
/// </summary>
public class ExerciseProblemViewModel
{
    /// <summary>
    /// The formatted problem text to display to the user.
    /// </summary>
    public string ProblemText { get; set; } = string.Empty;

    /// <summary>
    /// The module this exercise is from.
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// The name of the equation (e.g., "Net Income", "Current Ratio").
    /// </summary>
    public string EquationName { get; set; } = string.Empty;

    /// <summary>
    /// The topic/category and summary description of the equation.
    /// </summary>
    public string EquationSummary { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the expected answer is a ratio (vs currency amount).
    /// </summary>
    public bool IsRatioResult { get; set; } = false;
}
