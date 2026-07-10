namespace StudyHelper.ViewModels;

/// <summary>
/// Progress information for displaying Super Quiz state.
/// </summary>
public class SuperQuizProgress
{
    public int TotalQuestions { get; set; }
    public int Mastered { get; set; }
    public int Remaining { get; set; }
    public int CurrentRound { get; set; }
    public int QuestionsLeftThisRound { get; set; }
    public double OverallProgress => TotalQuestions > 0 
        ? (double)Mastered / TotalQuestions * 100 
        : 0;
}
