using StudyHelper.Models;

namespace StudyHelper.ViewModels;

public class SuperQuizResultViewModel
{
    public string SessionId { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string FeedbackMessage { get; set; } = string.Empty;
    public string CorrectAnswerText { get; set; } = string.Empty;
    public string UserAnswerText { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public SuperQuizProgress Progress { get; set; } = new();
    public SuperQuizNextAction NextAction { get; set; }

    public string NextButtonText => NextAction switch
    {
        SuperQuizNextAction.NextQuestion => "Next Question",
        SuperQuizNextAction.RoundComplete => "View Round Summary",
        SuperQuizNextAction.QuizComplete => "View Results",
        _ => "Continue"
    };

    public string NextActionUrl => NextAction switch
    {
        SuperQuizNextAction.NextQuestion => $"/SuperQuiz/Question?sessionId={SessionId}",
        SuperQuizNextAction.RoundComplete => $"/SuperQuiz/RoundSummary?sessionId={SessionId}",
        SuperQuizNextAction.QuizComplete => $"/SuperQuiz/Complete?sessionId={SessionId}",
        _ => "/"
    };
}
