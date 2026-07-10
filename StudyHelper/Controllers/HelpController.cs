using Microsoft.AspNetCore.Mvc;

namespace StudyHelper.Controllers;

/// <summary>
/// Controller for serving help documentation pages.
/// All help pages are publicly accessible (no authentication required).
/// </summary>
public class HelpController : Controller
{
    private readonly ILogger<HelpController> _logger;

    public HelpController(ILogger<HelpController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET: /Help
    /// Displays the main help overview page with links to all help topics.
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        _logger.LogInformation("Help overview page accessed");
        return View();
    }

    /// <summary>
    /// GET: /Help/Quiz
    /// Displays help documentation for the Quiz feature.
    /// </summary>
    [HttpGet]
    public IActionResult Quiz()
    {
        _logger.LogInformation("Quiz help page accessed");
        return View();
    }

    /// <summary>
    /// GET: /Help/GradedQuiz
    /// Displays help documentation for the Graded Quiz feature.
    /// </summary>
    [HttpGet]
    public IActionResult GradedQuiz()
    {
        _logger.LogInformation("Graded Quiz help page accessed");
        return View();
    }

    /// <summary>
    /// GET: /Help/SuperQuiz
    /// Displays help documentation for the Super Quiz feature.
    /// </summary>
    [HttpGet]
    public IActionResult SuperQuiz()
    {
        _logger.LogInformation("Super Quiz help page accessed");
        return View();
    }

    /// <summary>
    /// GET: /Help/Exercise
    /// Displays help documentation for the Exercise feature.
    /// </summary>
    [HttpGet]
    public IActionResult Exercise()
    {
        _logger.LogInformation("Exercise help page accessed");
        return View();
    }

    /// <summary>
    /// GET: /Help/GradedExercises
    /// Displays help documentation for the Graded Exercises feature.
    /// </summary>
    [HttpGet]
    public IActionResult GradedExercises()
    {
        _logger.LogInformation("Graded Exercises help page accessed");
        return View();
    }

    /// <summary>
    /// GET: /Help/TermFlashcards
    /// Displays help documentation for the Term Flashcards feature.
    /// </summary>
    [HttpGet]
    public IActionResult TermFlashcards()
    {
        _logger.LogInformation("Term Flashcards help page accessed");
        return View();
    }

    /// <summary>
    /// GET: /Help/EquationFlashcards
    /// Displays help documentation for the Equation Flashcards feature.
    /// </summary>
    [HttpGet]
    public IActionResult EquationFlashcards()
    {
        _logger.LogInformation("Equation Flashcards help page accessed");
        return View();
    }

    /// <summary>
    /// GET: /Help/StudyMaterials
    /// Displays help documentation for the Study Materials management feature.
    /// </summary>
    [HttpGet]
    public IActionResult StudyMaterials()
    {
        _logger.LogInformation("Study Materials help page accessed");
        return View();
    }

    /// <summary>
    /// GET: /Help/Account
    /// Displays help documentation for account management and authentication.
    /// </summary>
    [HttpGet]
    public IActionResult Account()
    {
        _logger.LogInformation("Account help page accessed");
        return View();
    }

    /// <summary>
    /// GET: /Help/Settings
    /// Displays help documentation for settings and appearance customization.
    /// </summary>
    [HttpGet]
    public IActionResult Settings()
    {
        _logger.LogInformation("Settings help page accessed");
        return View();
    }
}
