using StudyHelper.Models;

namespace StudyHelper.ViewModels;

/// <summary>
/// ViewModel for the Course Settings page — lists all user courses and
/// holds the Add Course form model.
/// </summary>
public class CourseSettingsViewModel
{
    public List<Course>       Courses          { get; set; } = [];
    public string?            ActiveCourseName { get; set; }

    /// <summary>True when the user has already reached the 10-course limit.</summary>
    public bool               AtMaxCapacity    { get; set; }

    /// <summary>Bound to the Add Course form on the page.</summary>
    public AddCourseViewModel AddCourse        { get; set; } = new();
}
