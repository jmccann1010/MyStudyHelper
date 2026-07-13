using StudyHelper.Models;
using StudyHelper.Services;
using StudyHelper.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudyHelper.Controllers;

/// <summary>
/// Handles application settings pages, including appearance customization and course management.
/// </summary>
[Authorize]
public class SettingsController(ICourseService courseService, ILogger<SettingsController> logger) : Controller
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

    // -------------------------------------------------------------------------
    // Course Settings (US-001 through US-004)
    // -------------------------------------------------------------------------

    /// <summary>
    /// GET: /Settings/CourseSettings
    /// Displays all courses for the authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CourseSettings()
    {
        var username = User.Identity?.Name
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var courses = await courseService.GetCoursesAsync(username);
        var activeName = HttpContext.Session.GetString("ActiveCourseName");

        var viewModel = new CourseSettingsViewModel
        {
            Courses          = courses,
            ActiveCourseName = activeName,
            AtMaxCapacity    = courses.Count >= 10,
            AddCourse        = new AddCourseViewModel()
        };

        return View(viewModel);
    }

    /// <summary>
    /// POST: /Settings/AddCourse
    /// Validates and creates a new course for the authenticated user.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCourse([Bind(Prefix = "AddCourse")] AddCourseViewModel model)
    {
        var username = User.Identity?.Name
            ?? throw new UnauthorizedAccessException("User not authenticated");

        // Re-build the view model for error re-display before any further processing
        async Task<IActionResult> RedisplayWithErrors()
        {
            var courses = await courseService.GetCoursesAsync(username);
            return View("CourseSettings", new CourseSettingsViewModel
            {
                Courses          = courses,
                ActiveCourseName = HttpContext.Session.GetString("ActiveCourseName"),
                AtMaxCapacity    = courses.Count >= 10,
                AddCourse        = model
            });
        }

        if (!ModelState.IsValid)
            return await RedisplayWithErrors();

        var success = await courseService.AddCourseAsync(username, model.CourseName, model.Instructor);

        if (!success)
        {
            // AddCourseAsync returns false for duplicate name or max-course limit
            var courses = await courseService.GetCoursesAsync(username);
            if (courses.Count >= 10)
                ModelState.AddModelError(string.Empty, "You have reached the maximum of 10 courses.");
            else
                ModelState.AddModelError(nameof(model.CourseName), "A course with that name already exists.");

            return await RedisplayWithErrors();
        }

        // If this is the first course, it was auto-activated by CourseService — hydrate session
        var activeCourse = await courseService.GetActiveCourseAsync(username);
        if (activeCourse != null)
        {
            HttpContext.Session.SetString("ActiveCourseName",     activeCourse.CourseName);
            HttpContext.Session.SetString("ActiveCourseNameSafe", activeCourse.CourseName);
        }

        logger.LogInformation("User {Username} created course '{CourseName}'", username, model.CourseName);
        TempData["SuccessMessage"] = $"Course '{model.CourseName}' created successfully.";
        return RedirectToAction(nameof(CourseSettings));
    }

    /// <summary>
    /// POST: /Settings/SetActiveCourse
    /// Marks the named course as the active course and persists the selection.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActiveCourse(string courseName)
    {
        var username = User.Identity?.Name
            ?? throw new UnauthorizedAccessException("User not authenticated");

        if (string.IsNullOrWhiteSpace(courseName))
        {
            TempData["ErrorMessage"] = "Please select a valid course.";
            return RedirectToAction(nameof(CourseSettings));
        }

        await courseService.SetActiveCourseAsync(username, courseName);

        // Write session keys so all course-specific controllers can read them per-request
        HttpContext.Session.SetString("ActiveCourseName",     courseName);
        HttpContext.Session.SetString("ActiveCourseNameSafe", courseName); // already filesystem-safe

        logger.LogInformation("User {Username} activated course '{CourseName}'", username, courseName);
        TempData["SuccessMessage"] = $"'{courseName}' is now your active course.";
        return RedirectToAction(nameof(CourseSettings));
    }

    /// <summary>
    /// POST: /Settings/RemoveCourse
    /// Deletes the named course and its directory after user confirmation.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCourse(string courseName)
    {
        var username = User.Identity?.Name
            ?? throw new UnauthorizedAccessException("User not authenticated");

        if (string.IsNullOrWhiteSpace(courseName))
        {
            TempData["ErrorMessage"] = "Please select a valid course to remove.";
            return RedirectToAction(nameof(CourseSettings));
        }

        var removed = await courseService.RemoveCourseAsync(username, courseName);

        if (removed)
        {
            // Clear session if the removed course was the active one
            if (HttpContext.Session.GetString("ActiveCourseName") == courseName)
            {
                HttpContext.Session.Remove("ActiveCourseName");
                HttpContext.Session.Remove("ActiveCourseNameSafe");
            }

            logger.LogInformation("User {Username} removed course '{CourseName}'", username, courseName);
            TempData["SuccessMessage"] = $"Course '{courseName}' has been removed.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Course '{courseName}' was not found.";
        }

        return RedirectToAction(nameof(CourseSettings));
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
