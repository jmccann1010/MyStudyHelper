namespace StudyHelper.Models;

/// <summary>
/// Represents a single question count option for Super Quiz selection.
/// Contains both the numeric count and a user-friendly display label.
/// </summary>
public class SuperQuizQuestionOption
{
    /// <summary>
    /// The actual number of questions for this option.
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// User-friendly label for this option (e.g., "10 Questions", "Half (25)", "Half + 10 (35)", "All (50)").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the option (e.g., "Quick Practice", "Moderate Practice", "Complete Mastery").
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this option is the default selection.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Indicates the option type for styling and categorization.
    /// </summary>
    public SuperQuizOptionType OptionType { get; set; }
}

/// <summary>
/// Categorizes Super Quiz question count options for UI presentation.
/// </summary>
public enum SuperQuizOptionType
{
    /// <summary>
    /// Fixed count option (10, 20, 30, etc.).
    /// </summary>
    Fixed,

    /// <summary>
    /// Half of available terms.
    /// </summary>
    Half,

    /// <summary>
    /// Half + increment (Half+10, Half+20, etc.).
    /// </summary>
    HalfPlus,

    /// <summary>
    /// All available terms.
    /// </summary>
    All
}
