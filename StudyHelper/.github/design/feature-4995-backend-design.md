# Feature #4995: Login Authentication - Backend Design

## Architecture Overview

### Backend Components
```
Controllers/
  AccountController.cs        - Authentication endpoints
Models/
  User.cs                     - User entity
ViewModels/
  LoginViewModel.cs           - Login form binding
  RegisterViewModel.cs        - Registration form binding
Services/
  IUserService.cs             - User management interface
  UserService.cs              - User CRUD with file storage
  IEncryptionService.cs       - Encryption interface
  EncryptionService.cs        - AES-256 encryption
```

## Data Models

### User.cs
```csharp
namespace StudyHelper.Models;

public class User
{
	public required string Username { get; set; }
	public required string PasswordHash { get; set; }
	public DateTime CreatedDate { get; set; }
	public DateTime? LastLoginDate { get; set; }
}
```

### LoginViewModel.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace StudyHelper.ViewModels;

public class LoginViewModel
{
	[Required(ErrorMessage = "Username is required")]
	[Display(Name = "Username")]
	public required string Username { get; set; }

	[Required(ErrorMessage = "Password is required")]
	[DataType(DataType.Password)]
	[Display(Name = "Password")]
	public required string Password { get; set; }

	[Display(Name = "Remember me")]
	public bool RememberMe { get; set; }
}
```

### RegisterViewModel.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace StudyHelper.ViewModels;

public class RegisterViewModel
{
	[Required(ErrorMessage = "Username is required")]
	[StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
	[Display(Name = "Username")]
	public required string Username { get; set; }

	[Required(ErrorMessage = "Password is required")]
	[StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
	[DataType(DataType.Password)]
	[Display(Name = "Password")]
	public required string Password { get; set; }

	[Required(ErrorMessage = "Please confirm your password")]
	[DataType(DataType.Password)]
	[Compare("Password", ErrorMessage = "Passwords do not match")]
	[Display(Name = "Confirm Password")]
	public required string ConfirmPassword { get; set; }
}
```

## Service Interfaces

### IEncryptionService.cs
```csharp
namespace StudyHelper.Services;

public interface IEncryptionService
{
	/// <summary>
	/// Encrypts plaintext data using AES-256-GCM
	/// </summary>
	byte[] Encrypt(string plaintext);

	/// <summary>
	/// Decrypts encrypted data using AES-256-GCM
	/// </summary>
	string Decrypt(byte[] ciphertext);
}
```

### IUserService.cs
```csharp
namespace StudyHelper.Services;

public interface IUserService
{
	/// <summary>
	/// Validates user credentials and returns user if valid
	/// </summary>
	Task<User?> ValidateUserAsync(string username, string password);

	/// <summary>
	/// Creates a new user account
	/// </summary>
	Task<bool> CreateUserAsync(string username, string password);

	/// <summary>
	/// Checks if username already exists
	/// </summary>
	Task<bool> UserExistsAsync(string username);

	/// <summary>
	/// Updates user's last login timestamp
	/// </summary>
	Task UpdateLastLoginAsync(string username);

	/// <summary>
	/// Retrieves user by username
	/// </summary>
	Task<User?> GetUserAsync(string username);
}
```

## Service Implementation Details

### EncryptionService.cs
**Responsibilities:**
- Encrypt/decrypt user data file
- Use AES-256-GCM for authenticated encryption
- Manage encryption key from configuration

**Key Features:**
- 256-bit encryption key
- Random IV (Initialization Vector) for each encryption
- Authentication tag for tamper detection
- Exception handling for decryption failures

**Configuration:**
```json
"Authentication": {
  "EncryptionKey": "base64-encoded-32-byte-key"
}
```

### UserService.cs
**Responsibilities:**
- Read/write encrypted user file
- Hash passwords using PBKDF2
- Validate credentials
- Manage user CRUD operations

**File Storage:**
- Location: `App_Data/users.dat`
- Format: Encrypted JSON array of User objects
- Thread safety: File locking using `FileStream` exclusive access
- Caching: In-memory cache with 5-minute expiration

**Password Hashing:**
- Algorithm: PBKDF2
- Iterations: 100,000
- Salt: Random 128-bit per user
- Hash length: 256 bits

**File Operations:**
```csharp
// File structure (decrypted):
[
  {
	"Username": "user1",
	"PasswordHash": "base64-hashed-password",
	"CreatedDate": "2026-01-01T00:00:00Z",
	"LastLoginDate": "2026-05-26T12:00:00Z"
  }
]
```

## AccountController.cs

### Actions

#### GET: /Account/Login
```csharp
[AllowAnonymous]
[HttpGet]
public IActionResult Login(string? returnUrl = null)
{
	ViewData["ReturnUrl"] = returnUrl;
	return View();
}
```

#### POST: /Account/Login
```csharp
[AllowAnonymous]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
{
	ViewData["ReturnUrl"] = returnUrl;

	if (!ModelState.IsValid)
		return View(model);

	var user = await _userService.ValidateUserAsync(model.Username, model.Password);

	if (user == null)
	{
		ModelState.AddModelError(string.Empty, "Invalid username or password");
		return View(model);
	}

	// Create authentication claims
	var claims = new List<Claim>
	{
		new Claim(ClaimTypes.Name, user.Username),
		new Claim(ClaimTypes.NameIdentifier, user.Username)
	};

	var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
	var authProperties = new AuthenticationProperties
	{
		IsPersistent = model.RememberMe,
		ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(1)
	};

	await HttpContext.SignInAsync(
		CookieAuthenticationDefaults.AuthenticationScheme,
		new ClaimsPrincipal(claimsIdentity),
		authProperties);

	await _userService.UpdateLastLoginAsync(user.Username);

	return LocalRedirect(returnUrl ?? "/");
}
```

#### GET: /Account/Register
```csharp
[AllowAnonymous]
[HttpGet]
public IActionResult Register()
{
	return View();
}
```

#### POST: /Account/Register
```csharp
[AllowAnonymous]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Register(RegisterViewModel model)
{
	if (!ModelState.IsValid)
		return View(model);

	if (await _userService.UserExistsAsync(model.Username))
	{
		ModelState.AddModelError("Username", "Username already exists");
		return View(model);
	}

	var success = await _userService.CreateUserAsync(model.Username, model.Password);

	if (!success)
	{
		ModelState.AddModelError(string.Empty, "An error occurred creating your account");
		return View(model);
	}

	// Auto-login after registration
	var user = await _userService.GetUserAsync(model.Username);
	var claims = new List<Claim>
	{
		new Claim(ClaimTypes.Name, user!.Username),
		new Claim(ClaimTypes.NameIdentifier, user.Username)
	};

	var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
	await HttpContext.SignInAsync(
		CookieAuthenticationDefaults.AuthenticationScheme,
		new ClaimsPrincipal(claimsIdentity));

	return RedirectToAction("Index", "Home");
}
```

#### POST: /Account/Logout
```csharp
[Authorize]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Logout()
{
	await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	return RedirectToAction("Login");
}
```

## Program.cs Configuration

### Authentication Services
```csharp
// Add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/Account/Login";
		options.LogoutPath = "/Account/Logout";
		options.AccessDeniedPath = "/Account/AccessDenied";
		options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
		options.SlidingExpiration = true;
		options.Cookie.HttpOnly = true;
		options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
		options.Cookie.SameSite = SameSiteMode.Strict;
	});

builder.Services.AddAuthorization();

// Register custom services
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IUserService, UserService>();
```

### Middleware Pipeline
```csharp
var app = builder.Build();

// ... existing middleware

app.UseAuthentication();
app.UseAuthorization();

// ... existing middleware
```

## Error Handling

### Validation Errors
- Display field-level validation errors on form
- Use ASP.NET Core ModelState
- Client-side validation with jQuery Unobtrusive Validation

### Service Errors
- File I/O exceptions logged and return generic error message
- Encryption failures logged with specific error codes
- Concurrency conflicts retried automatically

### Security Errors
- Invalid credentials: Generic "Invalid username or password"
- Account lockout (future): "Account temporarily locked"
- CSRF validation failure: Return 400 Bad Request

## Logging

### Authentication Events
```csharp
_logger.LogInformation("User {Username} logged in successfully", username);
_logger.LogWarning("Failed login attempt for user {Username}", username);
_logger.LogInformation("User {Username} registered successfully", username);
_logger.LogInformation("User {Username} logged out", username);
```

### Service Operations
```csharp
_logger.LogDebug("Loading user data from encrypted file");
_logger.LogError(ex, "Failed to decrypt user data file");
_logger.LogWarning("User file not found, creating new file");
```

## Security Considerations

### Password Hashing
- Never log passwords or password hashes
- Use ASP.NET Core Identity's PasswordHasher
- Salt automatically included in hash

### File Security
- Store App_Data outside wwwroot
- Set restrictive file permissions (admin only)
- Validate file integrity on read

### Session Management
- HttpOnly cookies prevent XSS attacks
- Secure flag requires HTTPS
- SameSite prevents CSRF
- Sliding expiration updates on activity

### Input Validation
- Server-side validation always enforced
- Whitelist allowed characters in username
- Password complexity requirements
- Anti-forgery tokens on all forms

## Testing Requirements

### Unit Tests
- `EncryptionService_Tests`: Encrypt/decrypt round-trip, invalid data
- `UserService_Tests`: CRUD operations, password validation, file I/O
- `AccountController_Tests`: Login/register/logout flows, validation

### Integration Tests
- End-to-end registration and login
- Concurrent user file access
- Session persistence across requests
- Authorization enforcement on protected routes

## Performance Considerations

### Caching Strategy
- Cache decrypted user list in memory (5-minute TTL)
- Invalidate cache on user creation
- Thread-safe cache access

### Async Operations
- All file I/O operations async
- Password hashing offloaded to thread pool
- No blocking calls in request pipeline

### File Size Management
- Periodic cleanup of old user data (optional)
- Monitor file size growth
- Consider database migration at 10MB+ file size

## Deployment Considerations

### Configuration
- Generate unique encryption key per environment
- Store keys in Azure Key Vault (production)
- Use User Secrets for development

### File Backup
- Include App_Data in backup strategy
- Test restore procedures
- Document recovery process

### Migration Path
- Support for importing existing users
- Export functionality for database migration
- Backward compatibility for encrypted file format
