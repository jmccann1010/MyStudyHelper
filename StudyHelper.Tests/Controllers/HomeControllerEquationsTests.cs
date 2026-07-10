using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudyHelper.Controllers;
using StudyHelper.Services;
using System.Security.Claims;

namespace StudyHelper.Tests.Controllers;

public class HomeControllerEquationsTests
{
    private readonly Mock<IUserStudyMaterialService> _mockMaterialService;
    private readonly HomeController _controller;

    public HomeControllerEquationsTests()
    {
        _mockMaterialService = new Mock<IUserStudyMaterialService>();

        _controller = new HomeController(
            NullLogger<HomeController>.Instance,
            _mockMaterialService.Object
        );

        // Setup default controller context
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Setup TempData
        _controller.TempData = new TempDataDictionary(
            _controller.HttpContext,
            Mock.Of<ITempDataProvider>()
        );
    }

    [Fact]
    public async Task Index_WhenUserAuthenticatedAndEquationsEnabled_SetsViewBagTrue()
    {
        // Arrange
        SetupAuthenticatedUser("testuser");
        _mockMaterialService
            .Setup(x => x.GetEquationsEnabledAsync("testuser"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.True((bool)_controller.ViewBag.EquationsEnabled);
    }

    [Fact]
    public async Task Index_WhenUserAuthenticatedAndEquationsDisabled_SetsViewBagFalse()
    {
        // Arrange
        SetupAuthenticatedUser("testuser");
        _mockMaterialService
            .Setup(x => x.GetEquationsEnabledAsync("testuser"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False((bool)_controller.ViewBag.EquationsEnabled);
    }

    [Fact]
    public async Task Index_WhenUserNotAuthenticated_SetsViewBagTrueByDefault()
    {
        // Arrange
        SetupAnonymousUser();

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.True((bool)_controller.ViewBag.EquationsEnabled);
        _mockMaterialService.Verify(x => x.GetEquationsEnabledAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Index_WhenServiceThrowsException_FailsOpenToEnabled()
    {
        // Arrange
        SetupAuthenticatedUser("testuser");
        _mockMaterialService
            .Setup(x => x.GetEquationsEnabledAsync("testuser"))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.True((bool)_controller.ViewBag.EquationsEnabled);
    }

    [Fact]
    public async Task Index_WhenServiceThrowsInvalidOperationException_FailsOpenToEnabled()
    {
        // Arrange
        SetupAuthenticatedUser("testuser");
        _mockMaterialService
            .Setup(x => x.GetEquationsEnabledAsync("testuser"))
            .ThrowsAsync(new InvalidOperationException("Cannot read metadata"));

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.True((bool)_controller.ViewBag.EquationsEnabled);
    }

    [Fact]
    public async Task Index_WhenUsernameIsNull_DefaultsToEnabled()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // No name claim
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = claimsPrincipal;

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.True((bool)_controller.ViewBag.EquationsEnabled);
        _mockMaterialService.Verify(x => x.GetEquationsEnabledAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Index_WhenUsernameIsEmpty_DefaultsToEnabled()
    {
        // Arrange
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = claimsPrincipal;

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.True((bool)_controller.ViewBag.EquationsEnabled);
        _mockMaterialService.Verify(x => x.GetEquationsEnabledAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Index_CallsServiceWithCorrectUsername()
    {
        // Arrange
        SetupAuthenticatedUser("specificuser");
        _mockMaterialService
            .Setup(x => x.GetEquationsEnabledAsync("specificuser"))
            .ReturnsAsync(false);

        // Act
        await _controller.Index();

        // Assert
        _mockMaterialService.Verify(x => x.GetEquationsEnabledAsync("specificuser"), Times.Once);
    }

    [Fact]
    public async Task Index_MultipleUsers_HandlesCorrectly()
    {
        // Arrange - First user
        SetupAuthenticatedUser("user1");
        _mockMaterialService
            .Setup(x => x.GetEquationsEnabledAsync("user1"))
            .ReturnsAsync(true);

        // Act - First user
        var result1 = await _controller.Index();

        // Assert - First user
        Assert.True((bool)_controller.ViewBag.EquationsEnabled);

        // Arrange - Second user (new controller instance to simulate different request)
        var controller2 = new HomeController(
            NullLogger<HomeController>.Instance,
            _mockMaterialService.Object
        );
        var claims2 = new List<Claim> { new Claim(ClaimTypes.Name, "user2") };
        var identity2 = new ClaimsIdentity(claims2, "TestAuth");
        var claimsPrincipal2 = new ClaimsPrincipal(identity2);
        controller2.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal2 }
        };
        controller2.TempData = new TempDataDictionary(
            controller2.HttpContext,
            Mock.Of<ITempDataProvider>()
        );

        _mockMaterialService
            .Setup(x => x.GetEquationsEnabledAsync("user2"))
            .ReturnsAsync(false);

        // Act - Second user
        var result2 = await controller2.Index();

        // Assert - Second user
        Assert.False((bool)controller2.ViewBag.EquationsEnabled);
    }

    [Fact]
    public async Task Index_ReturnsViewResult()
    {
        // Arrange
        SetupAuthenticatedUser("testuser");
        _mockMaterialService
            .Setup(x => x.GetEquationsEnabledAsync("testuser"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Index();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    // Helper methods

    private void SetupAuthenticatedUser(string username)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = claimsPrincipal;
    }

    private void SetupAnonymousUser()
    {
        var identity = new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = claimsPrincipal;
    }
}
