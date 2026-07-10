using StudyHelper.Models;
using StudyHelper.Services;
using StudyHelper.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudyHelper.Controllers;

/// <summary>
/// Controller for handling flashcard operations.
/// </summary>
[Authorize]
public class FlashcardController : Controller
{
    private readonly ITermDefinitionParserService _parserService;
    private readonly ILogger<FlashcardController> _logger;

    public FlashcardController(
        ITermDefinitionParserService parserService,
        ILogger<FlashcardController> logger)
    {
        _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET: /Flashcard/Card
    /// Displays a random flashcard with term and definition.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Card()
    {
        try
        {
            // Parse all terms using the current user's custom materials if available
            var username = User.Identity?.Name;
            var terms = await _parserService.ParseTermDefinitionsAsync(username);

            // Select random term using thread-safe Random.Shared
            var randomTerm = terms[Random.Shared.Next(terms.Count)];

            // Create ViewModel
            var viewModel = new FlashcardViewModel
            {
                Term = randomTerm.Term,
                Definition = randomTerm.Definition,
                Section = randomTerm.Section
            };

            _logger.LogInformation(
                "Displaying flashcard from section: {Section}", 
                randomTerm.Section ?? "Uncategorized");

            return View(viewModel);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "TermsAndDefinitions.md not found");
            TempData["ErrorMessage"] = "Flashcard content is temporarily unavailable.";
            return View("Error", new ErrorViewModel 
            { 
                RequestId = HttpContext?.TraceIdentifier ?? "unknown"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "No terms available");
            TempData["ErrorMessage"] = "No flashcards are currently available.";
            return View("Error", new ErrorViewModel 
            { 
                RequestId = HttpContext?.TraceIdentifier ?? "unknown"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in flashcard display");
            return View("Error", new ErrorViewModel 
            { 
                RequestId = HttpContext?.TraceIdentifier ?? "unknown"
            });
        }
    }
}
