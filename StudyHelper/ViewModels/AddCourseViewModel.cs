using System.ComponentModel.DataAnnotations;

namespace StudyHelper.ViewModels;

/// <summary>
/// ViewModel for the Add Course form on the Course Settings page.
/// </summary>
public class AddCourseViewModel
{
    [Required(ErrorMessage = "Course name is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Course name must be between 1 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$",
        ErrorMessage = "Course name may only contain letters, numbers, hyphens, and underscores. No spaces allowed.")]
    [Display(Name = "Course Name")]
    public string CourseName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Instructor is required")]
    [StringLength(100, ErrorMessage = "Instructor name must not exceed 100 characters")]
    [Display(Name = "Instructor")]
    public string Instructor { get; set; } = string.Empty;
}
