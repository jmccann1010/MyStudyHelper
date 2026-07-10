namespace StudyHelper.Models;

/// <summary>
/// Summary statistics for a completed round in Super Quiz.
/// </summary>
public class RoundSummary
{
    /// <summary>
    /// Round number (1-based).
    /// </summary>
    public int RoundNumber { get; set; }

    /// <summary>
    /// Total questions asked in this round.
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Number answered correctly in this round.
    /// </summary>
    public int CorrectAnswers { get; set; }

    /// <summary>
    /// Number answered incorrectly in this round.
    /// </summary>
    public int IncorrectAnswers { get; set; }

    /// <summary>
    /// Accuracy percentage for this round.
    /// </summary>
    public double AccuracyPercent => TotalQuestions > 0 
        ? (double)CorrectAnswers / TotalQuestions * 100 
        : 0;

    /// <summary>
    /// Round completion timestamp.
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
