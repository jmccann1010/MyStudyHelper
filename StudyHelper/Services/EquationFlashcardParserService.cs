using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service for parsing Equations.md and extracting equation flashcards.
/// </summary>
public class EquationFlashcardParserService : IEquationFlashcardParserService
{
    private readonly ILogger<EquationFlashcardParserService> _logger;
    private readonly IUserStudyMaterialService _studyMaterialService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public EquationFlashcardParserService(
        ILogger<EquationFlashcardParserService> logger,
        IUserStudyMaterialService studyMaterialService,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _studyMaterialService = studyMaterialService ?? throw new ArgumentNullException(nameof(studyMaterialService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// Parses the Equations.md file and extracts all equations for flashcard display.
    /// Uses custom user-uploaded file if available, otherwise falls back to default.
    /// </summary>
    public async Task<List<EquationFlashcard>> ParseEquationsAsync(string? username = null, string? courseName = null)
    {
        string filePath;

        if (!string.IsNullOrWhiteSpace(username))
        {
            // Prefer course-aware path; fall back to legacy when no course is active
            filePath = !string.IsNullOrWhiteSpace(courseName)
                ? await _studyMaterialService.GetEffectiveFilePathAsync(username, courseName, StudyMaterialType.Equations)
                : await _studyMaterialService.GetEffectiveFilePathAsync(username, StudyMaterialType.Equations);

            _logger.LogDebug("Using equations file for {Username}/{Course}: {Path}",
                username, courseName ?? "legacy", filePath);
        }
        else
        {
            var relativePath = _configuration["EquationsPath"]
                ?? Path.Combine("App_Data", "Equations.md");
            filePath = Path.Combine(_environment.ContentRootPath, relativePath);
            _logger.LogDebug("Using default equations file: {Path}", filePath);
        }

        if (!File.Exists(filePath))
        {
            _logger.LogError("Equations.md not found at {Path}", filePath);
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        _logger.LogInformation("Starting equation parsing from {Path}", filePath);

        var lines = await File.ReadAllLinesAsync(filePath);
        var equations = new List<EquationFlashcard>();

        string? currentName = null;
        string? currentSummary = null;
        string? currentEquation = null;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines - but complete current equation if we have one
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                if (!string.IsNullOrWhiteSpace(currentName) && 
                    !string.IsNullOrWhiteSpace(currentSummary) && 
                    !string.IsNullOrWhiteSpace(currentEquation))
                {
                    // Try to parse and add the equation
                    var equation = TryCreateEquation(currentName, currentSummary, currentEquation);
                    if (equation != null)
                    {
                        equations.Add(equation);
                        _logger.LogDebug("Parsed equation: {Name}", currentName);
                    }

                    // Reset for next equation
                    currentName = null;
                    currentSummary = null;
                    currentEquation = null;
                }
                continue;
            }

            // Parse "Equation Name: [name]"
            if (trimmedLine.StartsWith("Equation Name:", StringComparison.OrdinalIgnoreCase))
            {
                // If we have a previous complete equation, save it first
                if (!string.IsNullOrWhiteSpace(currentName) && 
                    !string.IsNullOrWhiteSpace(currentSummary) && 
                    !string.IsNullOrWhiteSpace(currentEquation))
                {
                    var equation = TryCreateEquation(currentName, currentSummary, currentEquation);
                    if (equation != null)
                    {
                        equations.Add(equation);
                        _logger.LogDebug("Parsed equation: {Name}", currentName);
                    }
                }

                // Start new equation
                currentName = trimmedLine.Substring("Equation Name:".Length).Trim();
                currentSummary = null;
                currentEquation = null;
                continue;
            }

            // Parse "Equation Summary: [summary]"
            if (trimmedLine.StartsWith("Equation Summary:", StringComparison.OrdinalIgnoreCase))
            {
                currentSummary = trimmedLine.Substring("Equation Summary:".Length).Trim();
                continue;
            }

            // Parse "Equation: [left] = [right]"
            if (trimmedLine.StartsWith("Equation:", StringComparison.OrdinalIgnoreCase))
            {
                currentEquation = trimmedLine.Substring("Equation:".Length).Trim();
                continue;
            }
        }

        // Handle last equation if file doesn't end with blank line
        if (!string.IsNullOrWhiteSpace(currentName) && 
            !string.IsNullOrWhiteSpace(currentSummary) && 
            !string.IsNullOrWhiteSpace(currentEquation))
        {
            var equation = TryCreateEquation(currentName, currentSummary, currentEquation);
            if (equation != null)
            {
                equations.Add(equation);
                _logger.LogDebug("Parsed equation: {Name}", currentName);
            }
        }

        if (equations.Count == 0)
        {
            _logger.LogWarning("No equations found in Equations.md");
            throw new InvalidOperationException("No valid equations found");
        }

        _logger.LogInformation("Parsed {Count} equations from Equations.md", equations.Count);
        return equations;
    }

    /// <summary>
    /// Attempts to create an EquationFlashcard from parsed components.
    /// Returns null if the equation is malformed.
    /// </summary>
    private EquationFlashcard? TryCreateEquation(string name, string summary, string equationText)
    {
        // Find the equals sign
        var equalsIndex = equationText.IndexOf('=');
        if (equalsIndex <= 0 || equalsIndex >= equationText.Length - 1)
        {
            _logger.LogWarning(
                "Skipping malformed equation (no valid '=' found). Name: {Name}, Equation: {Equation}", 
                name, equationText);
            return null;
        }

        // Split on the first equals sign
        var leftSide = equationText.Substring(0, equalsIndex).Trim();
        var rightSide = equationText.Substring(equalsIndex + 1).Trim();

        // Validate both sides are not empty
        if (string.IsNullOrWhiteSpace(leftSide) || string.IsNullOrWhiteSpace(rightSide))
        {
            _logger.LogWarning(
                "Skipping incomplete equation (empty left or right side). Name: {Name}", 
                name);
            return null;
        }

        return new EquationFlashcard
        {
            Name = name,
            Summary = summary,
            LeftSide = leftSide,
            RightSide = rightSide
        };
    }
}
