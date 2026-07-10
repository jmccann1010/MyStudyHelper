using StudyHelper.Models;

namespace StudyHelper.ViewModels;

public class SuperQuizStartViewModel
{
    /// <summary>
    /// Time per question in seconds.
    /// </summary>
    private const double SecondsPerQuestion = 15.0;

    /// <summary>
    /// Time per question in minutes (calculated from seconds).
    /// </summary>
    private const double MinutesPerQuestion = SecondsPerQuestion / 60.0; // 0.25

    /// <summary>
    /// Minimum number of questions required for Super Quiz.
    /// Shared constant used by controller and service for validation.
    /// </summary>
    public const int MinimumTermsRequired = 4;

    /// <summary>
    /// Maximum reasonable question count to prevent abuse.
    /// Used for upper-bound validation in controller.
    /// </summary>
    public const int MaximumReasonableQuestionCount = 1000;

    /// <summary>
    /// Sentinel value indicating "All available questions" should be used.
    /// When passed to service methods, this value means use all available terms.
    /// </summary>
    public const int AllQuestionsIndicator = -1;

    /// <summary>
    /// Increment size for fixed and half-plus options (10, 20, 30, ... and Half+10, Half+20, ...).
    /// </summary>
    private const int IncrementSize = 10;

    /// <summary>
    /// Total number of available terms/definitions.
    /// Used to calculate all question count options.
    /// </summary>
    public int TotalAvailableTerms { get; set; }

    /// <summary>
    /// User's selected question count (default: 10).
    /// </summary>
    public int SelectedQuestionCount { get; set; } = 10;

    /// <summary>
    /// Cached list of available options to avoid regeneration.
    /// </summary>
    private List<SuperQuizQuestionOption>? _cachedOptions;

    /// <summary>
    /// Number of questions for the Half option.
    /// Calculated as TotalAvailableTerms / 2 (integer division).
    /// Minimum of 4 questions enforced to prevent edge cases.
    /// </summary>
    public int HalfCount => Math.Max(TotalAvailableTerms / 2, MinimumTermsRequired);

    /// <summary>
    /// Generates all available question count options based on total available terms.
    /// Returns options in sequence: 10, 20, 30, ..., Half, Half+10, Half+20, ..., All
    /// Results are cached to avoid regeneration on subsequent calls.
    /// </summary>
    public List<SuperQuizQuestionOption> GetAvailableOptions()
    {
        // Return cached options if already generated
        if (_cachedOptions != null)
        {
            return _cachedOptions;
        }

        var options = new List<SuperQuizQuestionOption>();
        int halfCount = HalfCount;
        int allCount = TotalAvailableTerms;

        // Generate fixed increment options (10, 20, 30, ...) up to but not exceeding Half
        for (int count = IncrementSize; count < halfCount; count += IncrementSize)
        {
            options.Add(new SuperQuizQuestionOption
            {
                QuestionCount = count,
                Label = $"{count} Questions",
                Description = count == IncrementSize ? "Quick Practice" : "Moderate Practice",
                IsDefault = count == IncrementSize,
                OptionType = SuperQuizOptionType.Fixed
            });
        }

        // Add Half option
        options.Add(new SuperQuizQuestionOption
        {
            QuestionCount = halfCount,
            Label = $"Half ({halfCount} Questions)",
            Description = "Balanced Coverage",
            IsDefault = false,
            OptionType = SuperQuizOptionType.Half
        });

        // Generate Half+ increment options (Half+10, Half+20, ...) up to but not exceeding All
        for (int count = halfCount + IncrementSize; count < allCount; count += IncrementSize)
        {
            int offset = count - halfCount;
            options.Add(new SuperQuizQuestionOption
            {
                QuestionCount = count,
                Label = $"Half + {offset} ({count} Questions)",
                Description = "Extended Practice",
                IsDefault = false,
                OptionType = SuperQuizOptionType.HalfPlus
            });
        }

        // Add All option
        options.Add(new SuperQuizQuestionOption
        {
            QuestionCount = allCount,
            Label = $"All ({allCount} Questions)",
            Description = "Complete Mastery",
            IsDefault = false,
            OptionType = SuperQuizOptionType.All
        });

        // Ensure at least one option is marked as default (typically the first option)
        if (!options.Any(o => o.IsDefault) && options.Count > 0)
        {
            options[0].IsDefault = true;
        }

        // Cache the generated options for future calls
        _cachedOptions = options;
        return options;
    }

    /// <summary>
    /// Gets the currently selected option details.
    /// </summary>
    public SuperQuizQuestionOption? GetSelectedOption()
    {
        return GetAvailableOptions().FirstOrDefault(o => o.QuestionCount == SelectedQuestionCount);
    }

    /// <summary>
    /// Estimated time in minutes based on selected question count.
    /// Calculation: 15 seconds per question = 0.25 minutes per question.
    /// </summary>
    public double EstimatedTimeMinutes => SelectedQuestionCount * MinutesPerQuestion;

    /// <summary>
    /// Formatted estimated time string.
    /// </summary>
    public string EstimatedTimeFormatted =>
        EstimatedTimeMinutes < 60
            ? $"{EstimatedTimeMinutes:F0} minutes"
            : $"{EstimatedTimeMinutes / 60:F1} hours";
}

