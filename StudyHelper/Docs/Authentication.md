# Authentication Setup

## Encryption Key Generation

The authentication system uses AES-256-GCM encryption to protect user credentials stored in `App_Data/users.dat`. You must generate a secure encryption key before deploying to production.

### Generate Encryption Key

Run the following PowerShell command to generate a secure 256-bit (32-byte) encryption key:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

### Configure Encryption Key

1. Generate the key using the command above
2. Update `appsettings.json` (for development) or use User Secrets / Azure Key Vault for production:

```json
{
  "Authentication": {
	"EncryptionKey": "YOUR_GENERATED_KEY_HERE",
	"CookieExpiration": 60
  }
}
```

### Production Deployment

**⚠️ IMPORTANT**: Never commit the actual encryption key to source control.

For production environments, use one of these approaches:

#### Option 1: User Secrets (Development)
```bash
dotnet user-secrets set "Authentication:EncryptionKey" "YOUR_GENERATED_KEY_HERE"
```

#### Option 2: Environment Variables
```bash
$env:Authentication__EncryptionKey = "YOUR_GENERATED_KEY_HERE"
```

#### Option 3: Azure Key Vault (Recommended for Production)
Store the key in Azure Key Vault and reference it in your application configuration.

## Authentication Flow

### Login Process
1. User navigates to `/Account/Login`
2. User enters username and password
3. System validates credentials against encrypted `users.dat` file
4. On success: Creates authentication cookie and redirects to requested page or Home
5. On failure: Displays validation error

### Registration Process
1. User navigates to `/Account/Register`
2. User enters username, password, and password confirmation
3. System validates:
   - Username is unique
   - Passwords match
   - Password meets complexity requirements
4. Password is hashed using PBKDF2 (100,000 iterations)
5. User record is encrypted and saved to `users.dat`
6. User is automatically logged in and redirected to Home

### Route Protection
All controllers except `AccountController` require authentication via `[Authorize]` attribute. Unauthenticated users are automatically redirected to `/Account/Login` with a return URL.

## Security Features

- **Password Hashing**: PBKDF2 with HMACSHA256 (100,000 iterations)
- **File Encryption**: AES-256-GCM with unique nonce per encryption
- **Secure Cookies**: HttpOnly, Secure, SameSite=Strict
- **Thread Safety**: File access protected with SemaphoreSlim
- **Cache**: 5-minute in-memory cache for user data to reduce disk I/O

## File Storage

User credentials are stored in:
```
StudyHelper/App_Data/users.dat
```

This file is:
- Created automatically on first user registration
- Encrypted with AES-256-GCM
- JSON format (when decrypted)
- Thread-safe for concurrent access

## Configuration Options

### appsettings.json

```json
{
  "Authentication": {
	"EncryptionKey": "REPLACE_WITH_SECURE_KEY",
	"CookieExpiration": 60
  },
  "PasswordPolicy": {
	"MinimumLength": 6,
	"RequireUppercase": false,
	"RequireLowercase": false,
	"RequireDigit": false,
	"RequireSpecialCharacter": false
  }
}
```

### Cookie Settings

Configured in `Program.cs`:
- **Expiration**: 60 minutes (sliding)
- **HttpOnly**: true (prevents JavaScript access)
- **Secure**: true (HTTPS only)
- **SameSite**: Strict (CSRF protection)
- **LoginPath**: `/Account/Login`
- **LogoutPath**: `/Account/Logout`
- **AccessDeniedPath**: `/Account/AccessDenied`

## Testing Authentication

### Manual Testing

1. **Generate encryption key** and update configuration
2. **Start application**: `dotnet run`
3. **Navigate to Home**: Should redirect to `/Account/Login`
4. **Register new account**: Click "Create new account"
5. **Login**: Enter credentials from registration
6. **Verify protection**: Access should be granted to all features
7. **Logout**: Click username dropdown → Logout
8. **Verify logout**: Should redirect to login page

### First Time Setup

When running the application for the first time:

1. Generate and configure encryption key (see above)
2. Launch application
3. Navigate to `/Account/Register`
4. Create first user account
5. System will create `App_Data/users.dat` automatically

## Troubleshooting

### "Decryption failed" Error
- Encryption key in configuration doesn't match the key used to encrypt `users.dat`
- Solution: Delete `App_Data/users.dat` and register users again with correct key

### "Unable to read user data file" Error
- File is corrupted or locked by another process
- Check logs for detailed error message
- Verify `App_Data` directory has write permissions

### Login Redirects to Login Page
- Cookie authentication not working
- Check that `UseAuthentication()` is called before `UseAuthorization()` in `Program.cs`
- Verify browser accepts cookies

### Access Denied After Login
- User cookie exists but route requires different authorization
- Check controller `[Authorize]` attributes
- Verify authentication scheme matches

## Development Notes

- The `App_Data` directory is excluded from source control (`.gitignore`)
- Never commit `users.dat` to source control
- Consider implementing password reset functionality in future iterations
- Consider adding account lockout after failed login attempts
- Consider adding email verification for new registrations
