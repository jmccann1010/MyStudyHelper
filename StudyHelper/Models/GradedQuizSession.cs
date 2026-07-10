namespace StudyHelper.Models;

/// <summary>
/// Represents an active graded quiz session with user answers and score tracking.
/// </summary>
public class GradedQuizSession
{
    /// <summary>
    /// Unique session identifier.
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Authenticated username (for multi-user scoping).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Total questions in this quiz.
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Current question index (0-based).
    /// </summary>
    public int CurrentQuestionIndex { get; set; }

    /// <summary>
    /// List of generated questions for this session.
    /// </summary>
    public List<QuizQuestion> Questions { get; set; } = new();

    /// <summary>
    /// User's answer selections (index per question, or -1 if unanswered).
    /// </summary>
    public Dictionary<int, int> UserAnswers { get; set; } = new();

    /// <summary>
    /// Session creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last activity timestamp (for timeout detection).
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether quiz is complete.
    /// </summary>
    public bool IsComplete { get; set; }
}
