namespace StudyHelper.Models;

/// <summary>
/// Represents the score state of a graded quiz.
/// </summary>
public class QuizScore
{
    /// <summary>
    /// Number of questions answered correctly.
    /// </summary>
    public int CorrectCount { get; set; }

    /// <summary>
    /// Number of questions answered incorrectly.
    /// </summary>
    public int IncorrectCount { get; set; }

    /// <summary>
    /// Total questions in the quiz.
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Gets the percentage score (0-100), rounded to 2 decimal places.
    /// </summary>
    public decimal Percentage => TotalQuestions > 0
        ? Math.Round((decimal)CorrectCount / TotalQuestions * 100, 2)
        : 0;

    /// <summary>
    /// Gets the performance rating based on percentage.
    /// </summary>
    public string PerformanceRating => Percentage switch
    {
        >= 90 => "Excellent",
        >= 80 => "Good",
        >= 70 => "Fair",
        >= 60 => "Poor",
        _ => "Needs Improvement"
    };
}
