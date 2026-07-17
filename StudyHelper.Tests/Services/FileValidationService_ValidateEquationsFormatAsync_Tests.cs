using Microsoft.Extensions.Logging.Abstractions;
using StudyHelper.Services;

namespace StudyHelper.Tests.Services;

/// <summary>
/// Tests for FileValidationService.ValidateEquationsFormatAsync covering:
/// happy path, boundary, negative, error handling, and security cases.
/// </summary>
public class FileValidationService_ValidateEquationsFormatAsync_Tests
{
    private readonly FileValidationService _service = new(NullLogger<FileValidationService>.Instance);

    // -------------------------------------------------------------------------
    // Happy Path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ValidateEquationsFormatAsync_WellFormedFile_ReturnsValidWithCount()
    {
        // Arrange
        const string content = """
            Equation Name: Newton's Second Law
            Equation Summary: Relates force, mass, and acceleration.
            Equation: F = m * a

            Equation Name: Ohm's Law
            Equation Summary: Defines the relationship between voltage, current, and resistance.
            Equation: V = I * R
            """;

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.ParsedEquationCount);
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_SingleEquation_CountIsOne()
    {
        // Arrange
        const string content = """
            Equation Name: Kinetic Energy
            Equation Summary: Energy possessed by a moving object.
            Equation: KE = 0.5 * m * v^2
            """;

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, result.ParsedEquationCount);
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_HeadingsAndCommentsIgnored_DoNotCauseErrors()
    {
        // Arrange — lines that are not recognised keywords are silently skipped
        const string content = """
            # My Equations File

            Some introductory text that should be ignored.

            Equation Name: Ideal Gas Law
            Equation Summary: Describes the state of a hypothetical ideal gas.
            Equation: PV = nRT
            """;

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, result.ParsedEquationCount);
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_FileNotEndingWithBlankLine_LastBlockStillCounted()
    {
        // Arrange — no trailing blank line; FinaliseBlock must be called at end
        const string content =
            "Equation Name: E=mc2\nEquation Summary: Mass-energy equivalence.\nEquation: E = m * c^2";

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, result.ParsedEquationCount);
    }

    // -------------------------------------------------------------------------
    // Boundary
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ValidateEquationsFormatAsync_EmptyFile_ReturnsValidWithWarning()
    {
        // Act
        var result = await _service.ValidateEquationsFormatAsync("");

        // Assert
        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Warnings);
        Assert.Equal(0, result.ParsedEquationCount);
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_WhitespaceOnlyFile_ReturnsValidWithWarning()
    {
        // Act
        var result = await _service.ValidateEquationsFormatAsync("   \n\n\t\n");

        // Assert
        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_TenEquations_CountIsTen()
    {
        // Arrange — generate 10 complete equation blocks
        var blocks = Enumerable.Range(1, 10).Select(i =>
            $"Equation Name: Eq{i}\nEquation Summary: Summary for equation {i}.\nEquation: A{i} = B{i}\n");
        var content = string.Join("\n", blocks);

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(10, result.ParsedEquationCount);
    }

    // -------------------------------------------------------------------------
    // Negative / Error cases
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ValidateEquationsFormatAsync_MissingSummary_ReturnsError()
    {
        // Arrange — block has Name and Equation but no Summary
        const string content = """
            Equation Name: Missing Summary
            Equation: X = Y + Z
            """;

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Missing summary line"));
        Assert.Equal(0, result.ParsedEquationCount);
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_MissingEquationLine_ReturnsError()
    {
        // Arrange — block has Name and Summary but no Equation line
        const string content = """
            Equation Name: Missing Equation
            Equation Summary: This block has a name and summary but no equation line.
            """;

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("No equation line found"));
        Assert.Equal(0, result.ParsedEquationCount);
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_EquationWithoutEqualSign_ReturnsError()
    {
        // Arrange — E-E5
        const string content = """
            Equation Name: Bad Equation
            Equation Summary: This equation is missing an equals sign.
            Equation: Force mass acceleration
            """;

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("missing an equals sign"));
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_BlankLeftSide_ReturnsError()
    {
        // Arrange — E-E6
        const string content = """
            Equation Name: Blank Left
            Equation Summary: The left side of the equation is blank.
            Equation:  = something
            """;

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("blank left-hand side"));
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_BlankRightSide_ReturnsError()
    {
        // Arrange — E-E7
        const string content = """
            Equation Name: Blank Right
            Equation Summary: The right side of the equation is blank.
            Equation: something =
            """;

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("blank right-hand side"));
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_SummaryWithoutPrecedingName_ReturnsError()
    {
        // Arrange — E-E1: Summary appears with no Name before it
        const string content = """
            Equation Summary: Orphaned summary with no equation name.
            Equation: A = B
            """;

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("without a preceding 'Equation Name:'"));
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_EquationWithoutPrecedingSummary_ReturnsError()
    {
        // Arrange — E-E2: Equation line appears before Summary
        const string content = """
            Equation Name: Out of Order
            Equation: A = B + C
            Equation Summary: Summary appears after the equation line.
            """;

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("without a preceding 'Equation Summary:'"));
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_ZeroCompleteEquations_AddsWarning()
    {
        // Arrange — no equation blocks at all (only unrecognised lines)
        const string content = "Just plain text with no equation blocks defined here.";

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("No equations were found"));
        Assert.Equal(0, result.ParsedEquationCount);
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_MixedValidAndInvalidBlocks_CountsOnlyValid()
    {
        // Arrange — one good block and one block missing its summary
        const string content = """
            Equation Name: Good Equation
            Equation Summary: This one is complete and valid.
            Equation: P = F / A

            Equation Name: Bad Equation
            Equation: Q = m * c * delta_T
            """;

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(1, result.ParsedEquationCount);  // only the good block
    }

    // -------------------------------------------------------------------------
    // Security
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ValidateEquationsFormatAsync_OversizedInput_DoesNotThrow()
    {
        // Arrange — large repeated content (~5 MB)
        var block = "Equation Name: Test\nEquation Summary: Summary.\nEquation: A = B\n\n";
        var content = string.Concat(Enumerable.Repeat(block, 100_000));

        // Act
        var exception = await Record.ExceptionAsync(() => _service.ValidateEquationsFormatAsync(content));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_CaseInsensitiveKeywordMatching_Accepted()
    {
        // Arrange — keywords in different cases must still be recognised
        const string content = """
            EQUATION NAME: Case Test
            EQUATION SUMMARY: Tests case-insensitive keyword matching.
            EQUATION: X = Y
            """;

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, result.ParsedEquationCount);
    }
}
