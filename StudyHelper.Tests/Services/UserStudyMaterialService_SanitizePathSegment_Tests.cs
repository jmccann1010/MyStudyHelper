using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudyHelper.Services;

namespace StudyHelper.Tests.Services;

/// <summary>
/// Tests for UserStudyMaterialService path-safety helpers (SanitizePathSegment
/// and AssertUnderRoot) that were added to fix Finding 1 (path traversal).
///
/// The helpers are private; they are exercised via reflection so we can verify
/// boundary and security behaviour without making them public.
/// </summary>
public class UserStudyMaterialService_SanitizePathSegment_Tests
{
    private readonly UserStudyMaterialService _service;

    public UserStudyMaterialService_SanitizePathSegment_Tests()
    {
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns("C:\\App");

        var mockCache = new Mock<IMemoryCache>();
        // IMemoryCache.CreateEntry is called internally; return a minimal stub
        mockCache
            .Setup(c => c.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        var config = new ConfigurationBuilder().Build();

        var mockValidation = new Mock<IFileValidationService>();

        _service = new UserStudyMaterialService(
            mockEnv.Object,
            mockValidation.Object,
            NullLogger<UserStudyMaterialService>.Instance,
            mockCache.Object,
            config);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers — invoke private static SanitizePathSegment via reflection
    // ─────────────────────────────────────────────────────────────────────────

    private static string InvokeSanitize(string input)
    {
        var method = typeof(UserStudyMaterialService)
            .GetMethod("SanitizePathSegment", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException("SanitizePathSegment not found");

        try
        {
            return (string)method.Invoke(null, [input])!;
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            throw tie.InnerException;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Happy Path
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SanitizePathSegment_SimpleUsername_ReturnsSameValue()
    {
        // Act
        var result = InvokeSanitize("alice");

        // Assert
        Assert.Equal("alice", result);
    }

    [Fact]
    public void SanitizePathSegment_AlphanumericWithHyphen_ReturnsSameValue()
    {
        // Act
        var result = InvokeSanitize("my-course-2024");

        // Assert
        Assert.Equal("my-course-2024", result);
    }

    [Fact]
    public void SanitizePathSegment_LeadingAndTrailingWhitespace_IsTrimmed()
    {
        // Act
        var result = InvokeSanitize("  username  ");

        // Assert
        Assert.Equal("username", result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Boundary
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SanitizePathSegment_SingleCharacter_IsAccepted()
    {
        // Act
        var result = InvokeSanitize("a");

        // Assert
        Assert.Equal("a", result);
    }

    [Fact]
    public void SanitizePathSegment_LeadingDotRemoved_ProducesValidSegment()
    {
        // Arrange — ".hidden" → leading dot stripped → "hidden"
        var result = InvokeSanitize(".hidden");

        // Assert
        Assert.Equal("hidden", result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Security — path traversal inputs must be neutralised
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SanitizePathSegment_ForwardSlash_IsRemoved()
    {
        // Act
        var result = InvokeSanitize("a/b");

        // Assert — slashes stripped
        Assert.Equal("ab", result);
    }

    [Fact]
    public void SanitizePathSegment_BackSlash_IsRemoved()
    {
        // Act
        var result = InvokeSanitize("a\\b");

        // Assert
        Assert.Equal("ab", result);
    }

    [Fact]
    public void SanitizePathSegment_DotDot_BecomesEmpty_ThrowsArgumentException()
    {
        // Arrange — ".." after stripping leading dots becomes ""
        // Act & Assert
        Assert.Throws<ArgumentException>(() => InvokeSanitize(".."));
    }

    [Fact]
    public void SanitizePathSegment_DotDotWithSlash_TraversalNeutralised()
    {
        // Arrange — "../../etc":
        //   slashes stripped  → "..etc"
        //   TrimStart('.')    → "etc"
        // The traversal attempt is completely neutralised; no throw because
        // a non-empty safe segment remains. AssertUnderRoot is the second gate.
        var result = InvokeSanitize("../../etc");

        // Assert — only the non-dot, non-slash portion survives
        Assert.Equal("etc", result);
    }

    [Fact]
    public void SanitizePathSegment_WindowsDriveLetter_ColonIsRemoved()
    {
        // Arrange — "C:" → colon removed → "C"
        var result = InvokeSanitize("C:");

        // Assert
        Assert.Equal("C", result);
    }

    [Fact]
    public void SanitizePathSegment_NullByteInjection_IsRemoved()
    {
        // Arrange — null byte is stripped
        var result = InvokeSanitize("user\0name");

        // Assert
        Assert.Equal("username", result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Negative
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SanitizePathSegment_EmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => InvokeSanitize(""));
    }

    [Fact]
    public void SanitizePathSegment_WhitespaceOnly_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => InvokeSanitize("   "));
    }

    [Fact]
    public void SanitizePathSegment_OnlySlashes_ThrowsArgumentException()
    {
        // Arrange — "///" → all slashes removed → empty
        Assert.Throws<ArgumentException>(() => InvokeSanitize("///"));
    }
}
