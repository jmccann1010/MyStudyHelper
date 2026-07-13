using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service for parsing the TermsAndDefinitions.md file and extracting term/definition pairs.
/// </summary>
public class TermDefinitionParserService : ITermDefinitionParserService
{
    private readonly ILogger<TermDefinitionParserService> _logger;
    private readonly IUserStudyMaterialService _studyMaterialService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public TermDefinitionParserService(
        ILogger<TermDefinitionParserService> logger,
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
    /// Parses the TermsAndDefinitions.md file and extracts all term/definition pairs.
    /// Uses custom user-uploaded file if available, otherwise falls back to default.
    /// </summary>
    public async Task<List<TermDefinition>> ParseTermDefinitionsAsync(string? username = null, string? courseName = null)
    {
        string filePath;

        if (!string.IsNullOrWhiteSpace(username))
        {
            // Prefer course-aware path; fall back to legacy when no course is active
            filePath = !string.IsNullOrWhiteSpace(courseName)
                ? await _studyMaterialService.GetEffectiveFilePathAsync(username, courseName, StudyMaterialType.TermsAndDefinitions)
                : await _studyMaterialService.GetEffectiveFilePathAsync(username, StudyMaterialType.TermsAndDefinitions);

            _logger.LogDebug("Using terms file for {Username}/{Course}: {Path}",
                username, courseName ?? "legacy", filePath);
        }
        else
        {
            var relativePath = _configuration["TermDefinitionsPath"]
                ?? "App_Data/TermsAndDefinitions.md";
            filePath = Path.Combine(_environment.ContentRootPath, relativePath);
            _logger.LogDebug("Using default terms file: {Path}", filePath);
        }

        if (!File.Exists(filePath))
        {
            _logger.LogError("TermsAndDefinitions.md not found at {Path}", filePath);
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var lines = await File.ReadAllLinesAsync(filePath);
        var terms = new List<TermDefinition>();
        string? currentSection = null;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            // Track section headers (## Section Name)
            if (trimmedLine.StartsWith("##"))
            {
                currentSection = trimmedLine.TrimStart('#').Trim();
                _logger.LogDebug("Entering section: {Section}", currentSection);
                continue;
            }

            // Skip markdown headers, horizontal rules, and italicized lines
            if (trimmedLine.StartsWith("#") || 
                trimmedLine.StartsWith("---") ||
                trimmedLine.StartsWith("*"))
            {
                continue;
            }

            // Parse "Term: Definition" format
            var colonIndex = trimmedLine.IndexOf(':');
            if (colonIndex > 0 && colonIndex < trimmedLine.Length - 1)
            {
                var term = trimmedLine.Substring(0, colonIndex).Trim();
                var definition = trimmedLine.Substring(colonIndex + 1).Trim();

                if (!string.IsNullOrWhiteSpace(term) && !string.IsNullOrWhiteSpace(definition))
                {
                    terms.Add(new TermDefinition
                    {
                        Term = term,
                        Definition = definition,
                        Section = currentSection
                    });

                    _logger.LogDebug("Parsed term: {Term} in section: {Section}", term, currentSection);
                }
                else
                {
                    _logger.LogDebug("Skipped malformed line (empty term or definition): {Line}", trimmedLine);
                }
            }
        }

        if (terms.Count == 0)
        {
            _logger.LogWarning("No terms found in TermsAndDefinitions.md");
            throw new InvalidOperationException("No valid term/definition pairs found");
        }

        _logger.LogInformation("Parsed {Count} terms from TermsAndDefinitions.md", terms.Count);
        return terms;
    }
}
