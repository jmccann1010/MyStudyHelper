using StudyHelper.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Security;
using System.Text.RegularExpressions;

namespace StudyHelper.Services;

/// <summary>
/// Service that parses LaTeX-formatted equations from markdown files.
/// </summary>
public partial class EquationParserService : IEquationParserService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<EquationParserService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IUserStudyMaterialService _studyMaterialService;

    private const string CacheKeyPrefix = "SubjectMatterEquationsLatex_";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);

    // Regex patterns for LaTeX parsing
    [GeneratedRegex(@"\$\$(.*?)\$\$", RegexOptions.Singleline)]
    private static partial Regex LatexEquationPattern();

    [GeneratedRegex(@"\\frac\{([^}]+)\}\{([^}]+)\}")]
    private static partial Regex FracPattern();

    [GeneratedRegex(@"\\text\{([^}]+)\}")]
    private static partial Regex TextPattern();

    [GeneratedRegex(@"\*\*\[(Source|Inferred)\]\*\*")]
    private static partial Regex SourceTagPattern();

    public EquationParserService(
        IMemoryCache cache,
        ILogger<EquationParserService> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IUserStudyMaterialService studyMaterialService)
    {
        _cache = cache;
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
        _studyMaterialService = studyMaterialService ?? throw new ArgumentNullException(nameof(studyMaterialService));
    }

    /// <summary>
    /// Parses Equations.md and extracts all parseable equations.
    /// Uses custom user-uploaded file if available, otherwise falls back to default.
    /// </summary>
    public async Task<List<SubjectMatterEquation>> ParseEquationsAsync(string? username = null, string? courseName = null)
    {
        // Cache key is unique per user+course combination
        var cacheKey = (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(courseName))
            ? $"{CacheKeyPrefix}{username}_{courseName}"
            : string.IsNullOrWhiteSpace(username)
                ? $"{CacheKeyPrefix}default"
                : $"{CacheKeyPrefix}{username}";

        if (_cache.TryGetValue(cacheKey, out List<SubjectMatterEquation>? cachedEquations) && cachedEquations != null)
        {
            _logger.LogDebug("Returning {Count} cached equations for cache key {CacheKey}", cachedEquations.Count, cacheKey);
            return cachedEquations;
        }

        _logger.LogInformation("Parsing equations from Equations.md");

        try
        {
            string equationsFilePath;

            if (!string.IsNullOrWhiteSpace(username))
            {
                // Prefer course-aware path; fall back to legacy when no course is active
                equationsFilePath = !string.IsNullOrWhiteSpace(courseName)
                    ? await _studyMaterialService.GetEffectiveFilePathAsync(username, courseName, StudyMaterialType.Equations)
                    : await _studyMaterialService.GetEffectiveFilePathAsync(username, StudyMaterialType.Equations);

                _logger.LogDebug("Using equations file for {Username}/{Course}: {Path}",
                    username, courseName ?? "legacy", equationsFilePath);
            }
            else
            {
                var relativeFilePath = _configuration["ExerciseSettings:EquationsFilePath"]
                    ?? "App_Data/Equations.md";
                equationsFilePath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, relativeFilePath));
                _logger.LogDebug("Using default equations file: {Path}", equationsFilePath);
            }

            if (!File.Exists(equationsFilePath))
            {
                _logger.LogWarning("Equations.md not found at {Path}", equationsFilePath);
                return new List<SubjectMatterEquation>();
            }

            var content = await File.ReadAllTextAsync(equationsFilePath);
            var equations = ParseMarkdownContent(content);

            // Cache the results
            if (equations.Count > 0)
            {
                _cache.Set(cacheKey, equations, CacheExpiration);
                _logger.LogInformation("Parsed and cached {Count} equations from Equations.md with cache key {CacheKey}", equations.Count, cacheKey);
            }
            else
            {
                _logger.LogWarning("No parseable equations found in Equations.md");
            }

            return equations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Equations.md");
            return new List<SubjectMatterEquation>();
        }
    }

    /// <summary>
    /// Parses markdown content and extracts both LaTeX equations and labeled plain-text format equations.
    /// </summary>
    private List<SubjectMatterEquation> ParseMarkdownContent(string content)
    {
        var equations = new List<SubjectMatterEquation>();
        var currentHeading = "Unknown";
        var currentSourceTag = "";
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Track current heading (module/section)
            if (line.StartsWith("##") && !line.StartsWith("###"))
            {
                currentHeading = line.TrimStart('#').Trim();
                _logger.LogDebug("Found section: {Heading}", currentHeading);
            }

            // Track source tag
            var sourceMatch = SourceTagPattern().Match(line);
            if (sourceMatch.Success)
            {
                currentSourceTag = sourceMatch.Groups[1].Value;
            }

            // Look for LaTeX equations
            if (line.StartsWith("$$") && line.EndsWith("$$") && line.Length > 4)
            {
                var latexFormula = line.Substring(2, line.Length - 4).Trim();
                var equation = ParseLatexEquation(latexFormula, currentHeading, currentSourceTag);

                if (equation != null)
                {
                    equations.Add(equation);
                    _logger.LogDebug("Parsed LaTeX equation: {EquationId}", equation.EquationId);
                }
            }
            // Look for labeled format: "Equation Name:", "Equation Summary:", "Equation:"
            else if (line.StartsWith("Equation Name:", StringComparison.OrdinalIgnoreCase) && 
                     i + 2 < lines.Length)
            {
                var nameLine = line;
                var summaryLine = lines[i + 1].Trim();
                var equationLine = lines[i + 2].Trim();

                // Validate the format
                if (summaryLine.StartsWith("Equation Summary:", StringComparison.OrdinalIgnoreCase) &&
                    equationLine.StartsWith("Equation:", StringComparison.OrdinalIgnoreCase))
                {
                    var equation = ParseLabeledFormatEquation(nameLine, summaryLine, equationLine);
                    if (equation != null)
                    {
                        equations.Add(equation);
                        _logger.LogDebug("Parsed labeled-format equation: {Name}", equation.Name);
                        i += 2; // Skip the next two lines since we've processed them
                    }
                }
            }
            // Legacy support: plain-text 3-line format: Name, Topic - Summary, Equation
            else if (!string.IsNullOrWhiteSpace(line) && 
                     !line.StartsWith("#") && 
                     !line.Contains(":") &&
                     i + 2 < lines.Length)
            {
                var nextLine = lines[i + 1].Trim();
                var thirdLine = lines[i + 2].Trim();

                // Check if this matches the old 3-line pattern
                if (!string.IsNullOrWhiteSpace(nextLine) && 
                    nextLine.Contains(" - ") &&
                    !string.IsNullOrWhiteSpace(thirdLine) && 
                    thirdLine.Contains("="))
                {
                    var equation = ParsePlainTextEquation(line, nextLine, thirdLine);
                    if (equation != null)
                    {
                        equations.Add(equation);
                        _logger.LogDebug("Parsed legacy plain-text equation: {Name}", equation.Name);
                        i += 2; // Skip the next two lines since we've processed them
                    }
                }
            }
        }

        return equations;
    }

    /// <summary>
    /// Parses a single LaTeX equation string.
    /// </summary>
    private SubjectMatterEquation? ParseLatexEquation(string latex, string heading, string sourceTag)
    {
        try
        {
            // Clean LaTeX notation
            var cleaned = CleanLatex(latex);

            // Split on equals sign
            if (!cleaned.Contains('='))
                return null;

            var parts = cleaned.Split('=', 2);
            if (parts.Length != 2)
                return null;

            var leftSide = parts[0].Trim();
            var rightSide = parts[1].Trim();

            // Extract all variables
            var variables = ExtractVariables(cleaned);
            if (variables.Count < 2)
                return null; // Need at least 2 variables

            // Parse right side terms
            var terms = ParseRightSideTerms(rightSide);
            var equationType = DetermineEquationType(terms, cleaned);

            return new SubjectMatterEquation
            {
                EquationId = GenerateEquationId(leftSide, rightSide),
                Name = leftSide, // For LaTeX, use the left side as the name
                DisplayName = cleaned,
                LatexFormula = latex,
                Module = heading,
                Variables = variables,
                Formula = cleaned,
                Type = equationType,
                LeftSide = leftSide,
                RightSideTerms = terms,
                Explanation = heading,
                SourceTag = sourceTag
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LaTeX equation: {Latex}", latex);
            return null;
        }
    }

    /// <summary>
    /// Parses a labeled-format equation:
    /// Line 1: "Equation Name: [name]"
    /// Line 2: "Equation Summary: [summary]"
    /// Line 3: "Equation: [equation]"
    /// </summary>
    private SubjectMatterEquation? ParseLabeledFormatEquation(string nameLine, string summaryLine, string equationLine)
    {
        try
        {
            // Extract name (remove "Equation Name:" prefix)
            var name = nameLine.Substring("Equation Name:".Length).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // Extract summary (remove "Equation Summary:" prefix)
            var summary = summaryLine.Substring("Equation Summary:".Length).Trim();

            // Extract equation (remove "Equation:" prefix)
            var equation = equationLine.Substring("Equation:".Length).Trim();
            if (!equation.Contains('='))
                return null;

            var parts = equation.Split('=', 2);
            if (parts.Length != 2)
                return null;

            var leftSide = parts[0].Trim();
            var rightSide = parts[1].Trim();

            // Extract variables from the equation
            var variables = ExtractVariablesFromPlainText(equation);
            if (variables.Count < 2)
                return null; // Need at least 2 variables

            // Parse right side terms
            var terms = ParsePlainTextRightSideTerms(rightSide);
            var equationType = DetermineEquationType(terms, equation);

            // Module defaults to "General" for labeled format (no section headers typically)
            var module = "General";

            return new SubjectMatterEquation
            {
                EquationId = GenerateEquationId(leftSide, rightSide),
                Name = name,
                DisplayName = equation,
                Formula = equation,
                Module = module,
                Explanation = summary,
                Variables = variables,
                Type = equationType,
                LeftSide = leftSide,
                RightSideTerms = terms,
                LatexFormula = "", // No LaTeX for labeled format
                SourceTag = ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse labeled-format equation: {Name}", nameLine);
            return null;
        }
    }

    /// <summary>
    /// Parses a plain-text 3-line equation format:
    /// Line 1: Name (e.g., "Net Income")
    /// Line 2: Topic - Summary (e.g., "Income Statement - Overall performance measure...")
    /// Line 3: Equation (e.g., "Net Income = Revenues - Expenses + Gains - Losses")
    /// </summary>
    private SubjectMatterEquation? ParsePlainTextEquation(string nameLine, string topicLine, string equationLine)
    {
        try
        {
            // Parse the name
            var name = nameLine.Trim();

            // Parse topic and summary (split on " - ")
            var topicParts = topicLine.Split(new[] { " - " }, 2, StringSplitOptions.None);
            var topic = topicParts.Length > 0 ? topicParts[0].Trim() : "Unknown";
            var summary = topicParts.Length > 1 ? topicParts[1].Trim() : "";

            // Parse the equation
            if (!equationLine.Contains('='))
                return null;

            var parts = equationLine.Split('=', 2);
            if (parts.Length != 2)
                return null;

            var leftSide = parts[0].Trim();
            var rightSide = parts[1].Trim();

            // Extract variables from the equation
            var variables = ExtractVariablesFromPlainText(equationLine);
            if (variables.Count < 2)
                return null; // Need at least 2 variables

            // Parse right side terms
            var terms = ParsePlainTextRightSideTerms(rightSide);
            var equationType = DetermineEquationType(terms, equationLine);

            return new SubjectMatterEquation
            {
                EquationId = GenerateEquationId(leftSide, rightSide),
                Name = name,
                DisplayName = equationLine.Trim(),
                Formula = equationLine.Trim(),
                Module = topic,
                Explanation = summary,
                Variables = variables,
                Type = equationType,
                LeftSide = leftSide,
                RightSideTerms = terms,
                LatexFormula = "", // No LaTeX for plain-text format
                SourceTag = ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse plain-text equation: {Name}", nameLine);
            return null;
        }
    }

    /// <summary>
    /// Extracts variable names from plain-text equation.
    /// Variables are alphanumeric words (including spaces) that aren't operators.
    /// </summary>
    private static List<string> ExtractVariablesFromPlainText(string equation)
    {
        var variables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Split by operators and parentheses, keeping multi-word variables
        var operators = new[] { '=', '+', '-', '*', '/', '(', ')' };
        var parts = equation.Split(operators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && 
                !decimal.TryParse(trimmed, out _) && // Not a number
                trimmed.Length > 1) // At least 2 characters
            {
                variables.Add(trimmed);
            }
        }

        return variables.ToList();
    }

    /// <summary>
    /// Parses right-side terms from plain-text equation.
    /// For division operations, properly marks numerator and denominator.
    /// </summary>
    private static List<EquationTerm> ParsePlainTextRightSideTerms(string rightSide)
    {
        var terms = new List<EquationTerm>();
        var currentTerm = "";
        var currentOperator = "+";
        var hasDivision = rightSide.Contains('/');

        for (int i = 0; i < rightSide.Length; i++)
        {
            var ch = rightSide[i];

            if (ch == '+' || ch == '-')
            {
                if (!string.IsNullOrWhiteSpace(currentTerm))
                {
                    terms.Add(new EquationTerm
                    {
                        Variable = currentTerm.Trim(),
                        Operator = currentOperator,
                        IsNumerator = false,
                        IsDenominator = false
                    });
                    currentTerm = "";
                }
                currentOperator = ch == '+' ? "+" : "-";
            }
            else if (ch == '*' || ch == '/')
            {
                // Handle multiplication/division (ratio equations)
                if (!string.IsNullOrWhiteSpace(currentTerm))
                {
                    var term = new EquationTerm
                    {
                        Variable = currentTerm.Trim(),
                        Operator = currentOperator
                    };

                    // For division: mark the term before / as numerator
                    if (ch == '/')
                    {
                        term.IsNumerator = true;
                        term.IsDenominator = false;
                    }
                    else
                    {
                        term.IsNumerator = false;
                        term.IsDenominator = false;
                    }

                    terms.Add(term);
                    currentTerm = "";
                }
                currentOperator = ch == '*' ? "*" : "/";
            }
            else if (ch == '(' || ch == ')')
            {
                // Handle parentheses for complex equations
                continue;
            }
            else
            {
                currentTerm += ch;
            }
        }

        // Add the last term
        if (!string.IsNullOrWhiteSpace(currentTerm))
        {
            var lastTerm = new EquationTerm
            {
                Variable = currentTerm.Trim(),
                Operator = currentOperator
            };

            // If the last operator was division, this term is the denominator
            if (currentOperator == "/")
            {
                lastTerm.IsNumerator = false;
                lastTerm.IsDenominator = true;
            }
            else
            {
                lastTerm.IsNumerator = false;
                lastTerm.IsDenominator = false;
            }

            terms.Add(lastTerm);
        }

        return terms;
    }

    /// <summary>
    /// Cleans LaTeX notation from equation string.
    /// </summary>
    private static string CleanLatex(string latex)
    {
        // Remove \text{} wrappers
        var cleaned = TextPattern().Replace(latex, m => m.Groups[1].Value);

        // Convert \frac{A}{B} to (A / B)
        cleaned = FracPattern().Replace(cleaned, m =>
            $"({m.Groups[1].Value} / {m.Groups[2].Value})");

        // Replace LaTeX operators
        cleaned = cleaned.Replace(@"\times", "*");
        cleaned = cleaned.Replace(@"\div", "/");
        cleaned = cleaned.Replace(@"\pm", "±");
        cleaned = cleaned.Replace(@"\approx", "≈");

        return cleaned.Trim();
    }

    /// <summary>
    /// Extracts variable names from equation string.
    /// </summary>
    private static List<string> ExtractVariables(string equation)
    {
        if (equation.Length > 1000)
        {
            throw new ArgumentException("Equation exceeds maximum length of 1000 characters");
        }

        var variables = new HashSet<string>();

        // Remove parentheses for easier parsing
        var cleaned = equation.Replace("(", " ").Replace(")", " ");

        // Pattern: word characters and spaces (variable names)
        var pattern = @"[A-Za-z][A-Za-z\s]*[A-Za-z]|[A-Za-z]";
        var matches = Regex.Matches(cleaned, pattern);

        foreach (Match match in matches)
        {
            var variable = match.Value.Trim();

            // Filter out operator words and short words
            if (!string.IsNullOrWhiteSpace(variable) &&
                variable.Length > 1 &&
                !IsOperatorWord(variable))
            {
                variables.Add(variable);
            }
        }

        return variables.ToList();
    }

    /// <summary>
    /// Checks if a word is an operator/connector word to exclude.
    /// </summary>
    private static bool IsOperatorWord(string word)
    {
        var operators = new[] { "and", "or", "the", "of", "for", "in", "to", "by", "per" };
        return operators.Contains(word.ToLower());
    }

    /// <summary>
    /// Parses the right side of equation into terms.
    /// </summary>
    private List<EquationTerm> ParseRightSideTerms(string rightSide)
    {
        var terms = new List<EquationTerm>();

        // Check for fraction format: (A / B)
        if (rightSide.StartsWith("(") && rightSide.EndsWith(")") && rightSide.Contains('/'))
        {
            var inner = rightSide.Substring(1, rightSide.Length - 2);
            var fracParts = inner.Split('/', 2);

            if (fracParts.Length == 2)
            {
                var numeratorVars = ExtractVariables(fracParts[0]);
                var denominatorVars = ExtractVariables(fracParts[1]);

                foreach (var numer in numeratorVars)
                {
                    terms.Add(new EquationTerm
                    {
                        Variable = numer,
                        Operator = "/",
                        IsNumerator = true,
                        IsDenominator = false
                    });
                }

                foreach (var denom in denominatorVars)
                {
                    terms.Add(new EquationTerm
                    {
                        Variable = denom,
                        Operator = "/",
                        IsNumerator = false,
                        IsDenominator = true
                    });
                }

                return terms;
            }
        }

        // Parse sequential operators
        var currentOp = "+";
        var buffer = "";

        for (int i = 0; i < rightSide.Length; i++)
        {
            var ch = rightSide[i];

            if (ch == '+' || ch == '-' || ch == '*' || ch == '/')
            {
                if (!string.IsNullOrWhiteSpace(buffer))
                {
                    var vars = ExtractVariables(buffer);
                    foreach (var v in vars)
                    {
                        terms.Add(new EquationTerm { Variable = v, Operator = currentOp });
                    }
                    buffer = "";
                }
                currentOp = ch.ToString();
            }
            else
            {
                buffer += ch;
            }
        }

        // Process final buffer
        if (!string.IsNullOrWhiteSpace(buffer))
        {
            var vars = ExtractVariables(buffer);
            foreach (var v in vars)
            {
                terms.Add(new EquationTerm { Variable = v, Operator = currentOp });
            }
        }

        return terms;
    }

    /// <summary>
    /// Determines the equation type based on operators.
    /// </summary>
    private static EquationType DetermineEquationType(List<EquationTerm> terms, string formula)
    {
        if (!terms.Any())
            return EquationType.Complex;

        // Check for fraction (numerator/denominator)
        if (terms.Any(t => t.IsDenominator))
        {
            // Check if result is likely a ratio
            var leftSide = formula.Split('=')[0].Trim().ToLower();
            if (leftSide.Contains("ratio") || leftSide.Contains("roe") || 
                leftSide.Contains("roa") || leftSide.Contains("roi") ||
                leftSide.Contains("margin") || leftSide.Contains("turnover"))
            {
                return EquationType.Ratio;
            }
            return EquationType.Division;
        }

        var operators = terms.Select(t => t.Operator).Distinct().ToList();

        // Single operator type
        if (operators.Count == 1)
        {
            return operators[0] switch
            {
                "+" => EquationType.Addition,
                "-" => EquationType.Subtraction,
                "*" => EquationType.Multiplication,
                "/" => EquationType.Division,
                _ => EquationType.Complex
            };
        }

        // Multiple operators
        return EquationType.Complex;
    }

    /// <summary>
    /// Generates a unique identifier for an equation.
    /// </summary>
    private static string GenerateEquationId(string leftSide, string rightSide)
    {
        var left = SanitizeForId(leftSide);
        var right = SanitizeForId(rightSide);
        return $"{left}_{right}".Substring(0, Math.Min(50, $"{left}_{right}".Length));
    }

    /// <summary>
    /// Sanitizes a string for use in an ID.
    /// </summary>
    private static string SanitizeForId(string input)
    {
        return Regex.Replace(input, @"[^A-Za-z0-9]", "");
    }
}
