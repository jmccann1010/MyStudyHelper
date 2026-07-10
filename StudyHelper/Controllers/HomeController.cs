using StudyHelper.Models;
using StudyHelper.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace StudyHelper.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserStudyMaterialService _userStudyMaterialService;

        public HomeController(
            ILogger<HomeController> logger,
            IUserStudyMaterialService userStudyMaterialService)
        {
            _logger = logger;
            _userStudyMaterialService = userStudyMaterialService;
        }

        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            bool equationsEnabled = true; // Default for anonymous users

            if (!string.IsNullOrEmpty(username))
            {
                try
                {
                    equationsEnabled = await _userStudyMaterialService.GetEquationsEnabledAsync(username);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading equations preference for user {Username}, defaulting to enabled", username);
                    // Fail open - default to enabled
                    equationsEnabled = true;
                }
            }

            ViewBag.EquationsEnabled = equationsEnabled;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
