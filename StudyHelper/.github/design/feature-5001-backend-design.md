# Backend Design: User Custom Study Materials Upload (Feature #5001)

**Feature**: User Custom Study Materials Upload  
**Azure DevOps**: https://dev.azure.com/SchneiderDowns/Jeff/_workitems/edit/5001  
**Date**: 2026-05-27  
**Architect**: Solutions Architect  
**Target**: .NET 10, C# 14, ASP.NET Core MVC

---

## 1. Executive Summary

This design enables users to upload custom `TermsAndDefinitions.md` and `Equations.md` files for personalized study content. The system will securely store user-specific files, validate uploads, and seamlessly integrate custom content with existing flashcard, quiz, and exercise generators while maintaining default content fallback from the InputDocuments project.

---

## 2. Architecture Overview

### 2.1 Component Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                       │
│  StudyMaterialsController (Upload, Manage, Delete)          │
└─────────────────────┬───────────────────────────────────────┘
					  │
┌─────────────────────▼───────────────────────────────────────┐
│                     Business Logic Layer                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ IUserStudyMaterialService                           │   │
│  │ - UploadTermsAsync(username, file)                  │   │
│  │ - UploadEquationsAsync(username, file)              │   │
│  │ - GetUserMaterialsAsync(username)                   │   │
│  │ - DeleteUserMaterialAsync(username, type)           │   │
│  │ - GetEffectiveFilePathAsync(username, type)         │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ IFileValidationService                              │   │
│  │ - ValidateMarkdownFileAsync(stream)                 │   │
│  │ - ScanForMaliciousContentAsync(content)             │   │
│  │ - ValidateTermsFormatAsync(content)                 │   │
│  │ - ValidateEquationsFormatAsync(content)             │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────┬───────────────────────────────────────┘
					  │
┌─────────────────────▼───────────────────────────────────────┐
│                     Data Access Layer                        │
│  - User-specific file storage (App_Data/StudyMaterials/)    │
│  - Default files from InputDocuments project                │
│  - File encryption at rest via IEncryptionService           │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Data Flow

**Upload Flow:**
```
User → Controller → Validation → Encryption → Storage → Cache Invalidation
```

**Retrieval Flow:**
```
Parser Service → UserStudyMaterialService → Check Custom File
										  ↓
									  If exists → Return custom path
										  ↓
									  If not → Return default path
```

---

## 3. Data Models

### 3.1 New Models

```csharp
namespace StudyHelper.Models;

/// <summary>
/// Metadata about a user's uploaded study material file.
/// </summary>
public class UserStudyMaterial
{
	public string Username { get; set; } = string.Empty;
	public StudyMaterialType MaterialType { get; set; }
	public string FileName { get; set; } = string.Empty;
	public long FileSizeBytes { get; set; }
	public DateTime UploadedDate { get; set; }
	public string EncryptedFilePath { get; set; } = string.Empty;
	public string FileHash { get; set; } = string.Empty; // SHA256 for integrity
}

/// <summary>
/// Type of study material.
/// </summary>
public enum StudyMaterialType
{
	TermsAndDefinitions,
	Equations
}

/// <summary>
/// Result of file validation.
/// </summary>
public class FileValidationResult
{
	public bool IsValid { get; set; }
	public List<string> Errors { get; set; } = new();
	public List<string> Warnings { get; set; } = new();
}
```

---

## 4. Service Interfaces

### 4.1 IUserStudyMaterialService

```csharp
namespace StudyHelper.Services;

/// <summary>
/// Service for managing user-uploaded study materials.
/// </summary>
public interface IUserStudyMaterialService
{
	/// <summary>
	/// Upload a TermsAndDefinitions.md file for a user.
	/// </summary>
	Task<bool> UploadTermsAsync(string username, IFormFile file);

	/// <summary>
	/// Upload an Equations.md file for a user.
	/// </summary>
	Task<bool> UploadEquationsAsync(string username, IFormFile file);

	/// <summary>
	/// Get metadata for all user-uploaded materials.
	/// </summary>
	Task<List<UserStudyMaterial>> GetUserMaterialsAsync(string username);

	/// <summary>
	/// Delete a user's uploaded study material.
	/// </summary>
	Task<bool> DeleteUserMaterialAsync(string username, StudyMaterialType materialType);

	/// <summary>
	/// Get the effective file path for a material type (custom if exists, otherwise default).
	/// </summary>
	Task<string> GetEffectiveFilePathAsync(string username, StudyMaterialType materialType);

	/// <summary>
	/// Check if user has uploaded a specific material type.
	/// </summary>
	Task<bool> HasCustomMaterialAsync(string username, StudyMaterialType materialType);
}
```

### 4.2 IFileValidationService

```csharp
namespace StudyHelper.Services;

/// <summary>
/// Service for validating uploaded study material files.
/// </summary>
public interface IFileValidationService
{
	/// <summary>
	/// Validate that the file is a proper markdown file.
	/// </summary>
	Task<FileValidationResult> ValidateMarkdownFileAsync(Stream fileStream, string fileName);

	/// <summary>
	/// Scan file content for potentially malicious patterns.
	/// </summary>
	Task<FileValidationResult> ScanForMaliciousContentAsync(string content);

	/// <summary>
	/// Validate that Terms content follows expected markdown structure.
	/// </summary>
	Task<FileValidationResult> ValidateTermsFormatAsync(string content);

	/// <summary>
	/// Validate that Equations content follows expected LaTeX markdown structure.
	/// </summary>
	Task<FileValidationResult> ValidateEquationsFormatAsync(string content);
}
```

---

## 5. Service Implementation Details

### 5.1 UserStudyMaterialService

**Storage Strategy:**
- Location: `App_Data/StudyMaterials/{username}/`
- File naming: `TermsAndDefinitions.md` and `Equations.md`
- Metadata storage: JSON file per user (`App_Data/StudyMaterials/{username}/metadata.json`)
- Encryption: Files encrypted using existing `IEncryptionService`

**Key Methods:**

```csharp
public class UserStudyMaterialService : IUserStudyMaterialService
{
	private readonly IWebHostEnvironment _environment;
	private readonly IEncryptionService _encryptionService;
	private readonly IFileValidationService _validationService;
	private readonly ILogger<UserStudyMaterialService> _logger;
	private readonly IMemoryCache _cache;

	private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
	private const string StudyMaterialsFolder = "StudyMaterials";

	public async Task<bool> UploadTermsAsync(string username, IFormFile file)
	{
		// 1. Validate file size
		if (file.Length > MaxFileSizeBytes)
		{
			_logger.LogWarning("File too large for user {Username}: {Size} bytes", 
				username, file.Length);
			return false;
		}

		// 2. Validate file content
		using var stream = file.OpenReadStream();
		var validationResult = await _validationService.ValidateMarkdownFileAsync(stream, file.FileName);
		if (!validationResult.IsValid)
		{
			_logger.LogWarning("File validation failed for user {Username}: {Errors}", 
				username, string.Join(", ", validationResult.Errors));
			return false;
		}

		// 3. Read content and validate format
		stream.Position = 0;
		using var reader = new StreamReader(stream);
		var content = await reader.ReadToEndAsync();

		var formatResult = await _validationService.ValidateTermsFormatAsync(content);
		if (!formatResult.IsValid)
		{
			_logger.LogWarning("Terms format validation failed for user {Username}: {Errors}", 
				username, string.Join(", ", formatResult.Errors));
			return false;
		}

		var securityResult = await _validationService.ScanForMaliciousContentAsync(content);
		if (!securityResult.IsValid)
		{
			_logger.LogError("Security scan failed for user {Username}: {Errors}", 
				username, string.Join(", ", securityResult.Errors));
			return false;
		}

		// 4. Encrypt and save
		var userFolder = GetUserFolder(username);
		Directory.CreateDirectory(userFolder);

		var filePath = Path.Combine(userFolder, "TermsAndDefinitions.md");
		var encryptedContent = _encryptionService.Encrypt(content);
		await File.WriteAllBytesAsync(filePath, encryptedContent);

		// 5. Update metadata
		await SaveMetadataAsync(username, StudyMaterialType.TermsAndDefinitions, file, content);

		// 6. Invalidate cache for parser services
		_cache.Remove($"TermDefinitions_{username}");

		_logger.LogInformation("User {Username} uploaded TermsAndDefinitions.md", username);
		return true;
	}

	public async Task<string> GetEffectiveFilePathAsync(string username, StudyMaterialType materialType)
	{
		var customPath = GetCustomFilePath(username, materialType);

		if (File.Exists(customPath))
		{
			_logger.LogDebug("Using custom {MaterialType} for user {Username}", 
				materialType, username);
			return customPath;
		}

		// Return default path from InputDocuments
		var defaultPath = GetDefaultFilePath(materialType);
		_logger.LogDebug("Using default {MaterialType} for user {Username}", 
			materialType, username);
		return defaultPath;
	}

	private string GetUserFolder(string username)
	{
		var safeUsername = Path.GetFileNameWithoutExtension(username);
		return Path.Combine(
			_environment.ContentRootPath,
			"App_Data",
			StudyMaterialsFolder,
			safeUsername
		);
	}

	private string GetCustomFilePath(string username, StudyMaterialType materialType)
	{
		var fileName = materialType == StudyMaterialType.TermsAndDefinitions
			? "TermsAndDefinitions.md"
			: "Equations.md";
		return Path.Combine(GetUserFolder(username), fileName);
	}

	private string GetDefaultFilePath(StudyMaterialType materialType)
	{
		var fileName = materialType == StudyMaterialType.TermsAndDefinitions
			? "TermsAndDefinitions.md"
			: "Equations.md";
		return Path.Combine(
			_environment.ContentRootPath,
			"..",
			"InputDocuments",
			"Accumulating",
			fileName
		);
	}
}
```

### 5.2 FileValidationService

```csharp
public class FileValidationService : IFileValidationService
{
	private readonly ILogger<FileValidationService> _logger;

	private static readonly string[] DangerousPatterns = new[]
	{
		"<script", "javascript:", "onerror=", "onclick=", 
		"<iframe", "<object", "<embed", "eval(", "setTimeout("
	};

	public async Task<FileValidationResult> ValidateMarkdownFileAsync(Stream fileStream, string fileName)
	{
		var result = new FileValidationResult { IsValid = true };

		// Validate extension
		if (!fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
		{
			result.IsValid = false;
			result.Errors.Add("File must have .md extension");
		}

		// Validate file is readable
		try
		{
			using var reader = new StreamReader(fileStream, leaveOpen: true);
			var firstLine = await reader.ReadLineAsync();
			if (firstLine == null)
			{
				result.IsValid = false;
				result.Errors.Add("File appears to be empty or unreadable");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to read uploaded file");
			result.IsValid = false;
			result.Errors.Add("File could not be read");
		}

		return result;
	}

	public Task<FileValidationResult> ScanForMaliciousContentAsync(string content)
	{
		var result = new FileValidationResult { IsValid = true };

		foreach (var pattern in DangerousPatterns)
		{
			if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
			{
				result.IsValid = false;
				result.Errors.Add($"Potentially dangerous pattern detected: {pattern}");
				_logger.LogWarning("Malicious content detected: {Pattern}", pattern);
			}
		}

		return Task.FromResult(result);
	}

	public Task<FileValidationResult> ValidateTermsFormatAsync(string content)
	{
		var result = new FileValidationResult { IsValid = true };

		// Look for expected markdown patterns for terms
		// Expected format: **Term** - Definition
		var lines = content.Split('\n');
		var termCount = 0;

		foreach (var line in lines)
		{
			var trimmed = line.Trim();
			if (trimmed.StartsWith("**") && trimmed.Contains("**") && trimmed.Contains("-"))
			{
				termCount++;
			}
		}

		if (termCount == 0)
		{
			result.Warnings.Add("No terms found in expected format (**Term** - Definition)");
		}

		return Task.FromResult(result);
	}

	public Task<FileValidationResult> ValidateEquationsFormatAsync(string content)
	{
		var result = new FileValidationResult { IsValid = true };

		// Look for LaTeX equation markers
		if (!content.Contains("$$") && !content.Contains("\\["))
		{
			result.Warnings.Add("No LaTeX equations found (expected $$ or \\[ markers)");
		}

		return Task.FromResult(result);
	}
}
```

---

## 6. Controller Design

### 6.1 StudyMaterialsController

```csharp
namespace StudyHelper.Controllers;

[Authorize]
public class StudyMaterialsController : Controller
{
	private readonly IUserStudyMaterialService _materialService;
	private readonly ILogger<StudyMaterialsController> _logger;

	public StudyMaterialsController(
		IUserStudyMaterialService materialService,
		ILogger<StudyMaterialsController> logger)
	{
		_materialService = materialService;
		_logger = logger;
	}

	[HttpGet]
	public async Task<IActionResult> Manage()
	{
		var username = User.Identity?.Name ?? throw new UnauthorizedAccessException();
		var materials = await _materialService.GetUserMaterialsAsync(username);

		var viewModel = new ManageStudyMaterialsViewModel
		{
			UserMaterials = materials,
			HasCustomTerms = materials.Any(m => m.MaterialType == StudyMaterialType.TermsAndDefinitions),
			HasCustomEquations = materials.Any(m => m.MaterialType == StudyMaterialType.Equations)
		};

		return View(viewModel);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UploadTerms(IFormFile file)
	{
		var username = User.Identity?.Name ?? throw new UnauthorizedAccessException();

		if (file == null || file.Length == 0)
		{
			TempData["ErrorMessage"] = "Please select a file to upload";
			return RedirectToAction(nameof(Manage));
		}

		var success = await _materialService.UploadTermsAsync(username, file);

		if (success)
		{
			_logger.LogInformation("User {Username} successfully uploaded terms", username);
			TempData["SuccessMessage"] = "Terms and definitions uploaded successfully!";
		}
		else
		{
			_logger.LogWarning("User {Username} failed to upload terms", username);
			TempData["ErrorMessage"] = "Upload failed. Please check the file format and try again.";
		}

		return RedirectToAction(nameof(Manage));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UploadEquations(IFormFile file)
	{
		var username = User.Identity?.Name ?? throw new UnauthorizedAccessException();

		if (file == null || file.Length == 0)
		{
			TempData["ErrorMessage"] = "Please select a file to upload";
			return RedirectToAction(nameof(Manage));
		}

		var success = await _materialService.UploadEquationsAsync(username, file);

		if (success)
		{
			_logger.LogInformation("User {Username} successfully uploaded equations", username);
			TempData["SuccessMessage"] = "Equations uploaded successfully!";
		}
		else
		{
			_logger.LogWarning("User {Username} failed to upload equations", username);
			TempData["ErrorMessage"] = "Upload failed. Please check the file format and try again.";
		}

		return RedirectToAction(nameof(Manage));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(StudyMaterialType materialType)
	{
		var username = User.Identity?.Name ?? throw new UnauthorizedAccessException();

		var success = await _materialService.DeleteUserMaterialAsync(username, materialType);

		if (success)
		{
			_logger.LogInformation("User {Username} deleted {MaterialType}", username, materialType);
			TempData["SuccessMessage"] = $"{materialType} deleted. Using default content.";
		}
		else
		{
			_logger.LogWarning("User {Username} failed to delete {MaterialType}", username, materialType);
			TempData["ErrorMessage"] = "Delete failed.";
		}

		return RedirectToAction(nameof(Manage));
	}
}
```

---

## 7. Parser Service Integration

### 7.1 Modify TermDefinitionParserService

**Current constructor:**
```csharp
public TermDefinitionParserService(
	ILogger<TermDefinitionParserService> logger,
	IConfiguration configuration,
	IWebHostEnvironment environment)
```

**New constructor:**
```csharp
public TermDefinitionParserService(
	ILogger<TermDefinitionParserService> logger,
	IConfiguration configuration,
	IWebHostEnvironment environment,
	IUserStudyMaterialService materialService,
	IHttpContextAccessor httpContextAccessor)
{
	_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	_materialService = materialService ?? throw new ArgumentNullException(nameof(materialService));
	_httpContextAccessor = httpContextAccessor;
	ArgumentNullException.ThrowIfNull(environment);
	_environment = environment;
	_configuration = configuration;
}
```

**Modified ParseTermDefinitionsAsync:**
```csharp
public async Task<List<TermDefinition>> ParseTermDefinitionsAsync()
{
	// Determine effective file path based on user
	var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
	string filePath;

	if (!string.IsNullOrEmpty(username))
	{
		filePath = await _materialService.GetEffectiveFilePathAsync(
			username, StudyMaterialType.TermsAndDefinitions);

		// If file is encrypted (custom), decrypt it first
		if (await _materialService.HasCustomMaterialAsync(username, StudyMaterialType.TermsAndDefinitions))
		{
			var encryptedBytes = await File.ReadAllBytesAsync(filePath);
			var decryptedContent = _encryptionService.Decrypt(encryptedBytes);
			// Parse from decrypted content
			return ParseContent(decryptedContent);
		}
	}
	else
	{
		// Fallback to default if no user context
		filePath = Path.Combine(_environment.ContentRootPath, 
			"../InputDocuments/Accumulating/TermsAndDefinitions.md");
	}

	if (!File.Exists(filePath))
	{
		_logger.LogError("TermsAndDefinitions.md not found at {Path}", filePath);
		throw new FileNotFoundException($"File not found: {filePath}");
	}

	var lines = await File.ReadAllLinesAsync(filePath);
	// ... existing parsing logic
}
```

### 7.2 Similar Modifications for EquationParserService

Apply the same pattern to `EquationParserService` to check for user-uploaded equations before falling back to defaults.

---

## 8. Security Considerations

### 8.1 Authorization
- All operations require `[Authorize]` attribute
- Username extracted from authenticated `User.Identity.Name`
- No cross-user access permitted

### 8.2 File Security
- Files stored in user-specific folders with sanitized usernames
- All custom files encrypted at rest using `IEncryptionService`
- File size limited to 10 MB
- Malicious content scanning before storage

### 8.3 Input Validation
- File extension validation (.md only)
- Content format validation
- XSS/script injection pattern scanning
- Rate limiting (to be implemented via middleware)

### 8.4 Path Traversal Prevention
- Use `Path.GetFileNameWithoutExtension` for username sanitization
- Use `Path.Combine` for all path operations
- Never use user input directly in file paths

---

## 9. Configuration

### 9.1 appsettings.json

```json
{
  "StudyMaterials": {
	"MaxFileSizeBytes": 10485760,
	"StorageFolder": "StudyMaterials",
	"EnableEncryption": true
  }
}
```

---

## 10. Database Schema

**Note**: This feature uses file-based storage with JSON metadata. No SQL database changes required.

**Metadata file structure** (`App_Data/StudyMaterials/{username}/metadata.json`):

```json
{
  "username": "testuser",
  "materials": [
	{
	  "materialType": "TermsAndDefinitions",
	  "fileName": "TermsAndDefinitions.md",
	  "fileSizeBytes": 524288,
	  "uploadedDate": "2026-05-27T18:00:00Z",
	  "encryptedFilePath": "App_Data/StudyMaterials/testuser/TermsAndDefinitions.md",
	  "fileHash": "sha256_hash_here"
	}
  ]
}
```

---

## 11. Error Handling

### 11.1 Upload Failures
- File too large → Return user-friendly error
- Invalid format → Return specific format requirements
- Malicious content → Log security event, reject upload
- Storage failure → Log error, return generic message

### 11.2 Retrieval Failures
- Missing custom file → Silently fall back to default
- Decryption failure → Log error, fall back to default
- Default file missing → Throw exception (application configuration error)

---

## 12. Performance Considerations

### 12.1 Caching
- Cache decrypted custom files in `IMemoryCache`
- Cache key: `{MaterialType}_{Username}`
- Expiration: 1 hour or until file is updated/deleted

### 12.2 File I/O
- Use async file operations throughout
- Stream large files rather than loading fully into memory
- Implement rate limiting on uploads

---

## 13. Testing Requirements

### 13.1 Unit Tests
- `UserStudyMaterialService`: Upload, retrieve, delete operations
- `FileValidationService`: All validation methods
- Path resolution logic
- Encryption/decryption flow

### 13.2 Integration Tests
- Full upload workflow
- Parser service integration with custom files
- Fallback to default files
- Authorization checks

### 13.3 Security Tests
- Path traversal attempts
- Malicious content injection
- Cross-user access attempts
- File size limit enforcement

---

## 14. Deployment Considerations

### 14.1 Migration Strategy
- No database migration required
- Create `App_Data/StudyMaterials` folder on deployment
- Ensure write permissions for application pool identity
- Existing users continue using default files seamlessly

### 14.2 Rollback Plan
- Remove new services from DI container
- Remove StudyMaterialsController
- Revert parser service constructors to original signatures
- Delete user study materials folder if desired

---

## 15. Dependencies

### 15.1 New NuGet Packages
None required - all functionality uses existing .NET libraries.

### 15.2 Modified Services
- `TermDefinitionParserService` - Add user-specific file resolution
- `EquationParserService` - Add user-specific file resolution

### 15.3 New Services
- `IUserStudyMaterialService` / `UserStudyMaterialService`
- `IFileValidationService` / `FileValidationService`

---

## 16. Success Criteria

1. ✅ User can upload custom TermsAndDefinitions.md
2. ✅ User can upload custom Equations.md
3. ✅ Files validated before acceptance
4. ✅ Files encrypted at rest
5. ✅ Custom files used by flashcard/quiz/exercise generators
6. ✅ New users see default content
7. ✅ Users can delete custom files and revert to defaults
8. ✅ No cross-user access possible
9. ✅ All operations logged appropriately
10. ✅ >= 80% test coverage

---

## 17. Open Questions & Risks

### 17.1 Questions for Product Owner
1. Should we support versioning of uploaded files?
2. Should we provide sample template files for download?
3. What is the desired rate limit for uploads? (Propose: 5 uploads per hour per user)

### 17.2 Technical Risks
| Risk | Mitigation |
|------|-----------|
| File encryption performance impact | Use caching aggressively; benchmark decrypt operations |
| Storage growth over time | Implement monitoring; consider cleanup policy for inactive users |
| Complex parser integration | Thorough integration testing; fallback mechanisms |

---

## 18. Implementation Order

1. **Phase 1**: Data models and service interfaces
2. **Phase 2**: FileValidationService implementation
3. **Phase 3**: UserStudyMaterialService implementation
4. **Phase 4**: StudyMaterialsController
5. **Phase 5**: Parser service integration (TermDefinitionParserService)
6. **Phase 6**: Parser service integration (EquationParserService)
7. **Phase 7**: Unit and integration tests
8. **Phase 8**: Frontend implementation (see frontend design document)

---

**Document Status**: ✅ Ready for Human Review  
**Next Step**: Frontend Design Document → Frontend Development Engineer → Code Review
