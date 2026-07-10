using StudyHelper.Models;
using StudyHelper.Services;
using StudyHelper.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudyHelper.Controllers;

/// <summary>
/// Controller for handling equation flashcard operations.
/// </summary>
[Authorize]
public class EquationFlashcardController : Controller
{
    private readonly IEquationFlashcardParserService _parserService;
    private readonly ILogger<EquationFlashcardController> _logger;

    public EquationFlashcardController(
        IEquationFlashcardParserService parserService,
        ILogger<EquationFlashcardController> logger)
    {
        _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET: /EquationFlashcard/Card
    /// Displays a random equation flashcard.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Card()
    {
        try
        {
            // Parse all equations using the current user's custom materials if available
            var username = User.Identity?.Name;
            var equations = await _parserService.ParseEquationsAsync(username);

            // Select random equation using thread-safe Random.Shared
            var randomEquation = equations[Random.Shared.Next(equations.Count)];

            // Create ViewModel
            var viewModel = new EquationFlashcardViewModel
            {
                Name = randomEquation.Name,
                Summary = randomEquation.Summary,
                LeftSide = randomEquation.LeftSide,
                RightSide = randomEquation.RightSide
            };

            _logger.LogInformation("Displaying equation flashcard: {Name}", randomEquation.Name);

            return View(viewModel);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Equations.md not found");
            TempData["ErrorMessage"] = "Equation flashcards are temporarily unavailable.";
            return View("Error", new ErrorViewModel 
            { 
                RequestId = HttpContext?.TraceIdentifier ?? "unknown"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "No equations available");
            TempData["ErrorMessage"] = "No equation flashcards are currently available.";
            return View("Error", new ErrorViewModel 
            { 
                RequestId = HttpContext?.TraceIdentifier ?? "unknown"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in equation flashcard display");
            return View("Error", new ErrorViewModel 
            { 
                RequestId = HttpContext?.TraceIdentifier ?? "unknown"
            });
        }
    }
}
