using StudyHelper.Models;
using Microsoft.Extensions.Caching.Memory;

namespace StudyHelper.Services;

/// <summary>
/// Service for managing graded exercise sessions, score tracking, and session lifecycle.
/// </summary>
public class GradedExerciseService : IGradedExerciseService
{
    private readonly IExerciseProblemGeneratorService _problemGeneratorService;
    private readonly IEquationParserService _equationParserService;
    private readonly IUserStudyMaterialService _studyMaterialService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GradedExerciseService> _logger;

    private const int SessionTimeoutMinutes = 30;
    private const string CacheKeyPrefix = "graded-exercise-";

    public GradedExerciseService(
        IExerciseProblemGeneratorService problemGeneratorService,
        IEquationParserService equationParserService,
        IUserStudyMaterialService studyMaterialService,
        IMemoryCache cache,
        ILogger<GradedExerciseService> logger)
    {
        _problemGeneratorService = problemGeneratorService ?? throw new ArgumentNullException(nameof(problemGeneratorService));
        _equationParserService = equationParserService ?? throw new ArgumentNullException(nameof(equationParserService));
        _studyMaterialService = studyMaterialService ?? throw new ArgumentNullException(nameof(studyMaterialService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> StartExerciseAsync(int problemCount, string username, string? courseName = null)
    {
        // Validate inputs
        if (problemCount < 1 || problemCount > 50)
        {
            _logger.LogWarning("Invalid problem count: {ProblemCount}", problemCount);
            throw new ArgumentException("Problem count must be between 1 and 50.", nameof(problemCount));
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("Invalid username for exercise start");
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }

        try
        {
            _logger.LogInformation("Starting graded exercise for user {Username}/{Course} with {ProblemCount} problems",
                username, courseName ?? "no-course", problemCount);

            // Get user's equation file path using the course-aware overload when available
            var equationFilePath = !string.IsNullOrWhiteSpace(courseName)
                ? await _studyMaterialService.GetEffectiveFilePathAsync(username, courseName, StudyMaterialType.Equations)
                : await _studyMaterialService.GetEffectiveFilePathAsync(username, StudyMaterialType.Equations);

            if (string.IsNullOrEmpty(equationFilePath) || !File.Exists(equationFilePath))
            {
                _logger.LogWarning("No equation file found for user {Username}/{Course}", username, courseName ?? "no-course");
                throw new InvalidOperationException("No equation file found. Please upload an equations file first.");
            }

            // Parse equations using course-aware path
            var equations = await _equationParserService.ParseEquationsAsync(username, courseName);

            if (equations == null || equations.Count == 0)
            {
                _logger.LogWarning("No equations parsed from file for user {Username}/{Course}", username, courseName ?? "no-course");
                throw new InvalidOperationException("No valid equations found in your equations file.");
            }

            if (equations.Count < problemCount)
            {
                _logger.LogWarning("Insufficient equations: {EquationCount} available, {ProblemCount} requested",
                    equations.Count, problemCount);
                throw new InvalidOperationException(
                    $"Not enough equations. You have {equations.Count} equations but requested {problemCount} problems.");
            }

            // Generate problems
            var problems = new List<ExerciseProblem>();
            for (int i = 0; i < problemCount; i++)
            {
                var problem = _problemGeneratorService.GenerateProblem(equations);
                problems.Add(problem);
            }

            // Create session
            var session = new GradedExerciseSession
            {
                SessionId = Guid.NewGuid().ToString(),
                Username = username,
                TotalProblems = problemCount,
                CurrentProblemIndex = 0,
                Problems = problems,
                UserAnswers = new Dictionary<int, decimal>(),
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                IsComplete = false
            };

            // Store in cache
            var cacheKey = $"{CacheKeyPrefix}{session.SessionId}";
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(SessionTimeoutMinutes));

            _cache.Set(cacheKey, session, cacheOptions);

            _logger.LogInformation("Exercise session created: {SessionId} for user {Username}", 
                session.SessionId, username);

            return session.SessionId;
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error starting exercise for user {Username}", username);
            throw new InvalidOperationException("Unable to start exercise. Please try again.", ex);
        }
    }

    public async Task<SubmitAnswerResult> SubmitAnswerAsync(string sessionId, string userAnswer)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentNullException.ThrowIfNull(userAnswer);

        var session = await GetExerciseSessionAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Exercise session not found: {SessionId}", sessionId);
            throw new InvalidOperationException("Exercise session not found or expired.");
        }

        if (session.IsComplete)
        {
            _logger.LogWarning("Attempt to submit answer to completed session: {SessionId}", sessionId);
            throw new InvalidOperationException("Exercise session is already complete.");
        }

        if (session.CurrentProblemIndex >= session.TotalProblems)
        {
            _logger.LogWarning("Problem index out of range: {Index}/{Total}", 
                session.CurrentProblemIndex, session.TotalProblems);
            throw new InvalidOperationException("No more problems in this session.");
        }

        // Parse user answer
        var cleanedAnswer = userAnswer.Replace("$", "")
                                      .Replace(",", "")
                                      .Replace("%", "")
                                      .Trim();

        if (!decimal.TryParse(cleanedAnswer, out decimal parsedAnswer))
        {
            _logger.LogWarning("Invalid answer format from user {Username}: {Answer}", 
                session.Username, userAnswer);
            return new SubmitAnswerResult
            {
                IsValid = false,
                IsCorrect = false,
                IsLastProblem = false,
                ErrorMessage = "Please enter a valid number.",
                CorrectAnswer = 0
            };
        }

        // Get current problem and validate
        var currentProblem = session.Problems[session.CurrentProblemIndex];
        var validationResult = _problemGeneratorService.ValidateAnswer(currentProblem, parsedAnswer);

        // Store user answer
        session.UserAnswers[session.CurrentProblemIndex] = parsedAnswer;

        // Update session state
        session.LastActivityAt = DateTime.UtcNow;
        var isLastProblem = session.CurrentProblemIndex == session.TotalProblems - 1;

        if (!isLastProblem)
        {
            session.CurrentProblemIndex++;
        }

        // Update cache
        var cacheKey = $"{CacheKeyPrefix}{sessionId}";
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(SessionTimeoutMinutes));
        _cache.Set(cacheKey, session, cacheOptions);

        _logger.LogInformation("Answer submitted for session {SessionId}, problem {ProblemIndex}, correct: {IsCorrect}",
            sessionId, session.CurrentProblemIndex - (isLastProblem ? 0 : 1), validationResult.IsCorrect);

        return new SubmitAnswerResult
        {
            IsValid = true,
            IsCorrect = validationResult.IsCorrect,
            IsLastProblem = isLastProblem,
            ErrorMessage = string.Empty,
            CorrectAnswer = currentProblem.CorrectAnswer
        };
    }

    public async Task<GradedExerciseSession?> GetExerciseSessionAsync(string sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        var cacheKey = $"{CacheKeyPrefix}{sessionId}";
        if (_cache.TryGetValue(cacheKey, out GradedExerciseSession? session))
        {
            _logger.LogDebug("Exercise session retrieved from cache: {SessionId}", sessionId);
            return session;
        }

        _logger.LogDebug("Exercise session not found in cache: {SessionId}", sessionId);
        return await Task.FromResult<GradedExerciseSession?>(null);
    }

    public async Task<ExerciseScore> GetCurrentScoreAsync(string sessionId)
    {
        var session = await GetExerciseSessionAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found for score retrieval: {SessionId}", sessionId);
            throw new InvalidOperationException("Exercise session not found.");
        }

        int correctCount = 0;
        int incorrectCount = 0;

        foreach (var (problemIndex, userAnswer) in session.UserAnswers)
        {
            var problem = session.Problems[problemIndex];
            var result = _problemGeneratorService.ValidateAnswer(problem, userAnswer);

            if (result.IsCorrect)
                correctCount++;
            else
                incorrectCount++;
        }

        var score = new ExerciseScore
        {
            CorrectCount = correctCount,
            IncorrectCount = incorrectCount,
            TotalProblems = session.TotalProblems,
            PerformanceRating = CalculatePerformanceRating(correctCount, session.TotalProblems)
        };

        _logger.LogDebug("Score calculated for session {SessionId}: {CorrectCount}/{TotalProblems}",
            sessionId, correctCount, session.TotalProblems);

        return score;
    }

    public async Task<ExerciseProgress> GetExerciseProgressAsync(string sessionId)
    {
        var session = await GetExerciseSessionAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found for progress retrieval: {SessionId}", sessionId);
            throw new InvalidOperationException("Exercise session not found.");
        }

        var score = await GetCurrentScoreAsync(sessionId);

        return new ExerciseProgress
        {
            CurrentProblemIndex = session.CurrentProblemIndex,
            TotalProblems = session.TotalProblems,
            CorrectCount = score.CorrectCount,
            IncorrectCount = score.IncorrectCount,
            IsComplete = session.IsComplete
        };
    }

    public async Task<ExerciseScore> FinishExerciseAsync(string sessionId)
    {
        var session = await GetExerciseSessionAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found for finish: {SessionId}", sessionId);
            throw new InvalidOperationException("Exercise session not found.");
        }

        session.IsComplete = true;
        session.LastActivityAt = DateTime.UtcNow;

        // Update cache
        var cacheKey = $"{CacheKeyPrefix}{sessionId}";
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(SessionTimeoutMinutes));
        _cache.Set(cacheKey, session, cacheOptions);

        var finalScore = await GetCurrentScoreAsync(sessionId);

        _logger.LogInformation("Exercise session finished: {SessionId}, score: {CorrectCount}/{TotalProblems} ({Percentage}%)",
            sessionId, finalScore.CorrectCount, finalScore.TotalProblems, finalScore.Percentage);

        return finalScore;
    }

    public async Task<bool> ClearExerciseSessionAsync(string sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        var cacheKey = $"{CacheKeyPrefix}{sessionId}";
        if (_cache.TryGetValue(cacheKey, out GradedExerciseSession? _))
        {
            _cache.Remove(cacheKey);
            _logger.LogInformation("Exercise session cleared: {SessionId}", sessionId);
            return await Task.FromResult(true);
        }

        _logger.LogDebug("Exercise session not found for clearing: {SessionId}", sessionId);
        return await Task.FromResult(false);
    }

    private static string CalculatePerformanceRating(int correctCount, int totalProblems)
    {
        if (totalProblems == 0) return "N/A";

        var percentage = (decimal)correctCount / totalProblems * 100;

        return percentage switch
        {
            >= 90 => "Excellent",
            >= 80 => "Good",
            >= 70 => "Fair",
            >= 60 => "Poor",
            _ => "Needs Improvement"
        };
    }
}
