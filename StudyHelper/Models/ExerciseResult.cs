namespace StudyHelper.Models;

/// <summary>
/// Represents the result of answer validation.
/// </summary>
public class ExerciseResult
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
    /// The original problem.
    /// </summary>
    public ExerciseProblem Problem { get; set; } = new();

    /// <summary>
    /// Detailed feedback message.
    /// </summary>
    public string FeedbackMessage { get; set; } = string.Empty;

    /// <summary>
    /// Solution steps explanation.
    /// </summary>
    public string SolutionSteps { get; set; } = string.Empty;
}
