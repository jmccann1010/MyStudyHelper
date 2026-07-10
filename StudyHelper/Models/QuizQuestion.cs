namespace StudyHelper.Models;

/// <summary>
/// Represents a single multiple choice question with four answer options.
/// Supports bidirectional questions (term→definition and definition→term).
/// </summary>
public class QuizQuestion
{
    /// <summary>
    /// Gets or sets the term from the study material.
    /// </summary>
    public string Term { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the definition from the study material.
    /// </summary>
    public string Definition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the direction of the question (term→definition or definition→term).
    /// Defaults to TermToDefinition for backward compatibility.
    /// </summary>
    public QuestionDirection Direction { get; set; } = QuestionDirection.TermToDefinition;

    /// <summary>
    /// Gets the question prompt based on direction.
    /// For TermToDefinition: returns the term.
    /// For DefinitionToTerm: returns the definition.
    /// </summary>
    public string Prompt => Direction == QuestionDirection.TermToDefinition ? Term : Definition;

    /// <summary>
    /// Gets the correct answer based on direction.
    /// For TermToDefinition: returns the definition.
    /// For DefinitionToTerm: returns the term.
    /// </summary>
    public string CorrectAnswer => Direction == QuestionDirection.TermToDefinition ? Definition : Term;

    /// <summary>
    /// Gets or sets the text of the question to be displayed to the user.
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of answer options. Must contain exactly 4 items.
    /// </summary>
    public List<string> AnswerOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the zero-based index of the correct answer (0-3).
    /// </summary>
    public int CorrectAnswerIndex { get; set; }

    /// <summary>
    /// Gets or sets the explanation for why the correct answer is correct.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the module name from which this question was generated (e.g., "Module1").
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the topic or heading from the markdown file.
    /// </summary>
    public string Topic { get; set; } = string.Empty;
}
