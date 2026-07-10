using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudyHelper.Models;
using StudyHelper.Services;
using System.Text.Json;

namespace StudyHelper.Tests.Services;

public class UserStudyMaterialServiceEquationsTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly UserStudyMaterialService _service;
    private readonly Mock<IFileValidationService> _mockValidationService;
    private readonly IMemoryCache _cache;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public UserStudyMaterialServiceEquationsTests()
    {
        // Create unique test directory for each test run
        _testDirectory = Path.Combine(Path.GetTempPath(), "StudyHelperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Setup mocks
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(x => x.ContentRootPath).Returns(_testDirectory);

        _mockValidationService = new Mock<IFileValidationService>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration
            .Setup(x => x.GetSection("StudyMaterials:MaxFileSizeBytes").Value)
            .Returns("5242880");
        _mockConfiguration
            .Setup(x => x.GetSection("StudyMaterials:StorageFolder").Value)
            .Returns("StudyMaterials");

        _service = new UserStudyMaterialService(
            _mockEnvironment.Object,
            _mockValidationService.Object,
            NullLogger<UserStudyMaterialService>.Instance,
            _cache,
            _mockConfiguration.Object
        );
    }

    [Fact]
    public async Task GetEquationsEnabledAsync_WhenNewUserNoMetadata_ReturnsTrue()
    {
        // Arrange
        var username = "newuser";

        // Act
        var result = await _service.GetEquationsEnabledAsync(username);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetEquationsEnabledAsync_WhenUserHasEnabledTrue_ReturnsTrue()
    {
        // Arrange
        var username = "testuser";
        await CreateMetadataFile(username, equationsEnabled: true);

        // Act
        var result = await _service.GetEquationsEnabledAsync(username);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetEquationsEnabledAsync_WhenUserHasEnabledFalse_ReturnsFalse()
    {
        // Arrange
        var username = "testuser";
        await CreateMetadataFile(username, equationsEnabled: false);

        // Act
        var result = await _service.GetEquationsEnabledAsync(username);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetEquationsEnabledAsync_WhenUsernameIsNull_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetEquationsEnabledAsync(null!));
    }

    [Fact]
    public async Task GetEquationsEnabledAsync_WhenUsernameIsEmpty_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetEquationsEnabledAsync(string.Empty));
    }

    [Fact]
    public async Task GetEquationsEnabledAsync_WhenUsernameIsWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetEquationsEnabledAsync("   "));
    }

    [Fact]
    public async Task GetEquationsEnabledAsync_WhenOldMetadataWithoutProperty_ReturnsTrueAndMigrates()
    {
        // Arrange
        var username = "olduser";
        await CreateOldMetadataFileWithoutEquationsEnabled(username);

        // Act
        var result = await _service.GetEquationsEnabledAsync(username);

        // Assert
        Assert.True(result);

        // Verify migration occurred
        var metadataPath = GetMetadataPath(username);
        var json = await File.ReadAllTextAsync(metadataPath);
        var metadata = JsonSerializer.Deserialize<UserStudyMaterialMetadata>(json);
        Assert.NotNull(metadata);
        Assert.True(metadata.EquationsEnabled);
    }

    [Fact]
    public async Task GetEquationsEnabledAsync_WhenCorruptMetadata_ReturnsTrueFailOpen()
    {
        // Arrange
        var username = "corruptuser";
        var metadataPath = GetMetadataPath(username);
        var metadataFolder = Path.GetDirectoryName(metadataPath);
        Directory.CreateDirectory(metadataFolder!);
        await File.WriteAllTextAsync(metadataPath, "{ corrupt json content }");

        // Act
        var result = await _service.GetEquationsEnabledAsync(username);

        // Assert
        Assert.True(result); // Fail open
    }

    [Fact]
    public async Task SetEquationsEnabledAsync_WhenNewUser_CreatesMetadataWithSetting()
    {
        // Arrange
        var username = "newuser";

        // Act
        await _service.SetEquationsEnabledAsync(username, false);

        // Assert
        var metadataPath = GetMetadataPath(username);
        Assert.True(File.Exists(metadataPath));

        var json = await File.ReadAllTextAsync(metadataPath);
        var metadata = JsonSerializer.Deserialize<UserStudyMaterialMetadata>(json);
        Assert.NotNull(metadata);
        Assert.Equal(username, metadata.Username);
        Assert.False(metadata.EquationsEnabled);
    }

    [Fact]
    public async Task SetEquationsEnabledAsync_WhenExistingUser_UpdatesSetting()
    {
        // Arrange
        var username = "existinguser";
        await CreateMetadataFile(username, equationsEnabled: true);

        // Act
        await _service.SetEquationsEnabledAsync(username, false);

        // Assert
        var result = await _service.GetEquationsEnabledAsync(username);
        Assert.False(result);
    }

    [Fact]
    public async Task SetEquationsEnabledAsync_WhenUsernameIsNull_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SetEquationsEnabledAsync(null!, true));
    }

    [Fact]
    public async Task SetEquationsEnabledAsync_WhenUsernameIsEmpty_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SetEquationsEnabledAsync(string.Empty, true));
    }

    [Fact]
    public async Task SetEquationsEnabledAsync_PreservesExistingMaterials()
    {
        // Arrange
        var username = "userWithMaterials";
        var materials = new List<UserStudyMaterial>
        {
            new UserStudyMaterial
            {
                Username = username,
                MaterialType = StudyMaterialType.TermsAndDefinitions,
                FileName = "terms.md",
                FileSizeBytes = 1024,
                UploadedDate = DateTime.UtcNow,
                FilePath = "test/path",
                FileHash = "hash123"
            }
        };
        await CreateMetadataFile(username, equationsEnabled: true, materials: materials);

        // Act
        await _service.SetEquationsEnabledAsync(username, false);

        // Assert
        var userMaterials = await _service.GetUserMaterialsAsync(username);
        Assert.Single(userMaterials);
        Assert.Equal("terms.md", userMaterials[0].FileName);
    }

    [Fact]
    public async Task SetEquationsEnabledAsync_EnabledTrue_PersistsCorrectly()
    {
        // Arrange
        var username = "testuser";
        await CreateMetadataFile(username, equationsEnabled: false);

        // Act
        await _service.SetEquationsEnabledAsync(username, true);

        // Assert
        var result = await _service.GetEquationsEnabledAsync(username);
        Assert.True(result);
    }

    [Fact]
    public async Task SetEquationsEnabledAsync_EnabledFalse_PersistsCorrectly()
    {
        // Arrange
        var username = "testuser";
        await CreateMetadataFile(username, equationsEnabled: true);

        // Act
        await _service.SetEquationsEnabledAsync(username, false);

        // Assert
        var result = await _service.GetEquationsEnabledAsync(username);
        Assert.False(result);
    }

    [Fact]
    public async Task SetEquationsEnabledAsync_MultipleUpdates_PersistsLatestValue()
    {
        // Arrange
        var username = "testuser";

        // Act & Assert
        await _service.SetEquationsEnabledAsync(username, true);
        Assert.True(await _service.GetEquationsEnabledAsync(username));

        await _service.SetEquationsEnabledAsync(username, false);
        Assert.False(await _service.GetEquationsEnabledAsync(username));

        await _service.SetEquationsEnabledAsync(username, true);
        Assert.True(await _service.GetEquationsEnabledAsync(username));
    }

    // Helper methods

    private string GetMetadataPath(string username)
    {
        var safeUsername = Path.GetFileNameWithoutExtension(username);
        var userFolder = Path.Combine(_testDirectory, "App_Data", "StudyMaterials", safeUsername);
        return Path.Combine(userFolder, "metadata.json");
    }

    private async Task CreateMetadataFile(string username, bool equationsEnabled, List<UserStudyMaterial>? materials = null)
    {
        var metadata = new UserStudyMaterialMetadata
        {
            Username = username,
            Materials = materials ?? new List<UserStudyMaterial>(),
            EquationsEnabled = equationsEnabled
        };

        var metadataPath = GetMetadataPath(username);
        var metadataFolder = Path.GetDirectoryName(metadataPath);
        Directory.CreateDirectory(metadataFolder!);

        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(metadataPath, json);
    }

    private async Task CreateOldMetadataFileWithoutEquationsEnabled(string username)
    {
        // Create old-style metadata without EquationsEnabled property
        var oldMetadata = new
        {
            Username = username,
            Materials = new[]
            {
                new
                {
                    Username = username,
                    MaterialType = 0, // TermsAndDefinitions
                    FileName = "old_terms.md",
                    FileSizeBytes = 512L,
                    UploadedDate = DateTime.UtcNow,
                    FilePath = "old/path",
                    FileHash = "oldhash"
                }
            }
        };

        var metadataPath = GetMetadataPath(username);
        var metadataFolder = Path.GetDirectoryName(metadataPath);
        Directory.CreateDirectory(metadataFolder!);

        var json = JsonSerializer.Serialize(oldMetadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(metadataPath, json);
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
        _cache?.Dispose();
    }
}
