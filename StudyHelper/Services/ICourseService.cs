using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Manages per-user course creation, deletion, active-course selection,
/// and resolves the file-system directory for a given course.
/// </summary>
public interface ICourseService
{
    /// <summary>
    /// Returns all courses for <paramref name="username"/>, ordered by creation date.
    /// Returns an empty list when no course_settings.dat exists yet.
    /// </summary>
    Task<List<Course>> GetCoursesAsync(string username);

    /// <summary>
    /// Returns the course flagged as active for <paramref name="username"/>,
    /// or null when no course has been activated.
    /// </summary>
    Task<Course?> GetActiveCourseAsync(string username);

    /// <summary>
    /// Creates a new course and its directory under App_Data/{username}/{courseName}/.
    /// Returns false when the user already has 10 courses or the course name already exists.
    /// </summary>
    Task<bool> AddCourseAsync(string username, string courseName, string instructor);

    /// <summary>
    /// Sets the named course as the active course, persisting the selection to
    /// course_settings.dat so it survives app restarts.
    /// </summary>
    Task SetActiveCourseAsync(string username, string courseName);

    /// <summary>
    /// Removes the course entry from course_settings.dat and deletes the course
    /// directory and all its contents.
    /// Returns false when the course is not found.
    /// </summary>
    Task<bool> RemoveCourseAsync(string username, string courseName);

    /// <summary>
    /// Returns the absolute file-system path to the course directory.
    /// Does not create the directory or check that it exists.
    /// </summary>
    string GetCourseDirectory(string username, string courseName);
}
