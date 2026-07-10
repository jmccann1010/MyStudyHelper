using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service for managing graded quiz sessions, score tracking, and session lifecycle.
/// </summary>
public interface IGradedQuizService
{
    /// <summary>
    /// Starts a new graded quiz session with specified question count.
    /// </summary>
    /// <param name="questionCount">Number of questions in the quiz (1-50).</param>
    /// <param name="username">Authenticated username for session scoping.</param>
    /// <returns>Quiz session ID for future references.</returns>
    /// <exception cref="ArgumentException">Thrown when questionCount is outside 1-50 range or username is null/empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when unable to generate questions.</exception>
    Task<string> StartQuizAsync(int questionCount, string username);

    /// <summary>
    /// Submits an answer for the current question and advances quiz state.
    /// </summary>
    /// <param name="quizSessionId">Session ID.</param>
    /// <param name="selectedAnswerIndex">0-based index of selected answer (0-3).</param>
    /// <returns>Result containing validation status and updated progress.</returns>
    /// <exception cref="InvalidOperationException">Thrown when session not found or expired.</exception>
    Task<SubmitAnswerResult> SubmitAnswerAsync(string quizSessionId, int selectedAnswerIndex);

    /// <summary>
    /// Gets current score state without modifying session.
    /// </summary>
    /// <param name="quizSessionId">Session ID.</param>
    /// <returns>Current quiz score.</returns>
    /// <exception cref="InvalidOperationException">Thrown when session not found.</exception>
    Task<QuizScore> GetCurrentScoreAsync(string quizSessionId);

    /// <summary>
    /// Gets quiz progress and current question information.
    /// </summary>
    /// <param name="quizSessionId">Session ID.</param>
    /// <returns>Current quiz progress.</returns>
    /// <exception cref="InvalidOperationException">Thrown when session not found.</exception>
    Task<QuizProgress> GetQuizProgressAsync(string quizSessionId);

    /// <summary>
    /// Retrieves the full quiz session for rendering.
    /// </summary>
    /// <param name="quizSessionId">Session ID.</param>
    /// <returns>Full quiz session or null if not found/expired.</returns>
    Task<GradedQuizSession?> GetQuizSessionAsync(string quizSessionId);

    /// <summary>
    /// Finalizes quiz session and returns final score.
    /// </summary>
    /// <param name="quizSessionId">Session ID.</param>
    /// <returns>Final quiz score.</returns>
    /// <exception cref="InvalidOperationException">Thrown when session not found.</exception>
    Task<QuizScore> FinishQuizAsync(string quizSessionId);

    /// <summary>
    /// Clears quiz session (e.g., for retake).
    /// </summary>
    /// <param name="quizSessionId">Session ID.</param>
    /// <returns>True if session was cleared, false if not found.</returns>
    Task<bool> ClearQuizSessionAsync(string quizSessionId);
}
