using StudyHelper.Models;
using Microsoft.Extensions.Caching.Memory;

namespace StudyHelper.Services;

/// <summary>
/// Service for parsing markdown files from the InputDocuments project.
/// </summary>
public class MarkdownParserService : IMarkdownParserService
{
    private readonly ILogger<MarkdownParserService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IUserStudyMaterialService _userStudyMaterialService;
    private const string CacheKey = "MarkdownSections";

    public MarkdownParserService(
        ILogger<MarkdownParserService> logger, 
        IMemoryCache cache,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IUserStudyMaterialService userStudyMaterialService)
    {
        _logger = logger;
        _cache = cache;
        _configuration = configuration;
        _environment = environment;
        _userStudyMaterialService = userStudyMaterialService ?? throw new ArgumentNullException(nameof(userStudyMaterialService));
    }

    /// <summary>
    /// Parses all markdown files in the InputDocuments directory and extracts structured sections.
    /// Uses custom user-uploaded file if available, otherwise falls back to default.
    /// Uses memory cache to avoid re-parsing on every request.
    /// </summary>
    public async Task<List<MarkdownSection>> ParseMarkdownFilesAsync(string? username = null)
    {
        // Use per-user cache key if username provided
        var cacheKey = string.IsNullOrWhiteSpace(username) 
            ? CacheKey 
            : $"{CacheKey}_{username}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            _logger.LogInformation("Parsing markdown files (cache miss) for user: {Username}", username ?? "default");

            var sections = new List<MarkdownSection>();

            // Check if user has custom terms and definitions for quiz content
            if (!string.IsNullOrWhiteSpace(username))
            {
                var hasCustomContent = await _userStudyMaterialService.HasCustomMaterialAsync(username, StudyMaterialType.TermsAndDefinitions);

                if (hasCustomContent)
                {
                    var customFilePath = await _userStudyMaterialService.GetEffectiveFilePathAsync(username, StudyMaterialType.TermsAndDefinitions);
                    _logger.LogInformation("Using custom terms and definitions for quiz content for user {Username}: {Path}", username, customFilePath);

                    if (File.Exists(customFilePath))
                    {
                        var fileSections = await ParseFileAsync(customFilePath);
                        sections.AddRange(fileSections);
                        _logger.LogInformation("Parsed {Count} sections from custom terms and definitions", fileSections.Count);

                        if (sections.Count == 0)
                        {
                            _logger.LogError("No valid sections extracted from custom terms and definitions");
                            throw new InvalidOperationException("No valid sections extracted from custom terms and definitions");
                        }

                        return sections;
                    }
                }
            }

            // Fall back to default InputDocuments path
            var inputDocumentsPath = GetInputDocumentsPath();

            if (!Directory.Exists(inputDocumentsPath))
            {
                _logger.LogError("InputDocuments directory not found at path: {Path}", inputDocumentsPath);
                throw new FileNotFoundException($"InputDocuments directory not found at path: {inputDocumentsPath}");
            }

            var markdownFiles = Directory.GetFiles(inputDocumentsPath, "*.md", SearchOption.AllDirectories);

            if (markdownFiles.Length == 0)
            {
                _logger.LogError("No markdown files found in InputDocuments directory");
                throw new InvalidOperationException("No markdown files found in InputDocuments directory");
            }

            foreach (var filePath in markdownFiles)
            {
                try
                {
                    var fileSections = await ParseFileAsync(filePath);
                    sections.AddRange(fileSections);
                    _logger.LogInformation("Parsed {Count} sections from file: {FileName}", fileSections.Count, Path.GetFileName(filePath));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse file: {FilePath}", filePath);
                }
            }

            if (sections.Count == 0)
            {
                _logger.LogError("No valid sections extracted from markdown files");
                throw new InvalidOperationException("No valid sections extracted from markdown files");
            }

            return sections;
        }) ?? new List<MarkdownSection>();
    }

    /// <summary>
    /// Parses a single markdown file and extracts sections.
    /// </summary>
    private async Task<List<MarkdownSection>> ParseFileAsync(string filePath)
    {
        var sections = new List<MarkdownSection>();
        var lines = await File.ReadAllLinesAsync(filePath);
        var moduleName = ExtractModuleName(filePath);

        MarkdownSection? currentSection = null;
        var inCodeBlock = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Toggle code block state
            if (trimmedLine.StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            // Skip lines inside code blocks
            if (inCodeBlock)
            {
                continue;
            }

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            // Check if line is a heading
            if (trimmedLine.StartsWith("##"))
            {
                // Save previous section if it exists
                if (currentSection != null && !string.IsNullOrEmpty(currentSection.Heading))
                {
                    sections.Add(currentSection);
                }

                // Start new section
                var headingText = trimmedLine.TrimStart('#').Trim();
                // Remove trailing ^ if present
                headingText = headingText.TrimEnd('^').Trim();

                currentSection = new MarkdownSection
                {
                    Module = moduleName,
                    Heading = headingText
                };
            }
            else if (currentSection != null)
            {
                // Check if line is a term-definition pair (e.g., "Term: Definition")
                var colonIndex = trimmedLine.IndexOf(':');
                if (colonIndex > 0 && colonIndex < trimmedLine.Length - 1)
                {
                    var term = trimmedLine.Substring(0, colonIndex).Trim();
                    var definition = trimmedLine.Substring(colonIndex + 1).Trim();

                    // Validate that this looks like a term-definition pair
                    // Term should be reasonable length and not empty, definition should have substance
                    if (!string.IsNullOrWhiteSpace(term) && 
                        !string.IsNullOrWhiteSpace(definition) &&
                        term.Length <= 200 && 
                        definition.Length >= 10 &&
                        !term.Contains('\t') &&
                        !term.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        // This appears to be a term-definition pair
                        currentSection.TermDefinitions[term] = definition;
                        continue;
                    }
                }

                // Check if line is a bullet point
                if (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("*"))
                {
                    var bulletText = trimmedLine.TrimStart('-', '*').Trim();
                    if (!string.IsNullOrWhiteSpace(bulletText))
                    {
                        currentSection.BulletPoints.Add(bulletText);
                    }
                }
                else
                {
                    // Add as content line
                    currentSection.ContentLines.Add(trimmedLine);
                }
            }
        }

        // Add the last section
        if (currentSection != null && !string.IsNullOrEmpty(currentSection.Heading))
        {
            sections.Add(currentSection);
        }

        return sections;
    }

    /// <summary>
    /// Extracts the module name from the file path (e.g., "Module1" from ".../Module1/Slides.md").
    /// </summary>
    private string ExtractModuleName(string filePath)
    {
        var directoryName = Path.GetFileName(Path.GetDirectoryName(filePath));
        return directoryName ?? "Unknown";
    }

    /// <summary>
    /// Resolves the path to the InputDocuments directory using configuration.
    /// Validates the path for security and existence.
    /// </summary>
    private string GetInputDocumentsPath()
    {
        // Read path from configuration
        var configuredPath = _configuration["QuizSettings:InputDocumentsPath"];

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException(
                "InputDocuments path not configured. Set 'QuizSettings:InputDocumentsPath' in appsettings.json");
        }

        // Resolve relative paths against the application content root
        string resolvedPath;
        if (Path.IsPathRooted(configuredPath))
        {
            resolvedPath = configuredPath;
        }
        else
        {
            resolvedPath = Path.Combine(_environment.ContentRootPath, configuredPath);
        }

        // Normalize the path and validate
        resolvedPath = Path.GetFullPath(resolvedPath);

        // Verify directory exists
        if (!Directory.Exists(resolvedPath))
        {
            throw new DirectoryNotFoundException(
                $"InputDocuments directory not found at: {resolvedPath}");
        }

        _logger.LogDebug("Resolved InputDocuments path: {Path}", resolvedPath);
        return resolvedPath;
    }
}
