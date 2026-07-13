using StudyHelper.Services;
using StudyHelper.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace StudyHelper.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly ICourseService _courseService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IUserService userService,
        ICourseService courseService,
        ILogger<AccountController> logger)
    {
        ArgumentNullException.ThrowIfNull(userService);
        ArgumentNullException.ThrowIfNull(courseService);
        ArgumentNullException.ThrowIfNull(logger);

        _userService   = userService;
        _courseService = courseService;
        _logger        = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")] // Max 5 attempts per IP per minute — brute-force protection
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userService.ValidateUserAsync(model.Username, model.Password);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Username)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            // 7-day persistent window; reduced from 30 days to limit stolen-cookie exposure
            ExpiresUtc = model.RememberMe 
                ? DateTimeOffset.UtcNow.AddDays(7) 
                : DateTimeOffset.UtcNow.AddHours(1)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        await _userService.UpdateLastLoginAsync(user.Username);

        // Restore the previously active course into session so all course-specific
        // features work immediately after login without requiring re-selection.
        var activeCourse = await _courseService.GetActiveCourseAsync(user.Username);
        if (activeCourse != null)
        {
            HttpContext.Session.SetString("ActiveCourseName",     activeCourse.CourseName);
            HttpContext.Session.SetString("ActiveCourseNameSafe", activeCourse.CourseName);
            _logger.LogInformation("Restored active course '{CourseName}' for user {Username}",
                activeCourse.CourseName, user.Username);
        }

        _logger.LogInformation("User {Username} logged in successfully", user.Username);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _userService.UserExistsAsync(model.Username))
        {
            ModelState.AddModelError("Username", "Username already exists");
            return View(model);
        }

        var success = await _userService.CreateUserAsync(model.Username, model.Password);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, "An error occurred creating your account. Please try again.");
            return View(model);
        }

        _logger.LogInformation("New user {Username} registered successfully", model.Username);

        // Auto-login after registration
        var user = await _userService.GetUserAsync(model.Username);
        if (user != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Username)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            await _userService.UpdateLastLoginAsync(user.Username);
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var username = User.Identity?.Name;
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("User {Username} logged out", username);

        return RedirectToAction("Login");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
