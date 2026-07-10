namespace StudyHelper.Models;

/// <summary>
/// Represents a generated calculation problem with values and solution.
/// </summary>
public class ExerciseProblem
{
    /// <summary>
    /// The equation this problem is based on.
    /// </summary>
    public SubjectMatterEquation Equation { get; set; } = new();

    /// <summary>
    /// Generated values for each variable (except the one being solved for).
    /// </summary>
    public Dictionary<string, decimal> GivenValues { get; set; } = new();

    /// <summary>
    /// The variable the user needs to solve for.
    /// </summary>
    public string SolveForVariable { get; set; } = string.Empty;

    /// <summary>
    /// The calculated correct answer.
    /// </summary>
    public decimal CorrectAnswer { get; set; }

    /// <summary>
    /// Formatted problem text for display.
    /// </summary>
    public string ProblemText { get; set; } = string.Empty;

    /// <summary>
    /// Step-by-step solution explanation.
    /// </summary>
    public string SolutionSteps { get; set; } = string.Empty;

    /// <summary>
    /// Module source for display.
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the result is a ratio (vs currency).
    /// </summary>
    public bool IsRatioResult { get; set; } = false;
}
