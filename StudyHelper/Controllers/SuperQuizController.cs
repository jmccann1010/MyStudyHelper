using StudyHelper.Models;
using StudyHelper.Services;
using StudyHelper.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudyHelper.Controllers;

/// <summary>
/// Controller for handling Super Quiz operations including session management,
/// question display, answer validation, and completion tracking.
/// </summary>
[Authorize]
public class SuperQuizController : Controller
{
    private readonly ISuperQuizService _superQuizService;
    private readonly IMarkdownParserService _markdownParserService;
    private readonly ILogger<SuperQuizController> _logger;

    public SuperQuizController(
        ISuperQuizService superQuizService,
        IMarkdownParserService markdownParserService,
        ILogger<SuperQuizController> logger)
    {
        _superQuizService = superQuizService;
        _markdownParserService = markdownParserService;
        _logger = logger;
    }

    /// <summary>
    /// GET: /SuperQuiz/Start
    /// Display session start page with question count options.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Start()
    {
        try
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get available terms count using course-aware path when available
            var courseName = HttpContext.Session.GetString("ActiveCourseNameSafe");
            var sections = await _markdownParserService.ParseMarkdownFilesAsync(username, courseName);

            // Count total number of term/definition pairs across all sections
            var totalTerms = sections.Sum(s => s.TermDefinitions.Count);

            if (totalTerms < SuperQuizStartViewModel.MinimumTermsRequired)
            {
                ViewBag.ErrorMessage = $"At least {SuperQuizStartViewModel.MinimumTermsRequired} terms required for Super Quiz. You currently have {totalTerms} term(s). Please add more study materials.";
                return View("InsufficientContent");
            }

            // Build view model with all options
            var viewModel = new SuperQuizStartViewModel
            {
                TotalAvailableTerms = totalTerms,
                SelectedQuestionCount = 10 // Default to 10 questions
            };

            return View(viewModel);
        }
        catch (FileNotFoundException)
        {
            ViewBag.ErrorMessage = "No study materials found. Please upload study materials first.";
            return View("NoStudyMaterials");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Super Quiz start page for user {Username}", User.Identity?.Name);
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// POST: /SuperQuiz/Start
    /// Create session with selected question count and redirect to first question.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start([FromForm] int questionCount)
    {
        try
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Validate question count range
            if (questionCount < SuperQuizStartViewModel.MinimumTermsRequired)
            {
                TempData["ErrorMessage"] = $"Question count must be at least {SuperQuizStartViewModel.MinimumTermsRequired}.";
                return RedirectToAction(nameof(Start));
            }

            // Defensive upper-bound validation to prevent abuse
            if (questionCount > SuperQuizStartViewModel.MaximumReasonableQuestionCount)
            {
                TempData["ErrorMessage"] = $"Question count must not exceed {SuperQuizStartViewModel.MaximumReasonableQuestionCount}.";
                return RedirectToAction(nameof(Start));
            }

            // Start session using course-aware path when a course is active
            var courseName = HttpContext.Session.GetString("ActiveCourseNameSafe");
            var sessionId = await _superQuizService.StartSuperQuizAsync(username, questionCount, courseName);

            _logger.LogInformation(
                "Super Quiz session {SessionId} started for user {Username} with {QuestionCount} questions",
                sessionId,
                username,
                questionCount);

            return RedirectToAction(nameof(Question), new { sessionId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to start Super Quiz for user {Username}", User.Identity?.Name);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Start));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Super Quiz session for user {Username}", User.Identity?.Name);
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// GET: /SuperQuiz/Question?sessionId={guid}
    /// Display current question with answer options.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Question([FromQuery] string sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                TempData["ErrorMessage"] = "Invalid session. Please start a new Super Quiz.";
                return RedirectToAction(nameof(Start));
            }

            var username = User.Identity?.Name;
            if (!await _superQuizService.ValidateSessionOwnershipAsync(sessionId, username))
            {
                _logger.LogWarning("User {Username} attempted to access session {SessionId} they don't own", 
                    username, sessionId);
                return Forbid();
            }

            var question = await _superQuizService.GetCurrentQuestionAsync(sessionId);
            if (question == null)
            {
                TempData["ErrorMessage"] = "Your session expired. Please start a new Super Quiz.";
                return RedirectToAction(nameof(Start));
            }

            var progress = await _superQuizService.GetProgressAsync(sessionId);

            var viewModel = new SuperQuizQuestionViewModel
            {
                SessionId = sessionId,
                QuestionText = question.QuestionText,
                AnswerOptions = question.AnswerOptions,
                Module = question.Module,
                Topic = question.Topic,
                Direction = question.Direction,
                DirectionLabel = question.Direction == QuestionDirection.TermToDefinition 
                    ? "Term → Definition" 
                    : "Definition → Term",
                Progress = progress
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading question for session {SessionId}", sessionId);
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// POST: /SuperQuiz/SubmitAnswer
    /// Validate answer and advance to next state.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAnswer([FromForm] string sessionId, [FromForm] int selectedAnswerIndex)
    {
        try
        {
            if (selectedAnswerIndex < 0 || selectedAnswerIndex > 3)
            {
                _logger.LogWarning("Invalid answer index submitted: {Index}", selectedAnswerIndex);
                return BadRequest("Invalid answer selection.");
            }

            var username = User.Identity?.Name;
            if (!await _superQuizService.ValidateSessionOwnershipAsync(sessionId, username))
            {
                _logger.LogWarning("User {Username} attempted to submit answer for session {SessionId} they don't own",
                    username, sessionId);
                return Forbid();
            }

            var result = await _superQuizService.SubmitAnswerAsync(sessionId, selectedAnswerIndex);

            var viewModel = new SuperQuizResultViewModel
            {
                SessionId = sessionId,
                IsCorrect = result.IsCorrect,
                FeedbackMessage = result.IsCorrect ? "Correct!" : "Incorrect",
                CorrectAnswerText = result.CorrectAnswerText,
                UserAnswerText = result.UserAnswerText,
                Explanation = result.Explanation,
                Progress = result.Progress,
                NextAction = result.NextAction
            };

            return View("Result", viewModel);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Session not found or invalid: {SessionId}", sessionId);
            TempData["ErrorMessage"] = "Your session expired. Please start a new Super Quiz.";
            return RedirectToAction(nameof(Start));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer for session {SessionId}", sessionId);
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// GET: /SuperQuiz/RoundSummary?sessionId={guid}
    /// Display round completion summary and prompt for next round.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> RoundSummary([FromQuery] string sessionId)
    {
        try
        {
            var username = User.Identity?.Name;
            if (!await _superQuizService.ValidateSessionOwnershipAsync(sessionId, username))
            {
                _logger.LogWarning("User {Username} attempted to access round summary for session {SessionId} they don't own",
                    username, sessionId);
                return Forbid();
            }

            var summary = await _superQuizService.GetLastRoundSummaryAsync(sessionId);
            if (summary == null)
            {
                TempData["ErrorMessage"] = "Session not found.";
                return RedirectToAction(nameof(Start));
            }

            var progress = await _superQuizService.GetProgressAsync(sessionId);

            var viewModel = new SuperQuizRoundSummaryViewModel
            {
                SessionId = sessionId,
                RoundSummary = summary,
                Progress = progress
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading round summary for session {SessionId}", sessionId);
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// POST: /SuperQuiz/ContinueNextRound
    /// Start next round after round summary.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContinueNextRound([FromForm] string sessionId)
    {
        try
        {
            var username = User.Identity?.Name;
            if (!await _superQuizService.ValidateSessionOwnershipAsync(sessionId, username))
            {
                _logger.LogWarning("User {Username} attempted to continue round for session {SessionId} they don't own",
                    username, sessionId);
                return Forbid();
            }

            await _superQuizService.StartNextRoundAsync(sessionId);

            return RedirectToAction(nameof(Question), new { sessionId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot start next round for session {SessionId}", sessionId);
            TempData["ErrorMessage"] = "Unable to start next round.";
            return RedirectToAction(nameof(Start));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting next round for session {SessionId}", sessionId);
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// GET: /SuperQuiz/Complete?sessionId={guid}
    /// Display completion summary with all statistics.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Complete([FromQuery] string sessionId)
    {
        try
        {
            var username = User.Identity?.Name;
            if (!await _superQuizService.ValidateSessionOwnershipAsync(sessionId, username))
            {
                _logger.LogWarning("User {Username} attempted to access completion for session {SessionId} they don't own",
                    username, sessionId);
                return Forbid();
            }

            var summary = await _superQuizService.GetCompletionSummaryAsync(sessionId);
            if (summary == null)
            {
                TempData["ErrorMessage"] = "Session not found or not completed.";
                return RedirectToAction(nameof(Start));
            }

            var viewModel = new SuperQuizCompleteViewModel
            {
                Summary = summary
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading completion summary for session {SessionId}", sessionId);
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }
}
