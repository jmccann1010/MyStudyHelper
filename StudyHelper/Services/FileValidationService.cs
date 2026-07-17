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

        if (string.IsNullOrWhiteSpace(content))
        {
            result.Warnings.Add("File is empty.");
            return Task.FromResult(result);
        }

        var lines = content.Split('\n');
        var lineNumber = 0;
        var inSection = false; // true once the first ## heading has been seen

        foreach (var rawLine in lines)
        {
            lineNumber++;
            var trimmed = rawLine.TrimEnd('\r').Trim();

            // Skip blank lines, top-level headings (#), and sub-headings (###, ####, …).
            // IsExactSectionHeading matches only "##" or "## text", so deeper levels are skipped.
            if (string.IsNullOrWhiteSpace(trimmed) || (trimmed.StartsWith('#') && !IsExactSectionHeading(trimmed)))
                continue;

            // --- Exact ## section heading -----------------------------------------
            if (IsExactSectionHeading(trimmed))
            {
                inSection = true;
                result.ParsedSectionCount++;
                continue;
            }

            // --- Bullet points — always valid, no further checking ----------------
            if (trimmed.StartsWith('-') || trimmed.StartsWith('*'))
                continue;

            // --- T-E2: dash-with-spaces heuristic (looks like a term but no colon)
            // Matches: not blank, not heading, not bullet, contains " - ", no ":"
            if (trimmed.Contains(" - ") && !trimmed.Contains(':'))
            {
                var preview = trimmed.Length > 60 ? trimmed[..60] + "…" : trimmed;
                result.Errors.Add(
                    $"Line {lineNumber}: '{preview}' appears to be a term but is missing a colon (:) separator.");
                continue;
            }

            // --- Try to parse as Term: Definition ---------------------------------
            var colonIndex = trimmed.IndexOf(':');

            // T-E4: colon is the last character — term present but definition missing entirely
            if (colonIndex > 0 && colonIndex == trimmed.Length - 1)
            {
                result.Errors.Add(
                    $"Line {lineNumber}: '{trimmed}' has a colon but no definition. " +
                    "Format must be 'Term: Definition'.");
                continue;
            }

            if (colonIndex > 0 && colonIndex < trimmed.Length - 1)
            {
                var term       = trimmed[..colonIndex].Trim();
                var definition = trimmed[(colonIndex + 1)..].Trim();

                var termValid =
                    !string.IsNullOrWhiteSpace(term)  &&
                    term.Length <= 200                 &&
                    !term.Contains('\t')               &&
                    !term.StartsWith("http", StringComparison.OrdinalIgnoreCase);

                var defValid = !string.IsNullOrWhiteSpace(definition);

                if (termValid && defValid)
                {
                    // T-E1: valid term found before any ## heading
                    if (!inSection)
                    {
                        result.Errors.Add(
                            $"Line {lineNumber}: Term-definition pair found before any section heading (##). " +
                            "All terms must be under a ## heading.");
                    }
                    else
                    {
                        result.ParsedTermCount++;
                    }
                    continue;
                }

                // Colon found but pair fails validity — T-E3 (only flag inside a section)
                if (inSection)
                {
                    result.Errors.Add(
                        $"Line {lineNumber}: Unexpected content. " +
                        "Only Term: Definition pairs and bullet points are expected under a ## heading.");
                }
                continue;
            }

            // --- Non-blank line with no colon, not a bullet, not a heading --------
            // Only flag as T-E3 when inside a section; outside a section these are
            // introductory paragraphs and are silently skipped.
            if (inSection && colonIndex < 0)
            {
                result.Errors.Add(
                    $"Line {lineNumber}: Unexpected content. " +
                    "Only Term: Definition pairs and bullet points are expected under a ## heading.");
            }
        }

        // Post-pass warnings (do not affect IsValid)
        if (result.ParsedSectionCount == 0)
            result.Warnings.Add(
                "No section headings (##) were found. Flashcards and quizzes may not work. Check your file format.");

        if (result.ParsedTermCount == 0)
            result.Warnings.Add(
                "No term-definition pairs were found. Ensure each entry is on its own line " +
                "in the format  Term: Definition  under a ## section heading.");

        // Reject when any structural errors were found
        result.IsValid = result.Errors.Count == 0;

        _logger.LogInformation(
            "Terms format validation: {Sections} section(s), {Terms} term(s), {Errors} error(s)",
            result.ParsedSectionCount, result.ParsedTermCount, result.Errors.Count);

        return Task.FromResult(result);
    }

    public Task<FileValidationResult> ValidateEquationsFormatAsync(string content)
    {
        var result = new FileValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(content))
        {
            result.Warnings.Add("File is empty.");
            return Task.FromResult(result);
        }

        var lines = content.Split('\n');
        var lineNumber = 0;

        // State-machine fields for the current block being parsed
        string? currentName    = null;
        string? currentSummary = null;
        string? currentEquation = null;
        int     blockStartLine = 0;

        // Emit and validate a completed block, then reset state.
        void FinaliseBlock()
        {
            if (currentName == null) return;

            if (currentSummary == null)
            {
                result.Errors.Add(
                    $"Equation '{currentName}' (started at line {blockStartLine}): Missing summary line. " +
                    "Add 'Equation Summary: ...' after the name.");
            }
            else if (currentEquation == null)
            {
                result.Errors.Add(
                    $"Equation '{currentName}' (started at line {blockStartLine}): No equation line found. " +
                    "Add a line in the format 'Equation: Left = Right'.");
            }
            else
            {
                result.ParsedEquationCount++;
            }

            currentName     = null;
            currentSummary  = null;
            currentEquation = null;
            blockStartLine  = 0;
        }

        foreach (var rawLine in lines)
        {
            lineNumber++;
            var trimmed = rawLine.TrimEnd('\r').Trim();

            // --- Blank line — block terminator -----------------------------------
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                FinaliseBlock();
                continue;
            }

            // --- Equation Name: --------------------------------------------------
            if (trimmed.StartsWith("Equation Name:", StringComparison.OrdinalIgnoreCase))
            {
                // Finalise any in-progress block first
                FinaliseBlock();

                currentName     = trimmed["Equation Name:".Length..].Trim();
                blockStartLine  = lineNumber;
                continue;
            }

            // --- Equation Summary: -----------------------------------------------
            if (trimmed.StartsWith("Equation Summary:", StringComparison.OrdinalIgnoreCase))
            {
                // E-E1: summary without a preceding name
                if (currentName == null)
                {
                    result.Errors.Add(
                        $"Line {lineNumber}: 'Equation Summary:' found without a preceding 'Equation Name:'. " +
                        "Each block must start with 'Equation Name:'.");
                    continue;
                }

                currentSummary = trimmed["Equation Summary:".Length..].Trim();
                continue;
            }

            // --- Equation: -------------------------------------------------------
            if (trimmed.StartsWith("Equation:", StringComparison.OrdinalIgnoreCase))
            {
                // E-E2: equation line without a preceding summary
                if (currentSummary == null)
                {
                    result.Errors.Add(
                        $"Line {lineNumber}: 'Equation:' found without a preceding 'Equation Summary:' " +
                        $"for equation '{currentName ?? "unknown"}'.");
                    continue;
                }

                var equationValue = trimmed["Equation:".Length..].Trim();

                // E-E5: no equals sign
                var equalsIndex = equationValue.IndexOf('=');
                if (equalsIndex < 0)
                {
                    result.Errors.Add(
                        $"Line {lineNumber}: Equation '{equationValue}' is missing an equals sign (=).");
                    continue;
                }

                var leftSide  = equationValue[..equalsIndex].Trim();
                var rightSide = equationValue[(equalsIndex + 1)..].Trim();

                // E-E6: blank left side
                if (string.IsNullOrWhiteSpace(leftSide))
                {
                    result.Errors.Add(
                        $"Line {lineNumber}: Equation '{equationValue}' has a blank left-hand side.");
                    continue;
                }

                // E-E7: blank right side
                if (string.IsNullOrWhiteSpace(rightSide))
                {
                    result.Errors.Add(
                        $"Line {lineNumber}: Equation '{equationValue}' has a blank right-hand side.");
                    continue;
                }

                currentEquation = equationValue;
                continue;
            }

            // Unrecognised non-blank lines are silently ignored (comments, headings, etc.)
        }

        // Finalise the last block if the file does not end with a blank line
        FinaliseBlock();

        // E-W1: no complete equations found (warning only — does not reject)
        if (result.ParsedEquationCount == 0)
            result.Warnings.Add(
                "No equations were found. Ensure each block has 'Equation Name:', " +
                "'Equation Summary:', and 'Equation: Left = Right'.");

        result.IsValid = result.Errors.Count == 0;

        _logger.LogInformation(
            "Equations format validation: {Count} equation(s), {Errors} error(s)",
            result.ParsedEquationCount, result.Errors.Count);

        return Task.FromResult(result);
    }

    // -------------------------------------------------------------------------
    // Heading helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns true only for an exact ## section heading:
    ///   "##"          — bare heading (no title text)
    ///   "## Some Title" — heading followed by a space and text
    /// Returns false for ### and deeper ("###", "#### ", etc.), which must be
    /// silently skipped rather than counted as sections.
    /// </summary>
    private static bool IsExactSectionHeading(string line) =>
        line == "##" || line.StartsWith("## ", StringComparison.Ordinal);
}
