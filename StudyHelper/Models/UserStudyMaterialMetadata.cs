namespace StudyHelper.Models;

/// <summary>
/// Metadata for a user's study materials and preferences.
/// Stored in App_Data/StudyMaterials/{username}/metadata.json
/// </summary>
public class UserStudyMaterialMetadata
{
    /// <summary>
    /// Username of the user (should match authenticated identity).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// List of uploaded study material files for this user.
    /// </summary>
    public List<UserStudyMaterial> Materials { get; set; } = new();

    /// <summary>
    /// Whether equation-based features are enabled for this user.
    /// Controls visibility of Exercise, Graded Exercises, and Equation Flashcards on home page.
    /// Defaults to true for backward compatibility.
    /// </summary>
    public bool EquationsEnabled { get; set; } = true;
}
