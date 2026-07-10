namespace StudyHelper.Models;

/// <summary>
/// Represents an active graded exercise session with user progress and scoring.
/// </summary>
public class GradedExerciseSession
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
    /// Total problems in this exercise session.
    /// </summary>
    public int TotalProblems { get; set; }

    /// <summary>
    /// Current problem index (0-based).
    /// </summary>
    public int CurrentProblemIndex { get; set; }

    /// <summary>
    /// List of generated problems for this session.
    /// </summary>
    public List<ExerciseProblem> Problems { get; set; } = new();

    /// <summary>
    /// User's answer submissions (problem index to answer mapping).
    /// </summary>
    public Dictionary<int, decimal> UserAnswers { get; set; } = new();

    /// <summary>
    /// Session creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last activity timestamp (for timeout detection).
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether exercise session is complete.
    /// </summary>
    public bool IsComplete { get; set; }
}
