using StudyHelper.Models;
using StudyHelper.Services;
using StudyHelper.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

namespace StudyHelper.Controllers;

/// <summary>
/// Controller for managing user study materials (upload, delete, manage).
/// All file operations are scoped to the user's currently active course when one is set.
/// </summary>
[Authorize]
public class StudyMaterialsController : Controller
{
    private readonly IUserStudyMaterialService _materialService;
    private readonly ILogger<StudyMaterialsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public StudyMaterialsController(
        IUserStudyMaterialService materialService,
        ILogger<StudyMaterialsController> logger,
        IWebHostEnvironment environment)
    {
        _materialService = materialService ?? throw new ArgumentNullException(nameof(materialService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// Returns the active course name from session, or null when no course is selected.
    /// </summary>
    private string? GetActiveCourse() => HttpContext.Session.GetString("ActiveCourseNameSafe");

    /// <summary>
    /// GET: /StudyMaterials/Manage
    /// Display the study materials management page.
    /// When a course is active, lists materials for that course; otherwise falls back to legacy list.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Manage()
    {
        var username = User.Identity?.Name ?? throw new UnauthorizedAccessException("User not authenticated");

        try
        {
            var courseName = GetActiveCourse();

            // Use course-aware overload when a course is active
            var materials = courseName != null
                ? await _materialService.GetUserMaterialsAsync(username, courseName)
                : await _materialService.GetUserMaterialsAsync(username);

            var equationsEnabled = await _materialService.GetEquationsEnabledAsync(username);

            var viewModel = new ManageStudyMaterialsViewModel
            {
                UserMaterials      = materials,
                HasCustomTerms     = materials.Any(m => m.MaterialType == StudyMaterialType.TermsAndDefinitions),
                HasCustomEquations = materials.Any(m => m.MaterialType == StudyMaterialType.Equations),
                EquationsEnabled   = equationsEnabled
            };

            // Surface the active course name to the view via ViewData
            ViewData["ActiveCourseName"] = courseName;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading study materials for user {Username}", username);
            TempData["ErrorMessage"] = "Error loading study materials. Please try again.";
            return View(new ManageStudyMaterialsViewModel());
        }
    }

    /// <summary>
    /// POST: /StudyMaterials/UploadTerms
    /// Upload a TermsAndDefinitions.md file into the active course directory.
    /// <summary>
    /// POST: /StudyMaterials/UploadTerms
    /// Validates and saves a TermsAndDefinitions.md file for the active course.
    /// Returns a detailed success/warning/error message derived from the validation result.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadTerms(IFormFile file)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("UploadTerms called with no authenticated username.");
            return Unauthorized();
        }

        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a file to upload.";
            _logger.LogWarning("User {Username} attempted to upload empty terms file", username);
            return RedirectToAction(nameof(Manage));
        }

        var courseName = GetActiveCourse();

        var result = courseName != null
            ? await _materialService.UploadTermsAsync(username, courseName, file)
            : await _materialService.UploadTermsAsync(username, file);

        if (!result.IsValid)
        {
            _logger.LogWarning("User {Username}/{Course} failed to upload TermsAndDefinitions.md",
                username, courseName ?? "legacy");
            TempData["ErrorMessage"] = BuildErrorHtml(result.Errors);
        }
        else if (result.ParsedSectionCount == 0)
        {
            _logger.LogInformation("User {Username}/{Course} uploaded TermsAndDefinitions.md with zero sections",
                username, courseName ?? "legacy");
            TempData["WarningMessage"] =
                "Upload accepted, but no sections (## headings) were found. " +
                "Flashcards and quizzes may not work. Check your file format.";
        }
        else if (result.ParsedTermCount == 0)
        {
            _logger.LogInformation("User {Username}/{Course} uploaded TermsAndDefinitions.md with zero terms",
                username, courseName ?? "legacy");
            TempData["WarningMessage"] =
                "Upload accepted, but no term-definition pairs were found. " +
                "Ensure each entry is on its own line in the format \u2018Term: Definition\u2019 under a ## section heading.";
        }
        else
        {
            _logger.LogInformation("User {Username}/{Course} uploaded TermsAndDefinitions.md: {Sections} section(s), {Terms} term(s)",
                username, courseName ?? "legacy", result.ParsedSectionCount, result.ParsedTermCount);
            TempData["SuccessMessage"] =
                $"Upload successful. Found {result.ParsedSectionCount} section(s) and {result.ParsedTermCount} term(s) and definition(s).";
        }

        return RedirectToAction(nameof(Manage));
    }

    /// <summary>
    /// POST: /StudyMaterials/UploadEquations
    /// Validates and saves an Equations.md file for the active course.
    /// Returns a detailed success/warning/error message derived from the validation result.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadEquations(IFormFile file)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("UploadEquations called with no authenticated username.");
            return Unauthorized();
        }

        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a file to upload.";
            _logger.LogWarning("User {Username} attempted to upload empty equations file", username);
            return RedirectToAction(nameof(Manage));
        }

        var courseName = GetActiveCourse();

        var result = courseName != null
            ? await _materialService.UploadEquationsAsync(username, courseName, file)
            : await _materialService.UploadEquationsAsync(username, file);

        if (!result.IsValid)
        {
            _logger.LogWarning("User {Username}/{Course} failed to upload Equations.md",
                username, courseName ?? "legacy");
            TempData["ErrorMessage"] = BuildErrorHtml(result.Errors);
        }
        else if (result.ParsedEquationCount == 0)
        {
            _logger.LogInformation("User {Username}/{Course} uploaded Equations.md with zero equations",
                username, courseName ?? "legacy");
            TempData["WarningMessage"] =
                "Upload accepted, but no equations were found. Ensure each block has " +
                "\u2018Equation Name:\u2019, \u2018Equation Summary:\u2019, and \u2018Equation: Left = Right\u2019.";
        }
        else
        {
            _logger.LogInformation("User {Username}/{Course} uploaded Equations.md: {Count} equation(s)",
                username, courseName ?? "legacy", result.ParsedEquationCount);
            TempData["SuccessMessage"] = $"Upload successful. Found {result.ParsedEquationCount} equation(s).";
        }

        return RedirectToAction(nameof(Manage));
    }

    /// <summary>
    /// POST: /StudyMaterials/Delete
    /// Delete a study material from the active course directory.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(StudyMaterialType materialType)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("Delete called with no authenticated username.");
            return Unauthorized();
        }

        var courseName = GetActiveCourse();

        var success = courseName != null
            ? await _materialService.DeleteUserMaterialAsync(username, courseName, materialType)
            : await _materialService.DeleteUserMaterialAsync(username, materialType);

        if (success)
        {
            _logger.LogInformation("User {Username}/{Course} deleted {MaterialType}",
                username, courseName ?? "legacy", materialType);
            TempData["SuccessMessage"] = $"{materialType} deleted. Using default content.";
        }
        else
        {
            _logger.LogWarning("User {Username}/{Course} failed to delete {MaterialType}",
                username, courseName ?? "legacy", materialType);
            TempData["ErrorMessage"] = "Delete failed.";
        }

        return RedirectToAction(nameof(Manage));
    }

    /// <summary>
    /// GET: /StudyMaterials/DownloadTemplate?type=terms|equations
    /// Download a template file for study materials (always served from the global default).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DownloadTemplate(string type)
    {
        string fileName;
        string defaultPath;

        if (type == "terms")
        {
            fileName    = "TermsAndDefinitions_Template.md";
            defaultPath = Path.Combine(_environment.ContentRootPath, "App_Data", "TermsAndDefinitions.md");
        }
        else if (type == "equations")
        {
            fileName    = "Equations_Template.md";
            defaultPath = Path.Combine(_environment.ContentRootPath, "App_Data", "Equations.md");
        }
        else
        {
            _logger.LogWarning("Invalid template type requested: {Type}", type);
            return NotFound();
        }

        try
        {
            if (!System.IO.File.Exists(defaultPath))
            {
                _logger.LogError("Template file not found: {Path}", defaultPath);
                TempData["ErrorMessage"] = "Template file not available.";
                return RedirectToAction(nameof(Manage));
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(defaultPath);
            _logger.LogInformation("User {Username} downloaded template: {Type}", User.Identity?.Name, type);
            return File(fileBytes, "text/markdown", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading template: {Type}", type);
            TempData["ErrorMessage"] = "Error downloading template. Please try again.";
            return RedirectToAction(nameof(Manage));
        }
    }

    /// <summary>
    /// Builds an HTML-safe error list for display in the ErrorMessage banner.
    /// Caps the displayed list at 10 items and appends a truncation notice when needed.
    /// All error text is HTML-encoded to prevent injection.
    /// </summary>
    private static string BuildErrorHtml(List<string> errors)
    {
        const int MaxDisplayed = 10;

        var sb = new System.Text.StringBuilder();
        sb.Append("Upload failed. The following format errors were found:<ul>");

        var displayed = errors.Take(MaxDisplayed);
        foreach (var error in displayed)
            sb.Append($"<li>{HtmlEncoder.Default.Encode(error)}</li>");

        if (errors.Count > MaxDisplayed)
            sb.Append($"<li>...and {errors.Count - MaxDisplayed} more error(s). Please review the full file.</li>");

        sb.Append("</ul>");
        return sb.ToString();
    }

    /// <summary>
    /// POST: /StudyMaterials/UpdatePreferences
    /// Updates user preferences including equations enabled setting.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePreferences(bool equationsEnabled)
    {
        var username = User.Identity?.Name ?? throw new UnauthorizedAccessException("User not authenticated");

        try
        {
            await _materialService.SetEquationsEnabledAsync(username, equationsEnabled);

            TempData["SuccessMessage"] = "Your preferences have been saved successfully.";
            _logger.LogInformation("User {Username} updated equations enabled to {Enabled}", username, equationsEnabled);

            return RedirectToAction(nameof(Manage));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to update preferences for user {Username}", username);
            TempData["ErrorMessage"] = "Failed to save your preferences. Please try again.";
            return RedirectToAction(nameof(Manage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating preferences for user {Username}", username);
            TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
            return RedirectToAction(nameof(Manage));
        }
    }
}
