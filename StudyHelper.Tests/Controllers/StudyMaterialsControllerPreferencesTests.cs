using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudyHelper.Controllers;
using StudyHelper.Models;
using StudyHelper.Services;
using StudyHelper.Tests.Helpers;
using StudyHelper.ViewModels;
using System.Security.Claims;

namespace StudyHelper.Tests.Controllers;

public class StudyMaterialsControllerPreferencesTests
{
    private readonly Mock<IUserStudyMaterialService> _mockMaterialService;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly StudyMaterialsController _controller;

    public StudyMaterialsControllerPreferencesTests()
    {
        _mockMaterialService = new Mock<IUserStudyMaterialService>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();

        _controller = new StudyMaterialsController(
            _mockMaterialService.Object,
            NullLogger<StudyMaterialsController>.Instance,
            _mockEnvironment.Object
        );

        // Setup controller context with authenticated user and a working session
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        httpContext.Session = new FakeSession(); // prevents InvalidOperationException from GetActiveCourse()

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Setup TempData
        _controller.TempData = new TempDataDictionary(
            _controller.HttpContext,
            Mock.Of<ITempDataProvider>()
        );
    }

    [Fact]
    public async Task Manage_LoadsEquationsEnabledFromService()
    {
        // Arrange
        _mockMaterialService
            .Setup(x => x.GetUserMaterialsAsync("testuser"))
            .ReturnsAsync(new List<UserStudyMaterial>());

        _mockMaterialService
            .Setup(x => x.GetEquationsEnabledAsync("testuser"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Manage();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ManageStudyMaterialsViewModel>(viewResult.Model);
        Assert.False(model.EquationsEnabled);
    }

    [Fact]
    public async Task Manage_WhenEquationsEnabledTrue_SetsViewModelTrue()
    {
        // Arrange
        _mockMaterialService
            .Setup(x => x.GetUserMaterialsAsync("testuser"))
            .ReturnsAsync(new List<UserStudyMaterial>());

        _mockMaterialService
            .Setup(x => x.GetEquationsEnabledAsync("testuser"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Manage();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ManageStudyMaterialsViewModel>(viewResult.Model);
        Assert.True(model.EquationsEnabled);
    }

    [Fact]
    public async Task Manage_WhenServiceThrowsException_ReturnsViewWithErrorMessage()
    {
        // Arrange
        _mockMaterialService
            .Setup(x => x.GetUserMaterialsAsync("testuser"))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.Manage();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
        Assert.Equal("Error loading study materials. Please try again.", _controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task UpdatePreferences_WhenEnabledTrue_CallsServiceAndRedirects()
    {
        // Arrange
        _mockMaterialService
            .Setup(x => x.SetEquationsEnabledAsync("testuser", true))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdatePreferences(true);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(StudyMaterialsController.Manage), redirectResult.ActionName);
        Assert.Equal("Your preferences have been saved successfully.", _controller.TempData["SuccessMessage"]);
        _mockMaterialService.Verify(x => x.SetEquationsEnabledAsync("testuser", true), Times.Once);
    }

    [Fact]
    public async Task UpdatePreferences_WhenEnabledFalse_CallsServiceAndRedirects()
    {
        // Arrange
        _mockMaterialService
            .Setup(x => x.SetEquationsEnabledAsync("testuser", false))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdatePreferences(false);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(StudyMaterialsController.Manage), redirectResult.ActionName);
        Assert.Equal("Your preferences have been saved successfully.", _controller.TempData["SuccessMessage"]);
        _mockMaterialService.Verify(x => x.SetEquationsEnabledAsync("testuser", false), Times.Once);
    }

    [Fact]
    public async Task UpdatePreferences_WhenServiceThrowsInvalidOperationException_ReturnsErrorMessage()
    {
        // Arrange
        _mockMaterialService
            .Setup(x => x.SetEquationsEnabledAsync("testuser", true))
            .ThrowsAsync(new InvalidOperationException("Failed to save"));

        // Act
        var result = await _controller.UpdatePreferences(true);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(StudyMaterialsController.Manage), redirectResult.ActionName);
        Assert.Equal("Failed to save your preferences. Please try again.", _controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task UpdatePreferences_WhenServiceThrowsGenericException_ReturnsGenericErrorMessage()
    {
        // Arrange
        _mockMaterialService
            .Setup(x => x.SetEquationsEnabledAsync("testuser", true))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.UpdatePreferences(true);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(StudyMaterialsController.Manage), redirectResult.ActionName);
        Assert.Equal("An unexpected error occurred. Please try again.", _controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task UpdatePreferences_PreservesUsername()
    {
        // Arrange
        var capturedUsername = "";
        var capturedEnabled = false;

        _mockMaterialService
            .Setup(x => x.SetEquationsEnabledAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .Callback<string, bool>((username, enabled) =>
            {
                capturedUsername = username;
                capturedEnabled = enabled;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _controller.UpdatePreferences(true);

        // Assert
        Assert.Equal("testuser", capturedUsername);
        Assert.True(capturedEnabled);
    }

    [Fact]
    public async Task Manage_WithMixedMaterials_SetsEquationsEnabledCorrectly()
    {
        // Arrange
        var materials = new List<UserStudyMaterial>
        {
            new UserStudyMaterial
            {
                Username = "testuser",
                MaterialType = StudyMaterialType.TermsAndDefinitions,
                FileName = "terms.md",
                FileSizeBytes = 100,
                UploadedDate = DateTime.UtcNow,
                FilePath = "path/to/terms.md",
                FileHash = "hash1"
            },
            new UserStudyMaterial
            {
                Username = "testuser",
                MaterialType = StudyMaterialType.Equations,
                FileName = "equations.md",
                FileSizeBytes = 200,
                UploadedDate = DateTime.UtcNow,
                FilePath = "path/to/equations.md",
                FileHash = "hash2"
            }
        };

        _mockMaterialService
            .Setup(x => x.GetUserMaterialsAsync("testuser"))
            .ReturnsAsync(materials);

        _mockMaterialService
            .Setup(x => x.GetEquationsEnabledAsync("testuser"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Manage();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ManageStudyMaterialsViewModel>(viewResult.Model);
        Assert.False(model.EquationsEnabled);
        Assert.True(model.HasCustomTerms);
        Assert.True(model.HasCustomEquations);
    }
}
