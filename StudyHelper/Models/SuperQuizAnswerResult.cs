using StudyHelper.ViewModels;

namespace StudyHelper.Models;

/// <summary>
/// Result of submitting an answer in Super Quiz.
/// </summary>
public class SuperQuizAnswerResult
{
    public bool IsCorrect { get; set; }
    public string CorrectAnswerText { get; set; } = string.Empty;
    public string UserAnswerText { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Next action after this answer.
    /// </summary>
    public SuperQuizNextAction NextAction { get; set; }

    /// <summary>
    /// Session progress after this answer.
    /// </summary>
    public SuperQuizProgress Progress { get; set; } = new();
}

/// <summary>
/// Indicates what should happen next in the quiz flow.
/// </summary>
public enum SuperQuizNextAction
{
    /// <summary>Continue to next question in current round.</summary>
    NextQuestion,

    /// <summary>Current round complete, show round summary.</summary>
    RoundComplete,

    /// <summary>All questions mastered, show completion summary.</summary>
    QuizComplete
}
