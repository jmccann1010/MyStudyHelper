using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using StudyHelper.Services;

namespace StudyHelper.Tests.Services;

public class FileValidationServiceTests
{
    private readonly FileValidationService _service = new(NullLogger<FileValidationService>.Instance);

    [Fact]
    public async Task ValidateMarkdownFileAsync_WhenFileExtensionIsNotMarkdownThenReturnsInvalid()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("# Title"));

        // Act
        var result = await _service.ValidateMarkdownFileAsync(stream, "notes.txt");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("File must have .md extension", result.Errors);
    }

    [Fact]
    public async Task ValidateMarkdownFileAsync_WhenFileIsEmptyThenReturnsInvalid()
    {
        // Arrange
        using var stream = new MemoryStream(Array.Empty<byte>());

        // Act
        var result = await _service.ValidateMarkdownFileAsync(stream, "notes.md");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("File appears to be empty or unreadable", result.Errors);
    }

    [Fact]
    public async Task ValidateMarkdownFileAsync_WhenStreamIsUnreadableThenReturnsInvalid()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("# Content"));
        stream.Dispose();

        // Act
        var result = await _service.ValidateMarkdownFileAsync(stream, "notes.md");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("File could not be read", result.Errors);
    }

    [Fact]
    public async Task ValidateMarkdownFileAsync_WhenFileIsReadableMarkdownThenReturnsValid()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("# Heading\nBody"));

        // Act
        var result = await _service.ValidateMarkdownFileAsync(stream, "notes.md");

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidatePlainTextAsync_WhenContentContainsNonAsciiThenReturnsInvalid()
    {
        // Arrange
        const string content = "Valid line\nContains café";

        // Act
        var result = await _service.ValidatePlainTextAsync(content);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("non-ASCII", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidatePlainTextAsync_WhenContentIsAsciiThenReturnsValid()
    {
        // Arrange
        const string content = "Line one\nLine two\tTabbed";

        // Act
        var result = await _service.ValidatePlainTextAsync(content);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ScanForMaliciousContentAsync_WhenDangerousPatternExistsThenReturnsInvalid()
    {
        // Arrange
        const string content = "<script>alert('xss')</script>";

        // Act
        var result = await _service.ScanForMaliciousContentAsync(content);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("<script", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateTermsFormatAsync_WhenNoExpectedTermsThenAddsWarning()
    {
        // Arrange
        const string content = "Plain paragraph\nAnother line";

        // Act
        var result = await _service.ValidateTermsFormatAsync(content);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, warning => warning.Contains("No terms found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateEquationsFormatAsync_WhenNoLatexMarkersThenAddsWarning()
    {
        // Arrange
        const string content = "Revenue = Assets - Liabilities";

        // Act
        var result = await _service.ValidateEquationsFormatAsync(content);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, warning => warning.Contains("No LaTeX equations found", StringComparison.OrdinalIgnoreCase));
    }
}
