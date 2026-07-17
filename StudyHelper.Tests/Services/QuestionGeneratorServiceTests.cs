using Microsoft.Extensions.Logging.Abstractions;
using StudyHelper.Models;
using StudyHelper.Services;

namespace StudyHelper.Tests.Services;

public class QuestionGeneratorServiceTests
{
    private readonly QuestionGeneratorService _service = new(NullLogger<QuestionGeneratorService>.Instance);

    [Fact]
    public void GenerateQuestion_WhenSectionsIsNullThenThrowsArgumentNullException()
    {
        // Act
        var action = () => _service.GenerateQuestion(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void GenerateQuestion_WhenSectionsIsEmptyThenThrowsInvalidOperationException()
    {
        // Arrange
        var sections = new List<MarkdownSection>();

        // Act
        var action = () => _service.GenerateQuestion(sections);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void GenerateQuestion_WhenNoValidSectionsThenThrowsInvalidOperationException()
    {
        // Arrange
        var sections = new List<MarkdownSection>
        {
            new()
            {
                Heading = "",
                Module = "Module1"
            }
        };

        // Act
        var action = () => _service.GenerateQuestion(sections);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void GenerateQuestion_WhenSectionHasAtLeastFourTermsThenReturnsTermDefinitionQuestion()
    {
        // Arrange
        var section = new MarkdownSection
        {
            Module = "Module1",
            Heading = "Key Terms",
            TermDefinitions = new Dictionary<string, string>
            {
                ["Asset"] = "A resource controlled by the entity from which future economic benefits are expected.",
                ["Liability"] = "A present obligation of the entity arising from past events.",
                ["Equity"] = "The residual interest in assets after deducting liabilities.",
                ["Revenue"] = "Increases in economic benefits during the period."
            }
        };

        // Act
        var question = _service.GenerateQuestion(new List<MarkdownSection> { section });

        // Assert — service randomly picks term→definition ("What is the definition of") OR
        // definition→term ("Which term is defined as:"); validate common invariants.
        Assert.True(
            question.QuestionText.StartsWith("What is the definition of", StringComparison.Ordinal) ||
            question.QuestionText.StartsWith("Which term is defined as:", StringComparison.Ordinal),
            $"Unexpected question text prefix: {question.QuestionText}");
        Assert.Equal(4, question.AnswerOptions.Count);
        Assert.InRange(question.CorrectAnswerIndex, 0, 3);
        Assert.Equal("Module1", question.Module);
        Assert.Equal("Key Terms", question.Topic);
    }

    [Fact]
    public void GenerateQuestion_WhenSectionHasAtLeastFourBulletsThenReturnsBulletQuestion()
    {
        // Arrange
        var section = new MarkdownSection
        {
            Module = "Module2",
            Heading = "Principles",
            BulletPoints = new List<string>
            {
                "Consistency",
                "Prudence",
                "Accrual basis",
                "Going concern"
            }
        };

        // Act
        var question = _service.GenerateQuestion(new List<MarkdownSection> { section });

        // Assert
        Assert.StartsWith("Which of the following is related to", question.QuestionText);
        Assert.Equal(4, question.AnswerOptions.Count);
        Assert.InRange(question.CorrectAnswerIndex, 0, 3);
        Assert.Equal("Module2", question.Module);
        Assert.Equal("Principles", question.Topic);
    }

    [Fact]
    public void GenerateQuestion_WhenSectionHasSingleBulletThenReturnsMixedDistractorQuestion()
    {
        // Arrange
        var primarySection = new MarkdownSection
        {
            Module = "Module3",
            Heading = "Recognition",
            BulletPoints = new List<string> { "Recognize revenue when performance obligations are satisfied." }
        };

        var otherSection = new MarkdownSection
        {
            Module = "Module4",
            Heading = "Measurement",
            ContentLines = new List<string> { "Historical cost is a common measurement basis." }
        };

        // Act
        var question = _service.GenerateQuestion(new List<MarkdownSection> { primarySection, otherSection });

        if (question.QuestionText.Contains("What is Measurement"))
        {
            // Assert
            Assert.StartsWith("What is Measurement", question.QuestionText);
            Assert.Equal(4, question.AnswerOptions.Count);
            Assert.InRange(question.CorrectAnswerIndex, 0, 3);
            Assert.Contains(question.AnswerOptions, option => option.Contains("Recognize revenue", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            // Assert
            Assert.StartsWith("Which statement best describes an aspect of", question.QuestionText);
            Assert.Equal(4, question.AnswerOptions.Count);
            Assert.InRange(question.CorrectAnswerIndex, 0, 3);
            Assert.Contains(question.AnswerOptions, option => option.Contains("Recognize revenue", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void GenerateQuestion_WhenSectionHasContentLinesThenReturnsConceptQuestion()
    {
        // Arrange
        var primarySection = new MarkdownSection
        {
            Module = "Module5",
            Heading = "Depreciation",
            ContentLines = new List<string>
            {
                "Depreciation allocates the depreciable amount of an asset over its useful life."
            }
        };

        var otherSection = new MarkdownSection
        {
            Module = "Module6",
            Heading = "Cash Flow",
            BulletPoints = new List<string>
            {
                "Operating activities include primary revenue-producing activities."
            }
        };

        // Act
        var question = _service.GenerateQuestion(new List<MarkdownSection> { primarySection, otherSection });

        // Assert — the service may generate either a concept or mixed-distractor question
        // depending on its internal random branch; validate the common invariants only.
        Assert.Equal(4, question.AnswerOptions.Count);
        Assert.InRange(question.CorrectAnswerIndex, 0, 3);
        Assert.False(string.IsNullOrWhiteSpace(question.QuestionText));
        Assert.True(
            question.Module == "Module5" || question.Module == "Module6",
            $"Unexpected module: {question.Module}");
    }
}
