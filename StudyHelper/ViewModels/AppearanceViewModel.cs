using StudyHelper.Models;

namespace StudyHelper.ViewModels;

/// <summary>
/// View model for the Appearance settings page.
/// </summary>
public class AppearanceViewModel
{
    /// <summary>
    /// List of all available themes for user selection.
    /// </summary>
    public required List<Theme> AvailableThemes { get; set; }

    /// <summary>
    /// The ID of the currently active theme (if detectable).
    /// Optional: could be read from cookie or header in future.
    /// For now, JavaScript reads from localStorage.
    /// </summary>
    public string? CurrentThemeId { get; set; }

    /// <summary>
    /// Message to display (e.g., success after saving, or error).
    /// Optional: for future server-side feedback.
    /// </summary>
    public string? Message { get; set; }
}
