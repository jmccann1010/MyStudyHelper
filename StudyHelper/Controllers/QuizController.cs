using StudyHelper.Models;
using StudyHelper.Services;
using StudyHelper.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace StudyHelper.Controllers;

/// <summary>
/// Controller for handling quiz operations including question display and answer validation.
/// </summary>
[Authorize]
public class QuizController : Controller
{
    private readonly IMarkdownParserService _markdownParserService;
    private readonly IQuestionGeneratorService _questionGeneratorService;
    private readonly ILogger<QuizController> _logger;

    public QuizController(
        IMarkdownParserService markdownParserService,
        IQuestionGeneratorService questionGeneratorService,
        ILogger<QuizController> logger)
    {
        _markdownParserService = markdownParserService;
        _questionGeneratorService = questionGeneratorService;
        _logger = logger;
    }

    /// <summary>
    /// GET: /Quiz/Question
    /// Generates and displays a new quiz question.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Question()
    {
        try
        {
            // Parse markdown files using the current user's custom materials if available
            var username = User.Identity?.Name;
            var sections = await _markdownParserService.ParseMarkdownFilesAsync(username);

            // Generate question
            var question = _questionGeneratorService.GenerateQuestion(sections);

            // Map to ViewModel
            var viewModel = new QuizQuestionViewModel
            {
                QuestionText = question.QuestionText,
                AnswerOptions = question.AnswerOptions,
                Module = question.Module,
                Topic = question.Topic,
                Direction = question.Direction,
                DirectionLabel = question.Direction == QuestionDirection.TermToDefinition 
                    ? "Term → Definition" 
                    : "Definition → Term"
            };

            // Store question in TempData for answer validation
            TempData["CurrentQuestion"] = JsonSerializer.Serialize(question);

            _logger.LogInformation("Generated question from {Module} - {Topic}", question.Module, question.Topic);

            return View(viewModel);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "InputDocuments directory not found");
            return View("Error", new ErrorViewModel
            {
                RequestId = HttpContext?.TraceIdentifier ?? "unknown"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to generate question");
            return View("Error", new ErrorViewModel
            {
                RequestId = HttpContext?.TraceIdentifier ?? "unknown"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating question");
            return View("Error", new ErrorViewModel
            {
                RequestId = HttpContext?.TraceIdentifier ?? "unknown"
            });
        }
    }

    /// <summary>
    /// POST: /Quiz/SubmitAnswer
    /// Validates the user's answer and displays the result.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SubmitAnswer([FromForm] int selectedAnswerIndex)
    {
        try
        {
            // Validate input
            if (selectedAnswerIndex < 0 || selectedAnswerIndex > 3)
            {
                _logger.LogWarning("Invalid answer index submitted: {Index}", selectedAnswerIndex);
                return BadRequest("Invalid answer selection. Please select an answer between A and D.");
            }

            // Retrieve question from TempData
            var questionJson = TempData["CurrentQuestion"] as string;
            if (string.IsNullOrEmpty(questionJson))
            {
                _logger.LogWarning("TempData expired or missing for answer submission");
                TempData["ErrorMessage"] = "Your session expired. Please start a new question.";
                return RedirectToAction(nameof(Question));
            }

            var question = JsonSerializer.Deserialize<QuizQuestion>(questionJson);
            if (question == null)
            {
                _logger.LogError("Failed to deserialize question from TempData");
                return RedirectToAction(nameof(Question));
            }

            // Validate question integrity
            if (question.AnswerOptions == null || question.AnswerOptions.Count != 4)
            {
                _logger.LogError("Invalid question structure: AnswerOptions count is {Count}", 
                    question.AnswerOptions?.Count ?? 0);
                return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
            }

            if (question.CorrectAnswerIndex < 0 || question.CorrectAnswerIndex >= question.AnswerOptions.Count)
            {
                _logger.LogError("Invalid question structure: CorrectAnswerIndex is {Index}", 
                    question.CorrectAnswerIndex);
                return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
            }

            // Validate answer
            var isCorrect = selectedAnswerIndex == question.CorrectAnswerIndex;

            // Create result
            var result = new QuizResult
            {
                IsCorrect = isCorrect,
                CorrectAnswerIndex = question.CorrectAnswerIndex,
                CorrectAnswerText = question.AnswerOptions[question.CorrectAnswerIndex],
                Explanation = question.Explanation,
                UserAnswerIndex = selectedAnswerIndex,
                UserAnswerText = question.AnswerOptions[selectedAnswerIndex]
            };

            // Map to ViewModel
            var viewModel = new QuizResultViewModel
            {
                IsCorrect = result.IsCorrect,
                FeedbackMessage = result.IsCorrect ? "Correct!" : "Incorrect",
                CorrectAnswerText = result.CorrectAnswerText,
                UserAnswerText = result.UserAnswerText,
                Explanation = result.Explanation,
                Module = question.Module,
                Topic = question.Topic,
                Direction = question.Direction,
                DirectionLabel = question.Direction == QuestionDirection.TermToDefinition 
                    ? "Term → Definition" 
                    : "Definition → Term"
            };

            _logger.LogInformation("User answered {IsCorrect} for question from {Module} - {Topic}",
                isCorrect ? "correctly" : "incorrectly", question.Module, question.Topic);

            return View("Result", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing answer submission");
            return View("Error", new ErrorViewModel
            {
                RequestId = HttpContext?.TraceIdentifier ?? "unknown"
            });
        }
    }
}
