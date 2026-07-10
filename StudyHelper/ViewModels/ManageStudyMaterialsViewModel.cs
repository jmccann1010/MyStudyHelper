using StudyHelper.Models;

namespace StudyHelper.ViewModels;

/// <summary>
/// View model for the Manage Study Materials page.
/// </summary>
public class ManageStudyMaterialsViewModel
{
    public List<UserStudyMaterial> UserMaterials { get; set; } = new();
    public bool HasCustomTerms { get; set; }
    public bool HasCustomEquations { get; set; }

    /// <summary>
    /// Whether equation-based features are enabled for this user.
    /// Controls visibility of Exercise, Graded Exercises, and Equation Flashcards.
    /// </summary>
    public bool EquationsEnabled { get; set; } = true;

    public UserStudyMaterial? TermsMaterial => 
        UserMaterials.FirstOrDefault(m => m.MaterialType == StudyMaterialType.TermsAndDefinitions);

    public UserStudyMaterial? EquationsMaterial => 
        UserMaterials.FirstOrDefault(m => m.MaterialType == StudyMaterialType.Equations);
}
