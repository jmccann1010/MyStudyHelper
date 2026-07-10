using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service interface for generating multiple choice quiz questions from markdown content.
/// Supports bidirectional questions (term→definition and definition→term).
/// </summary>
public interface IQuestionGeneratorService
{
    /// <summary>
    /// Generates a multiple choice question with four answer options from parsed markdown sections.
    /// </summary>
    /// <param name="sections">The list of parsed markdown sections to generate questions from.</param>
    /// <param name="direction">Optional question direction. If null, a random direction will be selected (50/50 chance).</param>
    /// <returns>A quiz question with exactly 4 answer options in the specified or random direction.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the sections list is empty or insufficient content exists.</exception>
    QuizQuestion GenerateQuestion(List<MarkdownSection> sections, QuestionDirection? direction = null);
}
