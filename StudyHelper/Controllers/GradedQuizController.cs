using StudyHelper.Models;
using StudyHelper.Services;
using StudyHelper.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudyHelper.Controllers;

/// <summary>
/// Controller for handling graded quiz operations including question display, answer validation, and scoring.
/// </summary>
[Authorize]
public class GradedQuizController : Controller
{
    private readonly IGradedQuizService _quizService;
    private readonly ILogger<GradedQuizController> _logger;
    private const string SessionKey = "GradedQuizSessionId";

    public GradedQuizController(IGradedQuizService quizService, ILogger<GradedQuizController> logger)
    {
        _quizService = quizService ?? throw new ArgumentNullException(nameof(quizService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET: /GradedQuiz/Setup
    /// Displays the setup page where user selects question count.
    /// </summary>
    [HttpGet]
    public IActionResult Setup()
    {
        _logger.LogInformation("Setup page accessed");
        return View();
    }

    /// <summary>
    /// POST: /GradedQuiz/StartQuiz
    /// Validates question count and initializes a new quiz session.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartQuiz(int questionCount)
    {
        try
        {
            // Validate input
            if (questionCount < 1 || questionCount > 50)
            {
                _logger.LogWarning("Invalid question count submitted: {QuestionCount}", questionCount);
                ViewBag.Error = "Please select a valid number of questions (1-50).";
                return View("Setup");
            }

            // Get current user
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("No authenticated user found");
                return RedirectToAction("Login", "Account");
            }

            // Start quiz
            var courseName = HttpContext.Session.GetString("ActiveCourseNameSafe");
            var sessionId = await _quizService.StartQuizAsync(questionCount, username, courseName);
            TempData[SessionKey] = sessionId;

            _logger.LogInformation("Quiz started for user {Username} with {QuestionCount} questions", username, questionCount);

            return RedirectToAction(nameof(Question));
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Argument error in StartQuiz");
            ViewBag.Error = ex.Message;
            return View("Setup");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation in StartQuiz");
            ViewBag.Error = "Unable to start quiz. Please try again.";
            return View("Setup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in StartQuiz");
            return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
        }
    }

    /// <summary>
    /// GET: /GradedQuiz/Question
    /// Displays the current question in the quiz.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Question()
    {
        try
        {
            var sessionId = TempData.Peek(SessionKey)?.ToString();
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("No session ID in TempData");
                return RedirectToAction(nameof(Setup));
            }

            var session = await _quizService.GetQuizSessionAsync(sessionId);
            if (session == null)
            {
                _logger.LogWarning("Quiz session not found: {SessionId}", sessionId);
                TempData["ErrorMessage"] = "Your quiz session expired. Please start a new quiz.";
                return RedirectToAction(nameof(Setup));
            }

            if (session.IsComplete)
            {
                _logger.LogInformation("Quiz already complete: {SessionId}", sessionId);
                TempData.Keep(SessionKey);
                return RedirectToAction(nameof(Results));
            }

            var progress = await _quizService.GetQuizProgressAsync(sessionId);
            var question = session.Questions[session.CurrentQuestionIndex];

            var viewModel = new GradedQuizQuestionViewModel
            {
                QuestionNumber = session.CurrentQuestionIndex + 1,
                TotalQuestions = session.TotalQuestions,
                QuestionText = question.QuestionText,
                AnswerOptions = question.AnswerOptions,
                CorrectCount = progress.CorrectCount,
                IncorrectCount = progress.IncorrectCount,
                Direction = question.Direction,
                DirectionLabel = question.Direction == QuestionDirection.TermToDefinition 
                    ? "Term → Definition" 
                    : "Definition → Term"
            };

            // Preserve SessionId in TempData for next request
            TempData.Keep(SessionKey);

            _logger.LogInformation("Question page displayed for session {SessionId}, question {QuestionNumber}",
                sessionId, session.CurrentQuestionIndex + 1);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying question");
            return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
        }
    }

    /// <summary>
    /// POST: /GradedQuiz/SubmitAnswer
    /// Validates the submitted answer and advances to the next question or results page.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAnswer(int selectedAnswerIndex)
    {
        try
        {
            var sessionId = TempData.Peek(SessionKey)?.ToString();
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("No session ID in TempData during SubmitAnswer");
                return RedirectToAction(nameof(Setup));
            }

            var result = await _quizService.SubmitAnswerAsync(sessionId, selectedAnswerIndex);

            if (!result.IsValid)
            {
                _logger.LogWarning("Invalid answer submission: {ErrorMessage}", result.ErrorMessage);
                TempData["ErrorMessage"] = result.ErrorMessage;
                TempData.Keep(SessionKey);
                return RedirectToAction(nameof(Question));
            }

            _logger.LogInformation("Answer submitted for session {SessionId}, correct: {IsCorrect}", sessionId, result.IsCorrect);

            // Persist session ID for next page
            TempData.Keep(SessionKey);

            if (result.IsLastQuestion)
            {
                await _quizService.FinishQuizAsync(sessionId);
                return RedirectToAction(nameof(Results));
            }

            return RedirectToAction(nameof(Question));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Session expired during answer submission");
            TempData["ErrorMessage"] = "Your quiz session expired. Please start a new quiz.";
            return RedirectToAction(nameof(Setup));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer");
            return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
        }
    }

    /// <summary>
    /// GET: /GradedQuiz/Results
    /// Displays the final score and question review.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Results()
    {
        try
        {
            var sessionId = TempData[SessionKey]?.ToString();
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("No session ID in TempData for Results");
                return RedirectToAction(nameof(Setup));
            }

            var session = await _quizService.GetQuizSessionAsync(sessionId);
            if (session == null || !session.IsComplete)
            {
                _logger.LogWarning("Session not found or not complete for Results: {SessionId}", sessionId);
                return RedirectToAction(nameof(Setup));
            }

            var score = await _quizService.GetCurrentScoreAsync(sessionId);

            var viewModel = new GradedQuizResultViewModel
            {
                CorrectCount = score.CorrectCount,
                IncorrectCount = score.IncorrectCount,
                TotalQuestions = score.TotalQuestions,
                Percentage = score.Percentage,
                PerformanceRating = score.PerformanceRating,
                Questions = session.Questions,
                UserAnswers = session.UserAnswers
            };

            _logger.LogInformation("Results page displayed for session {SessionId}: {CorrectCount}/{TotalQuestions} ({Percentage}%)",
                sessionId, score.CorrectCount, score.TotalQuestions, score.Percentage);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying results");
            return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
        }
    }

    /// <summary>
    /// GET: /GradedQuiz/RetakeQuiz
    /// Clears the current quiz session and redirects to setup.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> RetakeQuiz()
    {
        try
        {
            var sessionId = TempData[SessionKey]?.ToString();
            if (!string.IsNullOrEmpty(sessionId))
            {
                await _quizService.ClearQuizSessionAsync(sessionId);
                _logger.LogInformation("Quiz session cleared for retake: {SessionId}", sessionId);
            }

            return RedirectToAction(nameof(Setup));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing quiz session");
            return RedirectToAction(nameof(Setup));
        }
    }
}
