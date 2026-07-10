namespace StudyHelper.Models;

/// <summary>
/// Represents the result of a submitted answer in a graded quiz or exercise.
/// </summary>
public class SubmitAnswerResult
{
    /// <summary>
    /// Whether the submission was valid (e.g., answer index in range or valid number format).
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Whether the submitted answer is correct.
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Error message if IsValid is false; otherwise null.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether this is the last question in the quiz.
    /// </summary>
    public bool IsLastQuestion { get; set; }

    /// <summary>
    /// Whether this is the last problem in the exercise.
    /// Alias for IsLastQuestion to support exercise terminology.
    /// </summary>
    public bool IsLastProblem
    {
        get => IsLastQuestion;
        set => IsLastQuestion = value;
    }

    /// <summary>
    /// Updated quiz progress after submission.
    /// </summary>
    public QuizProgress UpdatedProgress { get; set; } = new();

    /// <summary>
    /// The correct answer for exercise problems (decimal).
    /// </summary>
    public decimal CorrectAnswer { get; set; }
}
