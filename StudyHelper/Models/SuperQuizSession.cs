namespace StudyHelper.Models;

/// <summary>
/// Represents an active Super Quiz session with mastery-based retry tracking.
/// Supports multi-round retries until all questions are answered correctly.
/// </summary>
public class SuperQuizSession
{
    /// <summary>
    /// Unique session identifier.
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Authenticated username for session scoping.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// All questions generated at session start (immutable after creation).
    /// </summary>
    public List<QuizQuestion> AllQuestions { get; set; } = new();

    /// <summary>
    /// Current round number (starts at 1).
    /// Round 1 = all questions, Round 2+ = missed questions from previous round.
    /// </summary>
    public int CurrentRound { get; set; } = 1;

    /// <summary>
    /// Set of question indices that have been answered correctly at least once.
    /// Uses indices into AllQuestions list.
    /// </summary>
    public HashSet<int> CorrectlyAnswered { get; set; } = new();

    /// <summary>
    /// Queue of questions for the current round.
    /// Dequeued as user progresses through questions.
    /// </summary>
    public Queue<QuizQuestion> CurrentRoundQueue { get; set; } = new();

    /// <summary>
    /// List of questions missed in the current round.
    /// Accumulated during round, used to populate next round queue.
    /// </summary>
    public List<QuizQuestion> MissedThisRound { get; set; } = new();

    /// <summary>
    /// Historical summary of each completed round.
    /// </summary>
    public List<RoundSummary> RoundHistory { get; set; } = new();

    /// <summary>
    /// Session creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last activity timestamp for timeout detection.
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether all questions have been answered correctly.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Total number of questions in this session.
    /// </summary>
    public int TotalQuestions => AllQuestions.Count;

    /// <summary>
    /// Number of questions remaining to master.
    /// </summary>
    public int RemainingToMaster => TotalQuestions - CorrectlyAnswered.Count;

    /// <summary>
    /// Number of questions left in current round.
    /// </summary>
    public int QuestionsLeftThisRound => CurrentRoundQueue.Count;
}
