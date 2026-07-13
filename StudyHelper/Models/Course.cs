namespace StudyHelper.Models;

/// <summary>
/// In-memory representation of a single course belonging to a user.
/// CourseName is filesystem-safe (letters, numbers, hyphens, underscores only — no spaces).
/// </summary>
public class Course
{
    public required string Username    { get; set; }
    public required string CourseName  { get; set; }
    public required string Instructor  { get; set; }
    public DateTime        CreatedDate { get; set; }
    public DateTime        UpdatedDate { get; set; }

    /// <summary>
    /// True when this is the currently active course for the user.
    /// Only one course per user may have IsActive = true.
    /// </summary>
    public bool IsActive { get; set; }
}
