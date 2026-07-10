using StudyHelper.Models;
using StudyHelper.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudyHelper.Controllers;

/// <summary>
/// Handles application settings pages, including appearance customization.
/// </summary>
[Authorize]
public class SettingsController : Controller
{
    /// <summary>
    /// Displays the Appearance settings page with available themes.
    /// </summary>
    /// <returns>The Appearance view with theme options.</returns>
    public IActionResult Appearance()
    {
        var viewModel = new AppearanceViewModel
        {
            AvailableThemes = GetAvailableThemes(),
            CurrentThemeId = null  // Will be determined client-side via localStorage
        };

        return View(viewModel);
    }

    /// <summary>
    /// Returns the list of predefined themes.
    /// In future, this could load from configuration or database.
    /// </summary>
    private static List<Theme> GetAvailableThemes()
    {
        return new List<Theme>
        {
            new Theme
            {
                Id = "theme-default",
                Name = "Default",
                Description = "Light and clean with standard Bootstrap styling.",
                CssFile = "/css/themes/theme-default.css",
                ColorSwatches = new[] { "#667eea", "#764ba2", "#ffffff", "#f8f9fa" },
                FontFamily = "Segoe UI, Arial, sans-serif",
                IsDefault = true
            },
            new Theme
            {
                Id = "theme-dark-mode",
                Name = "Dark Mode",
                Description = "Easy on the eyes with dark backgrounds and light text.",
                CssFile = "/css/themes/theme-dark-mode.css",
                ColorSwatches = new[] { "#1a202c", "#2d3748", "#667eea", "#7c8fff" },
                FontFamily = "Segoe UI, Arial, sans-serif",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-high-contrast",
                Name = "High Contrast",
                Description = "Maximum contrast for accessibility and readability.",
                CssFile = "/css/themes/theme-high-contrast.css",
                ColorSwatches = new[] { "#000000", "#ffffff", "#ffff00", "#00ff00" },
                FontFamily = "Arial, sans-serif",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-ocean-blue",
                Name = "Ocean Blue",
                Description = "Professional and calming blue tones.",
                CssFile = "/css/themes/theme-ocean-blue.css",
                ColorSwatches = new[] { "#0066cc", "#004d99", "#e6f2ff", "#ffffff" },
                FontFamily = "Georgia, serif",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-warm-sunset",
                Name = "Warm Sunset",
                Description = "Friendly and inviting with warm red and pink tones.",
                CssFile = "/css/themes/theme-warm-sunset.css",
                ColorSwatches = new[] { "#ff6b6b", "#ee5a6f", "#fff5f5", "#ffffff" },
                FontFamily = "Verdana, sans-serif",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-forest-green",
                Name = "Forest Green",
                Description = "Nature-inspired green theme for a fresh look.",
                CssFile = "/css/themes/theme-forest-green.css",
                ColorSwatches = new[] { "#2d6a4f", "#1b4332", "#e8f5e9", "#ffffff" },
                FontFamily = "'Trebuchet MS', sans-serif",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-midnight-blue",
                Name = "Midnight Blue",
                Description = "Deep dark blue theme for late-night studying.",
                CssFile = "/css/themes/theme-midnight-blue.css",
                ColorSwatches = new[] { "#0a1929", "#1a2332", "#3d5afe", "#e3f2fd" },
                FontFamily = "'Segoe UI', Roboto, sans-serif",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-royal-purple",
                Name = "Royal Purple",
                Description = "Elegant purple theme with a touch of sophistication.",
                CssFile = "/css/themes/theme-royal-purple.css",
                ColorSwatches = new[] { "#9c27b0", "#7b1fa2", "#f3e5f5", "#ffffff" },
                FontFamily = "'Times New Roman', serif",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-amber-gold",
                Name = "Amber Gold",
                Description = "Warm golden tones for a bright, energetic feel.",
                CssFile = "/css/themes/theme-amber-gold.css",
                ColorSwatches = new[] { "#ff8f00", "#ef6c00", "#fff8e1", "#ffffff" },
                FontFamily = "'Century Gothic', sans-serif",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-cyberpunk",
                Name = "Cyberpunk",
                Description = "Neon dark theme inspired by futuristic aesthetics.",
                CssFile = "/css/themes/theme-cyberpunk.css",
                ColorSwatches = new[] { "#0d0221", "#1a0b2e", "#00ffff", "#ff00ff" },
                FontFamily = "'Courier New', monospace",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-slate-gray",
                Name = "Slate Gray",
                Description = "Professional neutral theme for focused work.",
                CssFile = "/css/themes/theme-slate-gray.css",
                ColorSwatches = new[] { "#607d8b", "#455a64", "#eceff1", "#ffffff" },
                FontFamily = "'Calibri', sans-serif",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-cherry-blossom",
                Name = "Cherry Blossom",
                Description = "Soft pink theme inspired by spring blossoms.",
                CssFile = "/css/themes/theme-cherry-blossom.css",
                ColorSwatches = new[] { "#e91e63", "#c2185b", "#fce4ec", "#ffffff" },
                FontFamily = "'Comic Sans MS', cursive",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-deep-teal",
                Name = "Deep Teal",
                Description = "Calming teal theme for a serene study environment.",
                CssFile = "/css/themes/theme-deep-teal.css",
                ColorSwatches = new[] { "#00897b", "#00695c", "#e0f2f1", "#ffffff" },
                FontFamily = "'Tahoma', sans-serif",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-espresso",
                Name = "Espresso",
                Description = "Rich brown dark theme for coffee lovers.",
                CssFile = "/css/themes/theme-espresso.css",
                ColorSwatches = new[] { "#1e1410", "#2d1f1a", "#d4a574", "#f5f5dc" },
                FontFamily = "'Garamond', serif",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-lavender-dream",
                Name = "Lavender Dream",
                Description = "Soft purple theme for a dreamy study experience.",
                CssFile = "/css/themes/theme-lavender-dream.css",
                ColorSwatches = new[] { "#9575cd", "#7e57c2", "#ede7f6", "#ffffff" },
                FontFamily = "'Palatino', serif",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-mint-fresh",
                Name = "Mint Fresh",
                Description = "Cool mint green theme for a refreshing feel.",
                CssFile = "/css/themes/theme-mint-fresh.css",
                ColorSwatches = new[] { "#26a69a", "#00897b", "#e0f7f5", "#ffffff" },
                FontFamily = "'Helvetica', sans-serif",
                IsDefault = false
            },
            new Theme
            {
                Id = "theme-crimson-night",
                Name = "Crimson Night",
                Description = "Deep red dark theme with romantic vibes.",
                CssFile = "/css/themes/theme-crimson-night.css",
                ColorSwatches = new[] { "#1a0510", "#2d0e1d", "#ff6b9d", "#ffe0eb" },
                FontFamily = "'Georgia', serif",
                IsDefault = false
            }
        };
    }
}
