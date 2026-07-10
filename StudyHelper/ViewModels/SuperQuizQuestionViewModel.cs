using StudyHelper.Models;

namespace StudyHelper.ViewModels;

public class SuperQuizQuestionViewModel
{
    public string SessionId { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public List<string> AnswerOptions { get; set; } = new();
    public string Module { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public QuestionDirection Direction { get; set; }
    public string DirectionLabel { get; set; } = string.Empty;
    public SuperQuizProgress? Progress { get; set; }
}
