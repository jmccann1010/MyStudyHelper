using StudyHelper.Models;
using StudyHelper.ViewModels;

namespace StudyHelper.Services;

/// <summary>
/// Service for managing Super Quiz sessions with mastery-based retry logic.
/// </summary>
public interface ISuperQuizService
{
    /// <summary>
    /// Starts a new Super Quiz session with the specified question count.
    /// </summary>
    /// <param name="username">Authenticated username for session scoping.</param>
    /// <param name="questionCount">
    /// Number of questions to include in the quiz.
    /// Use <see cref="SuperQuizStartViewModel.AllQuestionsIndicator"/> to include all available terms.
    /// </param>
    /// <returns>Session ID for future references.</returns>
    /// <exception cref="ArgumentException">Username is null or empty, or question count is invalid.</exception>
    /// <exception cref="InvalidOperationException">
    /// No study materials found, insufficient content, or question count exceeds available terms.
    /// </exception>
    Task<string> StartSuperQuizAsync(
        string username,
        int questionCount = SuperQuizStartViewModel.AllQuestionsIndicator,
        string? courseName = null);

    /// <summary>
    /// Gets the current question for the user to answer.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>Current question or null if session not found.</returns>
    Task<QuizQuestion?> GetCurrentQuestionAsync(string sessionId);

    /// <summary>
    /// Submits an answer and advances session state.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="selectedAnswerIndex">0-based index of selected answer (0-3).</param>
    /// <returns>Result containing validation and next action.</returns>
    /// <exception cref="InvalidOperationException">Session not found or invalid answer index.</exception>
    Task<SuperQuizAnswerResult> SubmitAnswerAsync(string sessionId, int selectedAnswerIndex);

    /// <summary>
    /// Gets current progress information.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>Progress state or null if session not found.</returns>
    Task<SuperQuizProgress?> GetProgressAsync(string sessionId);

    /// <summary>
    /// Gets summary for the just-completed round.
    /// Called when transitioning between rounds.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>Round summary or null if session not found.</returns>
    Task<RoundSummary?> GetLastRoundSummaryAsync(string sessionId);

    /// <summary>
    /// Starts the next round of missed questions.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <exception cref="InvalidOperationException">Session not found or no missed questions.</exception>
    Task StartNextRoundAsync(string sessionId);

    /// <summary>
    /// Gets completion summary with all round statistics.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>Completion summary or null if session not found or not complete.</returns>
    Task<SuperQuizCompletionSummary?> GetCompletionSummaryAsync(string sessionId);

    /// <summary>
    /// Validates that the session belongs to the specified user.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="username">Username to validate.</param>
    /// <returns>True if session exists and belongs to user.</returns>
    Task<bool> ValidateSessionOwnershipAsync(string sessionId, string username);
}
