using StudyHelper.Models;
using StudyHelper.Services;
using StudyHelper.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudyHelper.Controllers;

/// <summary>
/// Controller for handling graded exercise operations including problem display, 
/// answer validation, and scoring.
/// </summary>
[Authorize]
public class GradedExerciseController : Controller
{
    private readonly IGradedExerciseService _exerciseService;
    private readonly ILogger<GradedExerciseController> _logger;
    private const string SessionKey = "GradedExerciseSessionId";

    public GradedExerciseController(
        IGradedExerciseService exerciseService, 
        ILogger<GradedExerciseController> logger)
    {
        _exerciseService = exerciseService ?? throw new ArgumentNullException(nameof(exerciseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET: /GradedExercise/Setup
    /// Displays the setup page where user selects problem count.
    /// </summary>
    [HttpGet]
    public IActionResult Setup()
    {
        _logger.LogInformation("Setup page accessed");
        return View();
    }

    /// <summary>
    /// POST: /GradedExercise/StartExercise
    /// Validates problem count and initializes a new exercise session.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartExercise(int problemCount)
    {
        try
        {
            // Validate input
            if (problemCount < 1 || problemCount > 50)
            {
                _logger.LogWarning("Invalid problem count submitted: {ProblemCount}", problemCount);
                ViewBag.Error = "Please select a valid number of problems (1-50).";
                return View("Setup");
            }

            // Get current user
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("No authenticated user found");
                return RedirectToAction("Login", "Account");
            }

            // Start exercise
            var sessionId = await _exerciseService.StartExerciseAsync(problemCount, username);
            TempData[SessionKey] = sessionId;

            _logger.LogInformation("Exercise started for user {Username} with {ProblemCount} problems", 
                username, problemCount);

            return RedirectToAction(nameof(Problem));
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Argument error in StartExercise");
            ViewBag.Error = ex.Message;
            return View("Setup");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation in StartExercise");
            ViewBag.Error = ex.Message;
            return View("Setup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in StartExercise");
            return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
        }
    }

    /// <summary>
    /// GET: /GradedExercise/Problem
    /// Displays the current problem in the exercise.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Problem()
    {
        try
        {
            var sessionId = TempData.Peek(SessionKey)?.ToString();
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("No session ID in TempData");
                return RedirectToAction(nameof(Setup));
            }

            var session = await _exerciseService.GetExerciseSessionAsync(sessionId);
            if (session == null)
            {
                _logger.LogWarning("Exercise session not found: {SessionId}", sessionId);
                TempData["ErrorMessage"] = "Your exercise session expired. Please start a new exercise.";
                return RedirectToAction(nameof(Setup));
            }

            if (session.IsComplete)
            {
                _logger.LogInformation("Exercise already complete: {SessionId}", sessionId);
                TempData.Keep(SessionKey);
                return RedirectToAction(nameof(Results));
            }

            var progress = await _exerciseService.GetExerciseProgressAsync(sessionId);
            var problem = session.Problems[session.CurrentProblemIndex];

            var viewModel = new GradedExerciseProblemViewModel
            {
                ProblemNumber = session.CurrentProblemIndex + 1,
                TotalProblems = session.TotalProblems,
                ProblemText = problem.ProblemText,
                GivenValues = problem.GivenValues,
                SolveForVariable = problem.SolveForVariable,
                CorrectCount = progress.CorrectCount,
                IncorrectCount = progress.IncorrectCount
            };

            // Preserve SessionId in TempData for next request
            TempData.Keep(SessionKey);

            _logger.LogInformation("Problem page displayed for session {SessionId}, problem {ProblemNumber}", 
                sessionId, session.CurrentProblemIndex + 1);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying problem");
            return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
        }
    }

    /// <summary>
    /// POST: /GradedExercise/SubmitAnswer
    /// Validates the submitted answer and advances to the next problem or results page.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAnswer(string userAnswer)
    {
        try
        {
            var sessionId = TempData.Peek(SessionKey)?.ToString();
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("No session ID in TempData during SubmitAnswer");
                return RedirectToAction(nameof(Setup));
            }

            var result = await _exerciseService.SubmitAnswerAsync(sessionId, userAnswer);

            if (!result.IsValid)
            {
                _logger.LogWarning("Invalid answer submission: {ErrorMessage}", result.ErrorMessage);
                TempData["ErrorMessage"] = result.ErrorMessage;
                TempData.Keep(SessionKey);
                return RedirectToAction(nameof(Problem));
            }

            _logger.LogInformation("Answer submitted for session {SessionId}, correct: {IsCorrect}", 
                sessionId, result.IsCorrect);

            // Persist session ID for next page
            TempData.Keep(SessionKey);

            if (result.IsLastProblem)
            {
                await _exerciseService.FinishExerciseAsync(sessionId);
                return RedirectToAction(nameof(Results));
            }

            return RedirectToAction(nameof(Problem));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Session expired during answer submission");
            TempData["ErrorMessage"] = "Your exercise session expired. Please start a new exercise.";
            return RedirectToAction(nameof(Setup));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer");
            return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
        }
    }

    /// <summary>
    /// GET: /GradedExercise/Results
    /// Displays the final score and problem review.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Results()
    {
        try
        {
            var sessionId = TempData.Peek(SessionKey)?.ToString();
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("No session ID in TempData for Results");
                return RedirectToAction(nameof(Setup));
            }

            var session = await _exerciseService.GetExerciseSessionAsync(sessionId);
            if (session == null || !session.IsComplete)
            {
                _logger.LogWarning("Session not found or not complete for Results: {SessionId}", sessionId);
                return RedirectToAction(nameof(Setup));
            }

            var score = await _exerciseService.GetCurrentScoreAsync(sessionId);

            var viewModel = new GradedExerciseResultViewModel
            {
                CorrectCount = score.CorrectCount,
                IncorrectCount = score.IncorrectCount,
                TotalProblems = score.TotalProblems,
                Percentage = score.Percentage,
                PerformanceRating = score.PerformanceRating,
                Problems = session.Problems,
                UserAnswers = session.UserAnswers
            };

            _logger.LogInformation("Results page displayed for session {SessionId}: {CorrectCount}/{TotalProblems} ({Percentage}%)", 
                sessionId, score.CorrectCount, score.TotalProblems, score.Percentage);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying results");
            return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
        }
    }

    /// <summary>
    /// GET: /GradedExercise/RetakeExercise
    /// Clears the current exercise session and redirects to setup.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> RetakeExercise()
    {
        try
        {
            var sessionId = TempData.Peek(SessionKey)?.ToString();
            if (!string.IsNullOrEmpty(sessionId))
            {
                await _exerciseService.ClearExerciseSessionAsync(sessionId);
                _logger.LogInformation("Exercise session cleared for retake: {SessionId}", sessionId);
            }

            return RedirectToAction(nameof(Setup));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing exercise session");
            return RedirectToAction(nameof(Setup));
        }
    }
}
