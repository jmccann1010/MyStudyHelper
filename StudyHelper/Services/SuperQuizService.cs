using Microsoft.Extensions.Caching.Memory;
using StudyHelper.Models;
using StudyHelper.ViewModels;

namespace StudyHelper.Services;

/// <summary>
/// Service implementation for managing Super Quiz sessions with mastery-based retry logic.
/// Uses in-memory cache for session storage with 60-minute sliding expiration.
/// </summary>
public class SuperQuizService : ISuperQuizService
{
    private readonly IMarkdownParserService _markdownParserService;
    private readonly IQuestionGeneratorService _questionGeneratorService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<SuperQuizService> _logger;

    private const int MaxQuestionsLimit = 500;
    private const int MinQuestionsRequired = 4;
    private const int SessionTimeoutMinutes = 60;

    public SuperQuizService(
        IMarkdownParserService markdownParserService,
        IQuestionGeneratorService questionGeneratorService,
        IMemoryCache memoryCache,
        ILogger<SuperQuizService> logger)
    {
        _markdownParserService = markdownParserService;
        _questionGeneratorService = questionGeneratorService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> StartSuperQuizAsync(
        string username,
        int questionCount = SuperQuizStartViewModel.AllQuestionsIndicator,
        string? courseName = null)
    {
        if (string.IsNullOrEmpty(username))
        {
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }

        _logger.LogInformation(
            "Starting Super Quiz session for user {Username}/{Course} with question count {QuestionCount}",
            username, courseName ?? "no-course", questionCount);

        // Parse markdown files using course-aware path when available
        var sections = await _markdownParserService.ParseMarkdownFilesAsync(username, courseName);

        if (sections.Count == 0)
        {
            _logger.LogWarning("No study materials found for user {Username}", username);
            throw new InvalidOperationException("No study materials found. Please upload study materials first.");
        }

        // Count total number of term/definition pairs across all sections
        int totalTerms = sections.Sum(s => s.TermDefinitions.Count);

        if (totalTerms < MinQuestionsRequired)
        {
            _logger.LogWarning("Insufficient content for user {Username}: {Count} terms", username, totalTerms);
            throw new InvalidOperationException(
                $"At least {MinQuestionsRequired} terms required for Super Quiz. You currently have {totalTerms} term(s). Please add more study materials.");
        }

        // Calculate target question count
        // AllQuestionsIndicator means "All", otherwise use the provided count
        int targetQuestionCount = questionCount == SuperQuizStartViewModel.AllQuestionsIndicator 
            ? totalTerms 
            : questionCount;

        // Validate target count is achievable
        if (targetQuestionCount > totalTerms)
        {
            throw new InvalidOperationException(
                $"Cannot generate {targetQuestionCount} questions. Only {totalTerms} terms available.");
        }

        if (targetQuestionCount < MinQuestionsRequired)
        {
            throw new InvalidOperationException(
                $"At least {MinQuestionsRequired} questions required. Selected count of {targetQuestionCount} is too low.");
        }

        // Generate questions up to the target count
        // Since each call to GenerateQuestion picks from available sections, 
        // we simply call it targetQuestionCount times
        var allQuestions = new List<QuizQuestion>();

        for (int i = 0; i < targetQuestionCount && i < MaxQuestionsLimit; i++)
        {
            try
            {
                var question = _questionGeneratorService.GenerateQuestion(sections);
                allQuestions.Add(question);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate question {QuestionNumber} for user {Username}", i + 1, username);
                // Continue attempting to generate more questions
            }
        }

        // Final validation
        if (allQuestions.Count < MinQuestionsRequired)
        {
            throw new InvalidOperationException(
                $"Failed to generate sufficient questions. Only {allQuestions.Count} questions could be created.");
        }

        // Create session
        var session = new SuperQuizSession
        {
            SessionId = Guid.NewGuid().ToString(),
            Username = username,
            AllQuestions = allQuestions,
            CurrentRound = 1,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        // Randomize questions for first round
        RandomizeQuestionsIntoQueue(session, allQuestions);

        // Store in cache
        var cacheKey = GetCacheKey(session.SessionId);
        _memoryCache.Set(cacheKey, session, GetCacheOptions());

        _logger.LogInformation(
            "Super Quiz session {SessionId} started for user {Username} with {QuestionCount} questions",
            session.SessionId,
            username,
            allQuestions.Count);

        return session.SessionId;
    }

    /// <inheritdoc/>
    public Task<QuizQuestion?> GetCurrentQuestionAsync(string sessionId)
    {
        var session = GetSession(sessionId);
        if (session == null)
        {
            return Task.FromResult<QuizQuestion?>(null);
        }

        // Update activity timestamp
        session.LastActivityAt = DateTime.UtcNow;
        SaveSession(session);

        // Peek at current question (don't dequeue yet)
        var currentQuestion = session.CurrentRoundQueue.Count > 0 
            ? session.CurrentRoundQueue.Peek() 
            : null;

        return Task.FromResult(currentQuestion);
    }

    /// <inheritdoc/>
    public Task<SuperQuizAnswerResult> SubmitAnswerAsync(string sessionId, int selectedAnswerIndex)
    {
        // Validate answer index
        if (selectedAnswerIndex < 0 || selectedAnswerIndex > 3)
        {
            throw new InvalidOperationException($"Invalid answer index: {selectedAnswerIndex}. Must be between 0 and 3.");
        }

        var session = GetSession(sessionId);
        if (session == null)
        {
            throw new InvalidOperationException("Session not found or expired. Please start a new Super Quiz.");
        }

        if (session.CurrentRoundQueue.Count == 0)
        {
            throw new InvalidOperationException("No questions available in current round.");
        }

        // Dequeue current question
        var question = session.CurrentRoundQueue.Dequeue();

        // Update activity timestamp
        session.LastActivityAt = DateTime.UtcNow;

        // Validate answer
        var isCorrect = selectedAnswerIndex == question.CorrectAnswerIndex;

        // Find question index in AllQuestions
        var questionIndex = session.AllQuestions.FindIndex(q => 
            q.Term == question.Term && q.Definition == question.Definition);

        if (isCorrect)
        {
            // Mark as mastered
            if (questionIndex >= 0)
            {
                session.CorrectlyAnswered.Add(questionIndex);
            }
            _logger.LogDebug("User answered correctly for session {SessionId}, question index {Index}", 
                sessionId, questionIndex);
        }
        else
        {
            // Add to missed list for next round
            session.MissedThisRound.Add(question);
            _logger.LogDebug("User answered incorrectly for session {SessionId}, question index {Index}", 
                sessionId, questionIndex);
        }

        // Determine next action
        var nextAction = DetermineNextAction(session);

        // Handle round completion
        if (nextAction == SuperQuizNextAction.RoundComplete || nextAction == SuperQuizNextAction.QuizComplete)
        {
            SaveRoundSummary(session);
        }

        // Handle quiz completion
        if (nextAction == SuperQuizNextAction.QuizComplete)
        {
            session.IsComplete = true;
            _logger.LogInformation("Super Quiz session {SessionId} completed for user {Username} in {Rounds} rounds",
                sessionId, session.Username, session.CurrentRound);
        }

        // Save session
        SaveSession(session);

        // Build result
        var result = new SuperQuizAnswerResult
        {
            IsCorrect = isCorrect,
            CorrectAnswerText = question.AnswerOptions[question.CorrectAnswerIndex],
            UserAnswerText = question.AnswerOptions[selectedAnswerIndex],
            Explanation = question.Explanation,
            NextAction = nextAction,
            Progress = BuildProgress(session)
        };

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<SuperQuizProgress?> GetProgressAsync(string sessionId)
    {
        var session = GetSession(sessionId);
        if (session == null)
        {
            return Task.FromResult<SuperQuizProgress?>(null);
        }

        var progress = BuildProgress(session);
        return Task.FromResult<SuperQuizProgress?>(progress);
    }

    /// <inheritdoc/>
    public Task<RoundSummary?> GetLastRoundSummaryAsync(string sessionId)
    {
        var session = GetSession(sessionId);
        if (session == null || session.RoundHistory.Count == 0)
        {
            return Task.FromResult<RoundSummary?>(null);
        }

        var lastRound = session.RoundHistory[^1];
        return Task.FromResult<RoundSummary?>(lastRound);
    }

    /// <inheritdoc/>
    public Task StartNextRoundAsync(string sessionId)
    {
        var session = GetSession(sessionId);
        if (session == null)
        {
            throw new InvalidOperationException("Session not found or expired.");
        }

        if (session.MissedThisRound.Count == 0)
        {
            throw new InvalidOperationException("No missed questions to retry.");
        }

        // Increment round number
        session.CurrentRound++;

        // Randomize missed questions into new round queue
        RandomizeQuestionsIntoQueue(session, session.MissedThisRound);

        // Clear missed list
        session.MissedThisRound.Clear();

        // Update activity timestamp
        session.LastActivityAt = DateTime.UtcNow;

        // Save session
        SaveSession(session);

        _logger.LogInformation("Started round {Round} for session {SessionId} with {QuestionCount} questions",
            session.CurrentRound, sessionId, session.CurrentRoundQueue.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<SuperQuizCompletionSummary?> GetCompletionSummaryAsync(string sessionId)
    {
        var session = GetSession(sessionId);
        if (session == null || !session.IsComplete)
        {
            return Task.FromResult<SuperQuizCompletionSummary?>(null);
        }

        var totalTime = session.LastActivityAt - session.CreatedAt;

        // Calculate overall accuracy
        var totalQuestionsAnswered = session.RoundHistory.Sum(r => r.TotalQuestions);
        var totalCorrect = session.RoundHistory.Sum(r => r.CorrectAnswers);
        var overallAccuracy = totalQuestionsAnswered > 0 
            ? (double)totalCorrect / totalQuestionsAnswered * 100 
            : 0;

        var summary = new SuperQuizCompletionSummary
        {
            TotalQuestions = session.TotalQuestions,
            TotalRounds = session.RoundHistory.Count,
            TotalTime = totalTime,
            RoundHistory = session.RoundHistory,
            OverallAccuracy = overallAccuracy
        };

        return Task.FromResult<SuperQuizCompletionSummary?>(summary);
    }

    /// <inheritdoc/>
    public Task<bool> ValidateSessionOwnershipAsync(string sessionId, string? username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return Task.FromResult(false);
        }

        var session = GetSession(sessionId);
        if (session == null)
        {
            return Task.FromResult(false);
        }

        var isOwner = session.Username.Equals(username, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(isOwner);
    }

    #region Private Helper Methods

    private string GetCacheKey(string sessionId) => $"superquiz-session-{sessionId}";

    private MemoryCacheEntryOptions GetCacheOptions() => new MemoryCacheEntryOptions
    {
        SlidingExpiration = TimeSpan.FromMinutes(SessionTimeoutMinutes),
        Priority = CacheItemPriority.Normal
    };

    private SuperQuizSession? GetSession(string sessionId)
    {
        var cacheKey = GetCacheKey(sessionId);
        return _memoryCache.Get<SuperQuizSession>(cacheKey);
    }

    private void SaveSession(SuperQuizSession session)
    {
        var cacheKey = GetCacheKey(session.SessionId);
        _memoryCache.Set(cacheKey, session, GetCacheOptions());
    }

    private void RandomizeQuestionsIntoQueue(SuperQuizSession session, List<QuizQuestion> questions)
    {
        var random = new Random();
        var shuffled = questions.OrderBy(q => random.Next()).ToList();

        // Clear existing queue
        session.CurrentRoundQueue.Clear();

        // Enqueue shuffled questions
        // Note: Questions are already generated with randomized directions from QuestionGeneratorService
        // We don't modify the direction here to maintain consistency between question text and answer options
        foreach (var question in shuffled)
        {
            session.CurrentRoundQueue.Enqueue(question);
        }
    }

    private SuperQuizNextAction DetermineNextAction(SuperQuizSession session)
    {
        if (session.CurrentRoundQueue.Count > 0)
        {
            // More questions in current round
            return SuperQuizNextAction.NextQuestion;
        }
        else if (session.MissedThisRound.Count > 0)
        {
            // Round complete, but has missed questions
            return SuperQuizNextAction.RoundComplete;
        }
        else
        {
            // All questions answered correctly
            return SuperQuizNextAction.QuizComplete;
        }
    }

    private void SaveRoundSummary(SuperQuizSession session)
    {
        // Calculate round statistics
        var totalQuestions = session.AllQuestions.Count - session.CorrectlyAnswered.Count + session.MissedThisRound.Count;
        if (session.CurrentRound == 1)
        {
            totalQuestions = session.AllQuestions.Count;
        }

        var incorrectAnswers = session.MissedThisRound.Count;
        var correctAnswers = totalQuestions - incorrectAnswers;

        var summary = new RoundSummary
        {
            RoundNumber = session.CurrentRound,
            TotalQuestions = totalQuestions,
            CorrectAnswers = correctAnswers,
            IncorrectAnswers = incorrectAnswers,
            CompletedAt = DateTime.UtcNow
        };

        session.RoundHistory.Add(summary);

        _logger.LogInformation("Round {Round} completed for session {SessionId}: {Correct}/{Total} correct ({Accuracy:F1}%)",
            session.CurrentRound, session.SessionId, correctAnswers, totalQuestions, summary.AccuracyPercent);
    }

    private SuperQuizProgress BuildProgress(SuperQuizSession session)
    {
        return new SuperQuizProgress
        {
            TotalQuestions = session.TotalQuestions,
            Mastered = session.CorrectlyAnswered.Count,
            Remaining = session.RemainingToMaster,
            CurrentRound = session.CurrentRound,
            QuestionsLeftThisRound = session.QuestionsLeftThisRound
        };
    }

    #endregion
}
