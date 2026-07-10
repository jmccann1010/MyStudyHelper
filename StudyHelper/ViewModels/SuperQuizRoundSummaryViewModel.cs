using StudyHelper.Models;

namespace StudyHelper.ViewModels;

public class SuperQuizRoundSummaryViewModel
{
    public string SessionId { get; set; } = string.Empty;
    public RoundSummary RoundSummary { get; set; } = new();
    public SuperQuizProgress? Progress { get; set; }
}
