using StudyHelper.Models;
using Microsoft.Extensions.Caching.Memory;

namespace StudyHelper.Services;

/// <summary>
/// Service for managing graded quiz sessions, score tracking, and session lifecycle.
/// </summary>
public class GradedQuizService : IGradedQuizService
{
    private readonly IQuestionGeneratorService _questionGeneratorService;
    private readonly IMarkdownParserService _markdownParserService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GradedQuizService> _logger;

    private const int SessionTimeoutMinutes = 30;
    private const string CacheKeyPrefix = "graded-quiz-";

    public GradedQuizService(
        IQuestionGeneratorService questionGeneratorService,
        IMarkdownParserService markdownParserService,
        IMemoryCache cache,
        ILogger<GradedQuizService> logger)
    {
        _questionGeneratorService = questionGeneratorService ?? throw new ArgumentNullException(nameof(questionGeneratorService));
        _markdownParserService = markdownParserService ?? throw new ArgumentNullException(nameof(markdownParserService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> StartQuizAsync(int questionCount, string username, string? courseName = null)
    {
        // Validate inputs
        if (questionCount < 1 || questionCount > 50)
        {
            _logger.LogWarning("Invalid question count: {QuestionCount}", questionCount);
            throw new ArgumentException("Question count must be between 1 and 50.", nameof(questionCount));
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("Invalid username for quiz start");
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }

        try
        {
            _logger.LogInformation("Starting graded quiz for user {Username}/{Course} with {QuestionCount} questions",
                username, courseName ?? "no-course", questionCount);

            // Parse markdown sections using course-aware path when available
            var sections = await _markdownParserService.ParseMarkdownFilesAsync(username, courseName);

            // Generate questions
            var questions = new List<QuizQuestion>();
            for (int i = 0; i < questionCount; i++)
            {
                var question = _questionGeneratorService.GenerateQuestion(sections);
                questions.Add(question);
            }

            // Create session
            var sessionId = Guid.NewGuid().ToString();
            var session = new GradedQuizSession
            {
                SessionId = sessionId,
                Username = username,
                TotalQuestions = questionCount,
                Questions = questions,
                CurrentQuestionIndex = 0,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                IsComplete = false
            };

            // Store in cache with 30-minute expiration
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(SessionTimeoutMinutes));

            _cache.Set(GetCacheKey(sessionId), session, cacheOptions);

            _logger.LogInformation("Graded quiz session created: {SessionId} for user {Username}", sessionId, username);

            return sessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting graded quiz for user {Username}", username);
            throw;
        }
    }

    public async Task<SubmitAnswerResult> SubmitAnswerAsync(string quizSessionId, int selectedAnswerIndex)
    {
        // Retrieve session
        var session = await GetQuizSessionAsync(quizSessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found or expired: {SessionId}", quizSessionId);
            throw new InvalidOperationException("Quiz session not found or has expired.");
        }

        // Validate answer index
        if (selectedAnswerIndex < 0 || selectedAnswerIndex > 3)
        {
            _logger.LogWarning("Invalid answer index {Index} for session {SessionId}", selectedAnswerIndex, quizSessionId);
            return new SubmitAnswerResult
            {
                IsValid = false,
                ErrorMessage = "Invalid answer selection. Please select an option.",
                UpdatedProgress = await GetQuizProgressAsync(quizSessionId)
            };
        }

        try
        {
            // Get current question
            var currentQuestion = session.Questions[session.CurrentQuestionIndex];
            var isCorrect = selectedAnswerIndex == currentQuestion.CorrectAnswerIndex;

            // Check for duplicate submission (already answered)
            if (!session.UserAnswers.ContainsKey(session.CurrentQuestionIndex))
            {
                session.UserAnswers[session.CurrentQuestionIndex] = selectedAnswerIndex;
            }
            else
            {
                _logger.LogDebug("Duplicate answer submission for question {QuestionIndex} in session {SessionId}", 
                    session.CurrentQuestionIndex, quizSessionId);
            }

            // Advance to next question
            var isLastQuestion = session.CurrentQuestionIndex >= session.TotalQuestions - 1;
            if (!isLastQuestion)
            {
                session.CurrentQuestionIndex++;
            }
            else
            {
                session.IsComplete = true;
            }

            session.LastActivityAt = DateTime.UtcNow;

            // Update cache
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(SessionTimeoutMinutes));
            _cache.Set(GetCacheKey(quizSessionId), session, cacheOptions);

            // Get updated progress
            var progress = CalculateProgress(session);

            _logger.LogInformation("Answer submitted for session {SessionId}, question {QuestionIndex}, correct: {IsCorrect}", 
                quizSessionId, session.CurrentQuestionIndex - 1, isCorrect);

            return new SubmitAnswerResult
            {
                IsValid = true,
                IsCorrect = isCorrect,
                IsLastQuestion = isLastQuestion,
                UpdatedProgress = progress
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer for session {SessionId}", quizSessionId);
            throw;
        }
    }

    public async Task<QuizScore> GetCurrentScoreAsync(string quizSessionId)
    {
        var session = await GetQuizSessionAsync(quizSessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found for score retrieval: {SessionId}", quizSessionId);
            throw new InvalidOperationException("Quiz session not found.");
        }

        return CalculateScore(session);
    }

    public async Task<QuizProgress> GetQuizProgressAsync(string quizSessionId)
    {
        var session = await GetQuizSessionAsync(quizSessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found for progress retrieval: {SessionId}", quizSessionId);
            throw new InvalidOperationException("Quiz session not found.");
        }

        return CalculateProgress(session);
    }

    public async Task<GradedQuizSession?> GetQuizSessionAsync(string quizSessionId)
    {
        try
        {
            var session = _cache.Get<GradedQuizSession>(GetCacheKey(quizSessionId));

            if (session != null)
            {
                session.LastActivityAt = DateTime.UtcNow;

                // Refresh cache timeout
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(SessionTimeoutMinutes));
                _cache.Set(GetCacheKey(quizSessionId), session, cacheOptions);

                _logger.LogDebug("Retrieved session {SessionId}", quizSessionId);
            }
            else
            {
                _logger.LogDebug("Session not found or expired: {SessionId}", quizSessionId);
            }

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId}", quizSessionId);
            throw;
        }
    }

    public async Task<QuizScore> FinishQuizAsync(string quizSessionId)
    {
        var session = await GetQuizSessionAsync(quizSessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found for finish: {SessionId}", quizSessionId);
            throw new InvalidOperationException("Quiz session not found.");
        }

        try
        {
            session.IsComplete = true;
            session.LastActivityAt = DateTime.UtcNow;

            // Update cache
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(SessionTimeoutMinutes));
            _cache.Set(GetCacheKey(quizSessionId), session, cacheOptions);

            var score = CalculateScore(session);

            _logger.LogInformation("Quiz completed: {SessionId}, score: {CorrectCount}/{TotalQuestions} ({Percentage}%)", 
                quizSessionId, score.CorrectCount, score.TotalQuestions, score.Percentage);

            return score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finishing quiz {SessionId}", quizSessionId);
            throw;
        }
    }

    public async Task<bool> ClearQuizSessionAsync(string quizSessionId)
    {
        try
        {
            var cacheKey = GetCacheKey(quizSessionId);
            _cache.Remove(cacheKey);

            _logger.LogInformation("Quiz session cleared: {SessionId}", quizSessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing quiz session {SessionId}", quizSessionId);
            return false;
        }
    }

    /// <summary>
    /// Calculates the score based on user answers in the session.
    /// </summary>
    private QuizScore CalculateScore(GradedQuizSession session)
    {
        int correctCount = 0;

        foreach (var answerPair in session.UserAnswers)
        {
            var questionIndex = answerPair.Key;
            var userAnswerIndex = answerPair.Value;

            if (questionIndex >= 0 && questionIndex < session.Questions.Count)
            {
                var question = session.Questions[questionIndex];
                if (userAnswerIndex == question.CorrectAnswerIndex)
                {
                    correctCount++;
                }
            }
        }

        return new QuizScore
        {
            CorrectCount = correctCount,
            IncorrectCount = session.UserAnswers.Count - correctCount,
            TotalQuestions = session.TotalQuestions
        };
    }

    /// <summary>
    /// Calculates the progress based on current question index and answers so far.
    /// </summary>
    private QuizProgress CalculateProgress(GradedQuizSession session)
    {
        var score = CalculateScore(session);

        return new QuizProgress
        {
            CurrentQuestionNumber = session.CurrentQuestionIndex + 1,
            TotalQuestions = session.TotalQuestions,
            CorrectCount = score.CorrectCount,
            IncorrectCount = score.IncorrectCount
        };
    }

    /// <summary>
    /// Gets the cache key for a quiz session.
    /// </summary>
    private static string GetCacheKey(string quizSessionId)
    {
        return $"{CacheKeyPrefix}{quizSessionId}";
    }
}
