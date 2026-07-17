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
using System.Security.Claims;
using System.Text;

namespace StudyHelper.Tests.Controllers;

/// <summary>
/// Tests for StudyMaterialsController.UploadTerms and UploadEquations covering:
/// happy path, boundary, negative, and error handling cases.
/// Uses environment = "dev" equivalent: IUserStudyMaterialService is mocked so
/// SaveChangesAsync / file I/O is never called against a real store.
/// </summary>
public class StudyMaterialsController_Upload_Tests
{
    private readonly Mock<IUserStudyMaterialService> _mockService;
    private readonly StudyMaterialsController _controller;

    public StudyMaterialsController_Upload_Tests()
    {
        _mockService = new Mock<IUserStudyMaterialService>();
        var mockEnv = new Mock<IWebHostEnvironment>();

        _controller = new StudyMaterialsController(
            _mockService.Object,
            NullLogger<StudyMaterialsController>.Instance,
            mockEnv.Object);

        var claims = new List<Claim> { new(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        httpContext.Session = new FakeSession();

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper — build a minimal IFormFile stub
    // ─────────────────────────────────────────────────────────────────────────

    private static IFormFile MakeFormFile(string content = "## S\nT: D", string fileName = "file.md")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.Length).Returns(bytes.Length);
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.OpenReadStream()).Returns(stream);
        return mock.Object;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UploadTerms — Happy Path
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadTerms_ValidFileWithCounts_SetsSuccessMessageAndRedirects()
    {
        // Arrange
        var validResult = new FileValidationResult
        {
            IsValid = true,
            ParsedSectionCount = 3,
            ParsedTermCount = 15
        };
        _mockService
            .Setup(s => s.UploadTermsAsync("testuser", It.IsAny<IFormFile>()))
            .ReturnsAsync(validResult);

        // Act
        var result = await _controller.UploadTerms(MakeFormFile());

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Manage", redirect.ActionName);
        Assert.Contains("3", (string)_controller.TempData["SuccessMessage"]!);
        Assert.Contains("15", (string)_controller.TempData["SuccessMessage"]!);
    }

    [Fact]
    public async Task UploadTerms_ValidFileZeroSections_SetsWarningMessage()
    {
        // Arrange
        var warnResult = new FileValidationResult
        {
            IsValid = true,
            ParsedSectionCount = 0,
            ParsedTermCount = 0
        };
        _mockService
            .Setup(s => s.UploadTermsAsync("testuser", It.IsAny<IFormFile>()))
            .ReturnsAsync(warnResult);

        // Act
        await _controller.UploadTerms(MakeFormFile());

        // Assert
        Assert.Null(_controller.TempData["SuccessMessage"]);
        Assert.NotNull(_controller.TempData["WarningMessage"]);
        Assert.Contains("no sections", (string)_controller.TempData["WarningMessage"]!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadTerms_ValidFileZeroTerms_SetsWarningMessage()
    {
        // Arrange
        var warnResult = new FileValidationResult
        {
            IsValid = true,
            ParsedSectionCount = 2,
            ParsedTermCount = 0
        };
        _mockService
            .Setup(s => s.UploadTermsAsync("testuser", It.IsAny<IFormFile>()))
            .ReturnsAsync(warnResult);

        // Act
        await _controller.UploadTerms(MakeFormFile());

        // Assert
        Assert.Null(_controller.TempData["SuccessMessage"]);
        Assert.NotNull(_controller.TempData["WarningMessage"]);
        Assert.Contains("no term-definition pairs", (string)_controller.TempData["WarningMessage"]!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadTerms_InvalidFile_SetsErrorMessageAndRedirects()
    {
        // Arrange
        var invalidResult = new FileValidationResult
        {
            IsValid = false,
            Errors = ["Line 3: Term found before heading.", "Line 7: Missing colon."]
        };
        _mockService
            .Setup(s => s.UploadTermsAsync("testuser", It.IsAny<IFormFile>()))
            .ReturnsAsync(invalidResult);

        // Act
        var result = await _controller.UploadTerms(MakeFormFile());

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Manage", redirect.ActionName);
        Assert.NotNull(_controller.TempData["ErrorMessage"]);
        // BuildErrorHtml wraps errors in a <ul>
        Assert.Contains("<ul>", (string)_controller.TempData["ErrorMessage"]!);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UploadTerms — Negative
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadTerms_NullFile_SetsErrorMessageAndRedirects()
    {
        // Act
        var result = await _controller.UploadTerms(null!);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Manage", redirect.ActionName);
        Assert.NotNull(_controller.TempData["ErrorMessage"]);
        _mockService.Verify(s => s.UploadTermsAsync(It.IsAny<string>(), It.IsAny<IFormFile>()), Times.Never);
    }

    [Fact]
    public async Task UploadTerms_ZeroLengthFile_SetsErrorMessageAndRedirects()
    {
        // Arrange
        var emptyFile = MakeFormFile(content: "");

        // Act
        var result = await _controller.UploadTerms(emptyFile);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Manage", redirect.ActionName);
        Assert.NotNull(_controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task UploadTerms_NoAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange — anonymous controller (no identity name)
        var anonContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()) // unauthenticated
        };
        anonContext.Session = new FakeSession();
        _controller.ControllerContext = new ControllerContext { HttpContext = anonContext };
        _controller.TempData = new TempDataDictionary(anonContext, Mock.Of<ITempDataProvider>());

        // Act
        var result = await _controller.UploadTerms(MakeFormFile());

        // Assert — Finding 4 fix: must return 401 not throw
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UploadTerms_ErrorsExceedTen_TruncationLineIncluded()
    {
        // Arrange — 15 errors; BuildErrorHtml should cap at 10 and append truncation note
        var errors = Enumerable.Range(1, 15)
            .Select(i => $"Line {i}: Some error.")
            .ToList();
        var invalidResult = new FileValidationResult { IsValid = false, Errors = errors };
        _mockService
            .Setup(s => s.UploadTermsAsync("testuser", It.IsAny<IFormFile>()))
            .ReturnsAsync(invalidResult);

        // Act
        await _controller.UploadTerms(MakeFormFile());

        // Assert
        var msg = (string)_controller.TempData["ErrorMessage"]!;
        Assert.Contains("5 more error(s)", msg);  // 15 - 10 = 5
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UploadEquations — Happy Path
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadEquations_ValidFileWithCount_SetsSuccessMessage()
    {
        // Arrange
        var validResult = new FileValidationResult { IsValid = true, ParsedEquationCount = 8 };
        _mockService
            .Setup(s => s.UploadEquationsAsync("testuser", It.IsAny<IFormFile>()))
            .ReturnsAsync(validResult);

        // Act
        var result = await _controller.UploadEquations(MakeFormFile());

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Manage", redirect.ActionName);
        Assert.Contains("8", (string)_controller.TempData["SuccessMessage"]!);
    }

    [Fact]
    public async Task UploadEquations_ValidFileZeroEquations_SetsWarningMessage()
    {
        // Arrange
        var warnResult = new FileValidationResult { IsValid = true, ParsedEquationCount = 0 };
        _mockService
            .Setup(s => s.UploadEquationsAsync("testuser", It.IsAny<IFormFile>()))
            .ReturnsAsync(warnResult);

        // Act
        await _controller.UploadEquations(MakeFormFile());

        // Assert
        Assert.Null(_controller.TempData["SuccessMessage"]);
        Assert.NotNull(_controller.TempData["WarningMessage"]);
        Assert.Contains("no equations were found", (string)_controller.TempData["WarningMessage"]!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadEquations_InvalidFile_SetsErrorMessage()
    {
        // Arrange
        var invalidResult = new FileValidationResult
        {
            IsValid = false,
            Errors = ["Line 5: Missing summary.", "Line 9: Missing equals sign."]
        };
        _mockService
            .Setup(s => s.UploadEquationsAsync("testuser", It.IsAny<IFormFile>()))
            .ReturnsAsync(invalidResult);

        // Act
        await _controller.UploadEquations(MakeFormFile());

        // Assert
        Assert.NotNull(_controller.TempData["ErrorMessage"]);
        Assert.Contains("<ul>", (string)_controller.TempData["ErrorMessage"]!);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UploadEquations — Negative
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadEquations_NullFile_SetsErrorMessageAndRedirects()
    {
        // Act
        var result = await _controller.UploadEquations(null!);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Manage", redirect.ActionName);
        Assert.NotNull(_controller.TempData["ErrorMessage"]);
        _mockService.Verify(s => s.UploadEquationsAsync(It.IsAny<string>(), It.IsAny<IFormFile>()), Times.Never);
    }

    [Fact]
    public async Task UploadEquations_NoAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var anonContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        anonContext.Session = new FakeSession();
        _controller.ControllerContext = new ControllerContext { HttpContext = anonContext };
        _controller.TempData = new TempDataDictionary(anonContext, Mock.Of<ITempDataProvider>());

        // Act
        var result = await _controller.UploadEquations(MakeFormFile());

        // Assert — Finding 4 fix
        Assert.IsType<UnauthorizedResult>(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Delete — Negative (Finding 4 coverage)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_NoAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var anonContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        anonContext.Session = new FakeSession();
        _controller.ControllerContext = new ControllerContext { HttpContext = anonContext };
        _controller.TempData = new TempDataDictionary(anonContext, Mock.Of<ITempDataProvider>());

        // Act
        var result = await _controller.Delete(StudyMaterialType.TermsAndDefinitions);

        // Assert — Finding 4 fix
        Assert.IsType<UnauthorizedResult>(result);
    }
}
