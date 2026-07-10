using StudyHelper.Models;

namespace StudyHelper.ViewModels;

/// <summary>
/// ViewModel for displaying a quiz question to the user.
/// Supports bidirectional questions (term→definition and definition→term).
/// </summary>
public class QuizQuestionViewModel
{
    /// <summary>
    /// Gets or sets the text of the question to be displayed.
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of answer options (always 4 items).
    /// </summary>
    public List<string> AnswerOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the module name.
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the topic or heading.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the direction of the question (term→definition or definition→term).
    /// </summary>
    public QuestionDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets the display label for the question direction.
    /// </summary>
    public string DirectionLabel { get; set; } = string.Empty;
}
