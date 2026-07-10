namespace StudyHelper.ViewModels;

/// <summary>
/// ViewModel for displaying the result of an exercise submission.
/// </summary>
public class ExerciseResultViewModel
{
    /// <summary>
    /// Whether the user's answer was correct.
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// The user's submitted answer.
    /// </summary>
    public decimal UserAnswer { get; set; }

    /// <summary>
    /// The correct answer.
    /// </summary>
    public decimal CorrectAnswer { get; set; }

    /// <summary>
    /// Feedback message to display to the user.
    /// </summary>
    public string FeedbackMessage { get; set; } = string.Empty;

    /// <summary>
    /// Step-by-step solution explanation.
    /// </summary>
    public string SolutionSteps { get; set; } = string.Empty;

    /// <summary>
    /// The original problem text.
    /// </summary>
    public string ProblemText { get; set; } = string.Empty;

    /// <summary>
    /// The module this exercise was from.
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the result is a ratio (vs currency).
    /// </summary>
    public bool IsRatioResult { get; set; } = false;
}
