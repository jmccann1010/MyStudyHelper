using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service for managing graded exercise sessions, score tracking, and session lifecycle.
/// </summary>
public interface IGradedExerciseService
{
    /// <summary>
    /// Starts a new graded exercise session with specified problem count.
    /// </summary>
    /// <param name="problemCount">Number of problems in the session (1-50).</param>
    /// <param name="username">Authenticated username for session scoping.</param>
    /// <returns>Exercise session ID for future references.</returns>
    /// <exception cref="ArgumentException">Thrown when problemCount is outside 1-50 range or username is null/empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when unable to generate problems (no equations, insufficient equations, etc.).</exception>
    Task<string> StartExerciseAsync(int problemCount, string username);

    /// <summary>
    /// Submits an answer for the current problem and advances exercise state.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="userAnswer">User's submitted answer (decimal as string).</param>
    /// <returns>Result containing validation status, correctness, and updated progress.</returns>
    /// <exception cref="InvalidOperationException">Thrown when session not found or expired.</exception>
    /// <exception cref="ArgumentException">Thrown when answer format is invalid.</exception>
    Task<SubmitAnswerResult> SubmitAnswerAsync(string sessionId, string userAnswer);

    /// <summary>
    /// Gets current score state without modifying session.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>Current exercise score.</returns>
    /// <exception cref="InvalidOperationException">Thrown when session not found.</exception>
    Task<ExerciseScore> GetCurrentScoreAsync(string sessionId);

    /// <summary>
    /// Gets exercise progress and current problem information.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>Current exercise progress.</returns>
    /// <exception cref="InvalidOperationException">Thrown when session not found.</exception>
    Task<ExerciseProgress> GetExerciseProgressAsync(string sessionId);

    /// <summary>
    /// Retrieves the full exercise session for rendering.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>Full exercise session or null if not found/expired.</returns>
    Task<GradedExerciseSession?> GetExerciseSessionAsync(string sessionId);

    /// <summary>
    /// Finalizes exercise session and returns final score.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>Final exercise score with performance rating.</returns>
    /// <exception cref="InvalidOperationException">Thrown when session not found.</exception>
    Task<ExerciseScore> FinishExerciseAsync(string sessionId);

    /// <summary>
    /// Clears exercise session (e.g., for retake).
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>True if session was cleared, false if not found.</returns>
    Task<bool> ClearExerciseSessionAsync(string sessionId);
}
