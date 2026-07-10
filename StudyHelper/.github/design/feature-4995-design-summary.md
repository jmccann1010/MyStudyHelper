# Feature #4995: Login and User Authentication - Design Summary

## Overview
Implement a secure login and registration system that protects all application routes. Users must authenticate to access the application, with credentials stored securely in an encrypted file.

## Business Requirements
- All application pages require authentication
- Unauthenticated users are redirected to login page
- New users can create accounts via registration page
- User credentials stored in encrypted file format
- Secure session management with logout capability

## Architecture Approach

### Authentication Strategy
- **Cookie-based authentication** using ASP.NET Core Identity's cookie authentication
- **Session management** for authenticated users
- **Encrypted file storage** for user credentials using AES-256 encryption
- **Password hashing** using PBKDF2 (via ASP.NET Core's Identity PasswordHasher)

### Security Considerations
1. **Password Storage**: Passwords hashed with PBKDF2 (100,000 iterations)
2. **File Encryption**: User data file encrypted with AES-256-GCM
3. **Session Security**: HttpOnly, Secure, SameSite cookies
4. **CSRF Protection**: Anti-forgery tokens on all forms
5. **Password Requirements**: Minimum 8 characters, complexity rules

### Data Storage
- **Location**: `App_Data/users.dat` (encrypted binary file)
- **Format**: JSON-serialized user data, encrypted with AES-256-GCM
- **Key Management**: Application-specific encryption key stored in configuration
- **Backup**: File-based, can be included in application backups

## Technical Components

### Models
- `User.cs` - User entity model
- `LoginViewModel.cs` - Login form data
- `RegisterViewModel.cs` - Registration form data

### Services
- `IUserService.cs` - User management interface
- `UserService.cs` - User CRUD operations with file I/O
- `IEncryptionService.cs` - Encryption/decryption interface
- `EncryptionService.cs` - AES-256-GCM implementation

### Controllers
- `AccountController.cs` - Login, Register, Logout actions

### Views
- `Views/Account/Login.cshtml` - Login page
- `Views/Account/Register.cshtml` - Registration page
- Update `Views/Shared/_Layout.cshtml` - Add user info and logout link

### Middleware
- Authentication middleware (built-in ASP.NET Core)
- Custom authorization filter for route protection

## User Flows

### Registration Flow
1. User navigates to application
2. Redirected to Login page
3. Clicks "Create Account" link
4. Fills registration form (username, password, confirm password)
5. System validates input and username uniqueness
6. Password hashed and user record encrypted to file
7. User automatically logged in
8. Redirected to home page

### Login Flow
1. User enters username and password
2. System retrieves encrypted user file
3. Decrypts and validates credentials
4. Creates authentication cookie
5. Redirects to originally requested page or home

### Logout Flow
1. User clicks "Logout" link
2. System clears authentication cookie
3. Session ended
4. Redirected to login page

## Configuration Requirements

### appsettings.json
```json
{
  "Authentication": {
	"EncryptionKey": "[Generated-32-byte-base64-key]",
	"CookieExpiration": 60,
	"RequireConfirmedAccount": false
  },
  "PasswordPolicy": {
	"RequiredLength": 8,
	"RequireUppercase": true,
	"RequireLowercase": true,
	"RequireDigit": true,
	"RequireNonAlphanumeric": false
  }
}
```

## Non-Functional Requirements

### Performance
- Login response time < 500ms
- File I/O operations async
- In-memory caching of decrypted user data (with expiration)

### Security
- All passwords hashed with salt
- File encryption with authenticated encryption (AES-GCM)
- Secure cookie transmission (HTTPS required in production)
- Session timeout after 60 minutes of inactivity
- Rate limiting on login attempts (future enhancement)

### Scalability
- Single encrypted file suitable for <= 1000 users
- For larger deployments, migrate to database backend
- File locking mechanism for concurrent access

## Dependencies
- ASP.NET Core Authentication middleware
- System.Security.Cryptography (AES encryption)
- Microsoft.AspNetCore.Cryptography.KeyDerivation (password hashing)

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| File corruption | Users cannot login | Implement backup/restore mechanism, validation on read |
| Encryption key exposure | All passwords compromised | Store key in secure configuration, use user secrets in dev |
| Concurrent file access | Data corruption | Implement file locking, retry logic |
| Password reset | User locked out | Implement password reset via security questions (future) |

## Testing Strategy
- Unit tests for UserService and EncryptionService
- Integration tests for login/register flows
- Security testing for encryption/decryption
- Manual testing for UX flows

## Future Enhancements
- Password reset functionality
- "Remember me" option
- Account lockout after failed attempts
- Email verification
- Two-factor authentication
- OAuth provider integration
