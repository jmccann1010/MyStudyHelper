using StudyHelper.Models;

namespace StudyHelper.ViewModels;

/// <summary>
/// Summary shown when Super Quiz is completed.
/// </summary>
public class SuperQuizCompletionSummary
{
    public int TotalQuestions { get; set; }
    public int TotalRounds { get; set; }
    public TimeSpan TotalTime { get; set; }
    public List<RoundSummary> RoundHistory { get; set; } = new();
    public double OverallAccuracy { get; set; }
}
