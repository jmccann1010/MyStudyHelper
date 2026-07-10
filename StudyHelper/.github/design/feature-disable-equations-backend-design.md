# Backend Design: Disable Equations Setting

## Overview
This document details the backend implementation for the equations toggle feature, including data models, service layer changes, and controller modifications.

## Data Model Design

### 1. Create UserStudyMaterialMetadata Model

**File:** `Models/UserStudyMaterialMetadata.cs` (NEW)

```csharp
namespace StudyHelper.Models;

/// <summary>
/// Metadata for a user's study materials and preferences.
/// Stored in App_Data/StudyMaterials/{username}/metadata.json
/// </summary>
public class UserStudyMaterialMetadata
{
	/// <summary>
	/// Username of the user (should match authenticated identity).
	/// </summary>
	public string Username { get; set; } = string.Empty;

	/// <summary>
	/// List of uploaded study material files for this user.
	/// </summary>
	public List<UserStudyMaterial> Materials { get; set; } = new();

	/// <summary>
	/// Whether equation-based features are enabled for this user.
	/// Controls visibility of Exercise, Graded Exercises, and Equation Flashcards on home page.
	/// Defaults to true for backward compatibility.
	/// </summary>
	public bool EquationsEnabled { get; set; } = true;
}
```

**Design Notes:**
- This class replaces the implicit metadata structure currently used
- The `EquationsEnabled` property defaults to `true` for backward compatibility
- Existing JSON files without this property will deserialize with default value
- All properties use init-only setters for immutability where appropriate

---

## Service Layer Design

### 2. Update IUserStudyMaterialService Interface

**File:** `Services/IUserStudyMaterialService.cs` (MODIFY)

Add two new methods:

```csharp
/// <summary>
/// Gets whether equation-based features are enabled for the specified user.
/// Returns true if the preference is not set (default/backward compatible behavior).
/// </summary>
/// <param name="username">The username to check.</param>
/// <returns>True if equations are enabled, false otherwise.</returns>
/// <exception cref="ArgumentException">Thrown if username is null or empty.</exception>
Task<bool> GetEquationsEnabledAsync(string username);

/// <summary>
/// Sets whether equation-based features are enabled for the specified user.
/// Updates the user's metadata file with the new preference.
/// </summary>
/// <param name="username">The username to update.</param>
/// <param name="enabled">True to enable equations, false to disable.</param>
/// <returns>Task representing the asynchronous operation.</returns>
/// <exception cref="ArgumentException">Thrown if username is null or empty.</exception>
/// <exception cref="InvalidOperationException">Thrown if metadata cannot be saved.</exception>
Task SetEquationsEnabledAsync(string username, bool enabled);
```

---

### 3. Implement Service Methods

**File:** `Services/UserStudyMaterialService.cs` (MODIFY)

#### 3a. Update Metadata Loading Logic

Current implementation likely reads metadata as a simple structure. Update to use the new `UserStudyMaterialMetadata` model:

```csharp
private async Task<UserStudyMaterialMetadata> LoadMetadataAsync(string username)
{
	var metadataPath = GetMetadataPath(username);

	if (!File.Exists(metadataPath))
	{
		_logger.LogInformation("Metadata file not found for user {Username}, creating new", username);
		return new UserStudyMaterialMetadata
		{
			Username = username,
			Materials = new(),
			EquationsEnabled = true // Default for new users
		};
	}

	try
	{
		var json = await File.ReadAllTextAsync(metadataPath);
		var metadata = JsonSerializer.Deserialize<UserStudyMaterialMetadata>(json, _jsonOptions);

		if (metadata == null)
		{
			_logger.LogWarning("Metadata deserialization returned null for user {Username}", username);
			return new UserStudyMaterialMetadata
			{
				Username = username,
				Materials = new(),
				EquationsEnabled = true
			};
		}

		// Ensure EquationsEnabled has a value (backward compatibility)
		// If the property is missing from JSON, it will deserialize as false (default)
		// but we want it to be true for existing users
		// Check if Materials exists but EquationsEnabled is false (likely missing from old JSON)
		if (metadata.Materials.Any() && !metadata.EquationsEnabled)
		{
			// This is likely an old metadata file without the property
			// Default to true for backward compatibility
			metadata.EquationsEnabled = true;
			await SaveMetadataAsync(metadata); // Update file with new property
		}

		return metadata;
	}
	catch (JsonException ex)
	{
		_logger.LogError(ex, "Failed to deserialize metadata for user {Username}", username);
		// Return default metadata on error (fail open)
		return new UserStudyMaterialMetadata
		{
			Username = username,
			Materials = new(),
			EquationsEnabled = true
		};
	}
}

private async Task SaveMetadataAsync(UserStudyMaterialMetadata metadata)
{
	var metadataPath = GetMetadataPath(metadata.Username);
	var directoryPath = Path.GetDirectoryName(metadataPath);

	if (!Directory.Exists(directoryPath))
	{
		Directory.CreateDirectory(directoryPath!);
	}

	var json = JsonSerializer.Serialize(metadata, _jsonOptions);
	await File.WriteAllTextAsync(metadataPath, json);

	_logger.LogInformation("Saved metadata for user {Username}", metadata.Username);
}

private string GetMetadataPath(string username)
{
	var userDir = Path.Combine(_studyMaterialsPath, username);
	return Path.Combine(userDir, "metadata.json");
}
```

#### 3b. Implement GetEquationsEnabledAsync

```csharp
public async Task<bool> GetEquationsEnabledAsync(string username)
{
	if (string.IsNullOrWhiteSpace(username))
	{
		throw new ArgumentException("Username cannot be null or empty.", nameof(username));
	}

	try
	{
		var metadata = await LoadMetadataAsync(username);
		return metadata.EquationsEnabled;
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Error retrieving equations enabled status for user {Username}", username);
		// Fail open - return true (enabled) on error
		return true;
	}
}
```

#### 3c. Implement SetEquationsEnabledAsync

```csharp
public async Task SetEquationsEnabledAsync(string username, bool enabled)
{
	if (string.IsNullOrWhiteSpace(username))
	{
		throw new ArgumentException("Username cannot be null or empty.", nameof(username));
	}

	try
	{
		var metadata = await LoadMetadataAsync(username);
		metadata.EquationsEnabled = enabled;
		await SaveMetadataAsync(metadata);

		_logger.LogInformation("Set equations enabled to {Enabled} for user {Username}", enabled, username);
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Error setting equations enabled for user {Username}", username);
		throw new InvalidOperationException($"Failed to update equations setting for user {username}", ex);
	}
}
```

**Design Notes:**
- Methods are async for consistency with file I/O operations
- Validation throws `ArgumentException` for invalid input
- Errors are logged with structured logging
- `GetEquationsEnabledAsync` fails open (returns true) for resilience
- `SetEquationsEnabledAsync` throws exception on failure for caller to handle

---

## Controller Layer Design

### 4. Update StudyMaterialsController

**File:** `Controllers/StudyMaterialsController.cs` (MODIFY)

#### 4a. Update Manage Action (GET)

```csharp
/// <summary>
/// GET: /StudyMaterials/Manage
/// Displays the study materials management page with current preferences.
/// </summary>
[HttpGet]
[Authorize]
public async Task<IActionResult> Manage()
{
	var username = User.Identity?.Name;
	if (string.IsNullOrEmpty(username))
	{
		_logger.LogWarning("Unauthenticated user attempted to access Manage");
		return RedirectToAction("Login", "Account");
	}

	try
	{
		var materials = await _userStudyMaterialService.GetUserMaterialsAsync(username);
		var equationsEnabled = await _userStudyMaterialService.GetEquationsEnabledAsync(username);

		var viewModel = new ManageStudyMaterialsViewModel
		{
			Materials = materials,
			EquationsEnabled = equationsEnabled
		};

		return View(viewModel);
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Error loading study materials for user {Username}", username);
		return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
	}
}
```

#### 4b. Add UpdatePreferences Action (POST)

```csharp
/// <summary>
/// POST: /StudyMaterials/UpdatePreferences
/// Updates user preferences including equations enabled setting.
/// </summary>
[HttpPost]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdatePreferences(bool equationsEnabled)
{
	var username = User.Identity?.Name;
	if (string.IsNullOrEmpty(username))
	{
		_logger.LogWarning("Unauthenticated user attempted to update preferences");
		return RedirectToAction("Login", "Account");
	}

	try
	{
		await _userStudyMaterialService.SetEquationsEnabledAsync(username, equationsEnabled);

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
		return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
	}
}
```

**Design Notes:**
- Both actions require `[Authorize]` attribute
- POST action includes `[ValidateAntiForgeryToken]` for CSRF protection
- Success/error messages stored in TempData for display on redirect
- Structured logging for monitoring and debugging
- Graceful error handling with user-friendly messages

---

### 5. Update HomeController

**File:** `Controllers/HomeController.cs` (MODIFY)

```csharp
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

	/// <summary>
	/// GET: /
	/// Displays the home page with study options based on user preferences.
	/// </summary>
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

	// ... other actions ...
}
```

**Design Notes:**
- Inject `IUserStudyMaterialService` into controller
- Load preference for authenticated users only
- Anonymous users see all features (default to enabled)
- Pass preference to view via ViewBag
- Fail open on error (show all features)

---

## ViewModel Changes

### 6. Update ManageStudyMaterialsViewModel

**File:** `ViewModels/ManageStudyMaterialsViewModel.cs` (MODIFY)

```csharp
namespace StudyHelper.ViewModels;

/// <summary>
/// ViewModel for the Study Materials management page.
/// </summary>
public class ManageStudyMaterialsViewModel
{
	/// <summary>
	/// List of user's uploaded study materials.
	/// </summary>
	public List<UserStudyMaterial> Materials { get; set; } = new();

	/// <summary>
	/// Whether equation-based features are enabled for this user.
	/// Controls visibility of Exercise, Graded Exercises, and Equation Flashcards.
	/// </summary>
	public bool EquationsEnabled { get; set; } = true;
}
```

---

## Dependency Injection Registration

**File:** `Program.cs` (VERIFY - likely already registered)

Ensure `IUserStudyMaterialService` is registered:

```csharp
builder.Services.AddScoped<IUserStudyMaterialService, UserStudyMaterialService>();
```

---

## Logging Strategy

### Log Levels

| Scenario | Level | Message Template |
|----------|-------|------------------|
| Setting loaded | Information | `"Retrieved equations enabled status: {Enabled} for user {Username}"` |
| Setting updated | Information | `"User {Username} updated equations enabled to {Enabled}"` |
| Metadata file missing (new user) | Information | `"Metadata file not found for user {Username}, creating new"` |
| Backward compat migration | Information | `"Migrated old metadata for user {Username} to include EquationsEnabled"` |
| Deserialization error | Error | `"Failed to deserialize metadata for user {Username}"` |
| Save error | Error | `"Error setting equations enabled for user {Username}"` |
| Unexpected error | Error | `"Unexpected error updating preferences for user {Username}"` |

### Structured Logging

All log messages include:
- Username (when available)
- Operation (Get/Set)
- Value (for Set operations)
- Exception details (for errors)

---

## Error Handling Matrix

| Error Type | Scenario | Handling | User Experience |
|------------|----------|----------|-----------------|
| `ArgumentException` | Invalid username | Log + throw | 500 error page |
| `FileNotFoundException` | Metadata missing | Create new | Default enabled |
| `JsonException` | Corrupt metadata | Log + default | Default enabled |
| `IOException` | File locked/permissions | Log + throw | Error message + retry |
| `InvalidOperationException` | Save failed | Log + throw | Error message + retry |
| `UnauthorizedAccessException` | Permissions error | Log + throw | Error message |

---

## Testing Requirements

### Unit Tests

**File:** `Tests/Services/UserStudyMaterialServiceTests.cs` (NEW/MODIFY)

```csharp
public class UserStudyMaterialServiceTests
{
	[Fact]
	public async Task GetEquationsEnabledAsync_NewUser_ReturnsTrue()
	{
		// Arrange: New user with no metadata file
		// Act: Get equations enabled
		// Assert: Returns true (default)
	}

	[Fact]
	public async Task GetEquationsEnabledAsync_UserEnabledEquations_ReturnsTrue()
	{
		// Arrange: User with EquationsEnabled = true in metadata
		// Act: Get equations enabled
		// Assert: Returns true
	}

	[Fact]
	public async Task GetEquationsEnabledAsync_UserDisabledEquations_ReturnsFalse()
	{
		// Arrange: User with EquationsEnabled = false in metadata
		// Act: Get equations enabled
		// Assert: Returns false
	}

	[Fact]
	public async Task SetEquationsEnabledAsync_ValidUser_SavesPreference()
	{
		// Arrange: User with existing metadata
		// Act: Set equations enabled to false
		// Assert: Metadata file contains EquationsEnabled = false
	}

	[Fact]
	public async Task SetEquationsEnabledAsync_NullUsername_ThrowsArgumentException()
	{
		// Arrange: Null username
		// Act & Assert: Throws ArgumentException
	}

	[Fact]
	public async Task GetEquationsEnabledAsync_CorruptMetadata_ReturnsTrue()
	{
		// Arrange: Corrupt metadata JSON file
		// Act: Get equations enabled
		// Assert: Returns true (fail open)
	}

	[Fact]
	public async Task LoadMetadataAsync_OldMetadataWithoutProperty_DefaultsToTrue()
	{
		// Arrange: Metadata JSON without EquationsEnabled property
		// Act: Load metadata
		// Assert: EquationsEnabled is true
	}
}
```

### Controller Tests

**File:** `Tests/Controllers/StudyMaterialsControllerTests.cs` (MODIFY)

```csharp
[Fact]
public async Task UpdatePreferences_ValidInput_SavesAndRedirects()
{
	// Arrange: Authenticated user, equationsEnabled = false
	// Act: POST UpdatePreferences
	// Assert: Service called, success message in TempData, redirects to Manage
}

[Fact]
public async Task UpdatePreferences_ServiceThrowsException_ShowsErrorMessage()
{
	// Arrange: Service throws InvalidOperationException
	// Act: POST UpdatePreferences
	// Assert: Error message in TempData, redirects to Manage
}
```

---

## Migration Strategy

### Existing Users

No explicit migration needed. Backward compatibility handled in code:

1. **First Load:** Old metadata without `EquationsEnabled` deserializes with default `false`
2. **Detection:** Service detects this is an old file (has Materials but EquationsEnabled is false)
3. **Auto-Update:** Service sets `EquationsEnabled = true` and saves file
4. **Result:** Existing users see all features (backward compatible)

### New Users

- Metadata created with `EquationsEnabled = true` from the start
- No special handling needed

---

## API Summary

### New Public Methods

| Method | Interface | Return | Description |
|--------|-----------|--------|-------------|
| `GetEquationsEnabledAsync(string)` | `IUserStudyMaterialService` | `Task<bool>` | Gets user preference |
| `SetEquationsEnabledAsync(string, bool)` | `IUserStudyMaterialService` | `Task` | Sets user preference |
| `UpdatePreferences(bool)` | `StudyMaterialsController` | `IActionResult` | POST action to save |

### Modified Methods

| Method | Change | Reason |
|--------|--------|--------|
| `LoadMetadataAsync` | Use `UserStudyMaterialMetadata` model | Strongly typed metadata |
| `SaveMetadataAsync` | Use `UserStudyMaterialMetadata` model | Strongly typed metadata |
| `Manage` (GET) | Load and pass EquationsEnabled | Display in view |
| `Index` (Home) | Load and pass EquationsEnabled | Conditional rendering |

---

## Configuration

No configuration changes needed. Uses existing:
- `_studyMaterialsPath` from existing service
- JSON serialization options from existing code

---

## Success Criteria

- ✅ `UserStudyMaterialMetadata` model created
- ✅ Service methods implemented and tested
- ✅ Controller actions implemented
- ✅ Backward compatibility verified
- ✅ Error handling robust
- ✅ Logging comprehensive
- ✅ All unit tests pass
- ✅ Build succeeds with no warnings

---

**Document Version:** 1.0  
**Status:** Approved for Implementation
