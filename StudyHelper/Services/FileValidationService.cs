using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service for validating uploaded study material files.
/// </summary>
public class FileValidationService : IFileValidationService
{
    private readonly ILogger<FileValidationService> _logger;

    private static readonly string[] DangerousPatterns = new[]
    {
        "<script", "javascript:", "onerror=", "onclick=", 
        "<iframe", "<object", "<embed", "eval(", "setTimeout(",
        "document.cookie", "window.location", "<form", "onload="
    };

    public FileValidationService(ILogger<FileValidationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FileValidationResult> ValidateMarkdownFileAsync(Stream fileStream, string fileName)
    {
        var result = new FileValidationResult { IsValid = true };

        // Validate extension
        if (!fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            result.IsValid = false;
            result.Errors.Add("File must have .md extension");
            _logger.LogWarning("File validation failed: Invalid extension for {FileName}", fileName);
        }

        // Validate file is readable
        try
        {
            using var reader = new StreamReader(fileStream, leaveOpen: true);
            var firstLine = await reader.ReadLineAsync();
            if (firstLine == null)
            {
                result.IsValid = false;
                result.Errors.Add("File appears to be empty or unreadable");
                _logger.LogWarning("File validation failed: Empty or unreadable file {FileName}", fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read uploaded file {FileName}", fileName);
            result.IsValid = false;
            result.Errors.Add("File could not be read");
        }

        return result;
    }

    public Task<FileValidationResult> ValidatePlainTextAsync(string content)
    {
        var result = new FileValidationResult { IsValid = true };

        if (string.IsNullOrEmpty(content))
        {
            return Task.FromResult(result);
        }

        var invalidChars = new List<char>();
        var lineNumber = 1;
        var charPosition = 0;

        foreach (char c in content)
        {
            charPosition++;

            // Track line numbers for better error reporting
            if (c == '\n')
            {
                lineNumber++;
                charPosition = 0;
                continue;
            }

            // Allow standard ASCII printable characters (32-126) plus common whitespace
            // ASCII 9 = Tab, 10 = LF, 13 = CR, 32-126 = printable ASCII
            if (c == '\t' || c == '\r' || (c >= 32 && c <= 126))
            {
                continue;
            }

            // Character is outside plain text range
            if (!invalidChars.Contains(c))
            {
                invalidChars.Add(c);
                var charCode = (int)c;
                var charDesc = c < 32 ? $"control character (ASCII {charCode})" : $"non-ASCII character '{c}' (code {charCode})";
                result.Errors.Add($"Invalid {charDesc} found at line {lineNumber}, position {charPosition}");
                _logger.LogWarning("Non-plain-text character detected: {Char} (ASCII {Code}) at line {Line}", c, charCode, lineNumber);
            }
        }

        if (invalidChars.Count > 0)
        {
            result.IsValid = false;
            result.Errors.Insert(0, $"File contains {invalidChars.Count} type(s) of non-ASCII/non-text characters. Please save as plain text (ASCII) encoding.");
            _logger.LogWarning("File validation failed: {Count} non-ASCII character types found", invalidChars.Count);
        }
        else
        {
            _logger.LogDebug("Plain text validation passed: all characters are ASCII");
        }

        return Task.FromResult(result);
    }

    public Task<FileValidationResult> ScanForMaliciousContentAsync(string content)
    {
        var result = new FileValidationResult { IsValid = true };

        foreach (var pattern in DangerousPatterns)
        {
            if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.Errors.Add($"Potentially dangerous pattern detected: {pattern}");
                _logger.LogWarning("Malicious content detected: {Pattern}", pattern);
            }
        }

        return Task.FromResult(result);
    }

    public Task<FileValidationResult> ValidateTermsFormatAsync(string content)
    {
        var result = new FileValidationResult { IsValid = true };

        // Look for expected markdown patterns for terms
        // Expected format: **Term** - Definition
        var lines = content.Split('\n');
        var termCount = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("**") && trimmed.Contains("**") && trimmed.Contains("-"))
            {
                termCount++;
            }
        }

        if (termCount == 0)
        {
            result.Warnings.Add("No terms found in expected format (**Term** - Definition). File may not work correctly with flashcards.");
            _logger.LogInformation("Terms format validation: No terms found in expected format");
        }
        else
        {
            _logger.LogInformation("Terms format validation: Found {TermCount} terms", termCount);
        }

        return Task.FromResult(result);
    }

    public Task<FileValidationResult> ValidateEquationsFormatAsync(string content)
    {
        var result = new FileValidationResult { IsValid = true };

        // Look for LaTeX equation markers
        if (!content.Contains("$$") && !content.Contains("\\["))
        {
            result.Warnings.Add("No LaTeX equations found (expected $$ or \\[ markers). File may not work correctly with quizzes and exercises.");
            _logger.LogInformation("Equations format validation: No LaTeX markers found");
        }
        else
        {
            _logger.LogInformation("Equations format validation: LaTeX markers found");
        }

        return Task.FromResult(result);
    }
}
