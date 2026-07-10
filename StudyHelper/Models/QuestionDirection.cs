namespace StudyHelper.Models;

/// <summary>
/// Defines the direction of a quiz question.
/// </summary>
public enum QuestionDirection
{
    /// <summary>
    /// Question displays a term and asks for the definition.
    /// This is the traditional quiz format.
    /// </summary>
    TermToDefinition = 0,

    /// <summary>
    /// Question displays a definition and asks for the term.
    /// This is the new bidirectional format.
    /// </summary>
    DefinitionToTerm = 1
}
