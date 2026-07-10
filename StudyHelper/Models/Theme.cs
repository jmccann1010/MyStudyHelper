namespace StudyHelper.Models;

/// <summary>
/// Represents a visual theme with color and typography settings.
/// </summary>
public class Theme
{
    /// <summary>
    /// Unique identifier for the theme (matches CSS file name).
    /// Example: "theme-dark-mode"
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Display name for the theme.
    /// Example: "Dark Mode"
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// User-friendly description of the theme.
    /// Example: "Easy on the eyes with dark backgrounds and light text."
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Path to the theme CSS file relative to wwwroot.
    /// Example: "/css/themes/theme-dark-mode.css"
    /// </summary>
    public required string CssFile { get; set; }

    /// <summary>
    /// Array of hex color codes representing the theme's color palette.
    /// Used for visual preview swatches.
    /// Example: ["#1a1a1a", "#2d2d2d", "#0dcaf0", "#198754"]
    /// </summary>
    public required string[] ColorSwatches { get; set; }

    /// <summary>
    /// Font family used in this theme.
    /// Example: "Segoe UI, Arial, sans-serif"
    /// </summary>
    public required string FontFamily { get; set; }

    /// <summary>
    /// Indicates if this is the default theme applied when no preference is saved.
    /// </summary>
    public bool IsDefault { get; set; }
}
