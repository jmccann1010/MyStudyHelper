using StudyHelper.Models;
using StudyHelper.Services;
using StudyHelper.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security;
using System.Text.Json;

namespace StudyHelper.Controllers;

/// <summary>
/// Controller for the Exercises feature.
/// </summary>
[Authorize]
public class ExerciseController : Controller
{
    private readonly IEquationParserService _equationParserService;
    private readonly IExerciseProblemGeneratorService _problemGeneratorService;
    private readonly ILogger<ExerciseController> _logger;

    public ExerciseController(
        IEquationParserService equationParserService,
        IExerciseProblemGeneratorService problemGeneratorService,
        ILogger<ExerciseController> logger)
    {
        _equationParserService = equationParserService;
        _problemGeneratorService = problemGeneratorService;
        _logger = logger;
    }

    /// <summary>
    /// Generates and displays a new exercise problem.
    /// GET: /Exercise/Problem
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Problem()
    {
        try
        {
            // Parse equations from Equations.md using the current user's custom materials if available
            var username = User.Identity?.Name;
            var courseName = HttpContext.Session.GetString("ActiveCourseNameSafe");
            var equations = await _equationParserService.ParseEquationsAsync(username, courseName);

            if (equations.Count == 0)
            {
                _logger.LogWarning("No equations found in Equations.md");
                TempData["ErrorMessage"] = "Exercise content is temporarily unavailable. Please try again later.";
                return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
            }

            // Generate problem
            var problem = _problemGeneratorService.GenerateProblem(equations);

            // Store problem in TempData for validation (JSON serialized)
            TempData["CurrentProblem"] = JsonSerializer.Serialize(problem);

            // Map to ViewModel
            var viewModel = new ExerciseProblemViewModel
            {
                ProblemText = problem.ProblemText,
                Module = problem.Module,
                EquationName = string.IsNullOrWhiteSpace(problem.Equation.Name) 
                    ? problem.Equation.DisplayName 
                    : problem.Equation.Name,
                EquationSummary = problem.Equation.Explanation,
                IsRatioResult = problem.IsRatioResult
            };

            _logger.LogInformation("Generated exercise from {Module}: {Equation}, solving for {Variable}",
                problem.Module, problem.Equation.DisplayName, problem.SolveForVariable);

            return View(viewModel);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "No solvable equations available");
            TempData["ErrorMessage"] = "Exercise content is temporarily unavailable. Please try again later.";
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Error reading Equations.md");
            TempData["ErrorMessage"] = "Unable to load exercise. Please try again later.";
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
        catch (SecurityException ex)
        {
            _logger.LogError(ex, "Security exception during exercise generation");
            TempData["ErrorMessage"] = "A security error occurred. Please contact support.";
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating exercise problem");
            TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Validates the user's answer and displays the result.
    /// POST: /Exercise/Submit
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Submit([FromForm] decimal userAnswer)
    {
        try
        {
            // Retrieve problem from TempData
            if (TempData["CurrentProblem"] is not string problemJson || string.IsNullOrEmpty(problemJson))
            {
                _logger.LogWarning("TempData expired for exercise submission");
                TempData["ErrorMessage"] = "Your session expired. Please start a new exercise.";
                return RedirectToAction(nameof(Problem));
            }

            var problem = JsonSerializer.Deserialize<ExerciseProblem>(problemJson);
            if (problem == null)
            {
                _logger.LogError("Failed to deserialize exercise problem");
                TempData["ErrorMessage"] = "An error occurred processing your answer. Please try a new exercise.";
                return RedirectToAction(nameof(Problem));
            }

            // Validate input range
            if (userAnswer < 0 || (problem.IsRatioResult && userAnswer > 100) || (!problem.IsRatioResult && userAnswer > 999999999.99m))
            {
                _logger.LogWarning("User submitted out-of-range answer for equation {EquationId}", problem.Equation.EquationId);
                TempData["ErrorMessage"] = problem.IsRatioResult
                    ? "Please enter a valid ratio between 0 and 100."
                    : "Please enter a valid amount between $0 and $999,999,999.99.";
                TempData["CurrentProblem"] = problemJson; // Restore problem for retry
                return RedirectToAction(nameof(Problem));
            }

            // Validate answer
            var result = _problemGeneratorService.ValidateAnswer(problem, userAnswer);

            // Map to ViewModel
            var viewModel = new ExerciseResultViewModel
            {
                IsCorrect = result.IsCorrect,
                UserAnswer = result.UserAnswer,
                CorrectAnswer = result.CorrectAnswer,
                FeedbackMessage = result.FeedbackMessage,
                SolutionSteps = result.SolutionSteps,
                ProblemText = problem.ProblemText,
                Module = problem.Module,
                IsRatioResult = problem.IsRatioResult
            };

            _logger.LogInformation("User answered {IsCorrect} for equation {EquationId}",
                result.IsCorrect ? "correctly" : "incorrectly",
                problem.Equation.EquationId);

            return View("Result", viewModel);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing exercise problem");
            TempData["ErrorMessage"] = "An error occurred processing your answer. Please try a new exercise.";
            return RedirectToAction(nameof(Problem));
        }
        catch (DivideByZeroException ex)
        {
            _logger.LogError(ex, "Division by zero in exercise calculation");
            TempData["ErrorMessage"] = "An error occurred with this exercise. Please try a new one.";
            return RedirectToAction(nameof(Problem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing exercise submission");
            TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
            return RedirectToAction(nameof(Problem));
        }
    }
}
