# Backend Design: Fix Encrypted Passwords

## Overview

This document covers the complete backend changes required to fix password security in the
`UserService`. The work addresses five defects identified in the approved User Stories:
passwords are stored in plaintext on registration, `VerifyPassword` uses a plaintext
equality check, login is restricted by a hardcoded username allowlist, `IEncryptionService`
is disabled in DI, and `users.dat` is stored as raw JSON rather than encrypted bytes.

No new files, tables, or external dependencies are introduced. All changes are confined to
`UserService.cs`, `Program.cs`, and a one-time startup migration path within `UserService`.

---

## User Stories Addressed

- US-001: Hash passwords on user registration
- US-002: Fix password verification to use hash comparison
- US-003: Remove hardcoded username allowlist from login
- US-004: Migrate existing plaintext stored passwords to hashed passwords
- US-005: Re-enable file encryption for `users.dat`

---

## Existing Infrastructure (No Changes Required)

The following are already correct and must not be modified:

| Component | Location | Notes |
|---|---|---|
| `HashPassword(string)` | `UserService.cs` | PBKDF2-HMACSHA256, 100k iterations, 16-byte salt, 32-byte hash — correct as-is |
| `IEncryptionService` | `Services/IEncryptionService.cs` | Interface is correct |
| `EncryptionService` | `Services/EncryptionService.cs` | AES-256-GCM implementation is correct |
| `Authentication:EncryptionKey` | `appsettings.json` / `appsettings.Development.json` | Key is already configured |
| `SaltSizeBytes`, `HashSizeBytes`, `Pbkdf2Iterations` | `UserService.cs` | Constants are correct |

---

## API Endpoints

No new endpoints. No changes to any controller, route, or HTTP surface. All fixes are
internal to the service layer.

---

## Data Model

### `User` (no property changes)

```csharp
public class User
{
	public required string Username { get; set; }
	public required string Password { get; set; }   // Will store PBKDF2 hash after fix
	public DateTime CreatedDate { get; set; }
	public DateTime? LastLoginDate { get; set; }
}
```

The `Password` field continues to hold a `string`. After the fix, its value will always be
a Base64-encoded PBKDF2 hash (48 bytes decoded: 16-byte salt + 32-byte hash = 64-character
Base64 string). This is the detection signature used in the migration.

### `users.dat` (format change)

| State | Format |
|---|---|
| **Before fix** | Raw UTF-8 JSON, e.g. `[{"Username":"x","Password":"plaintext",...}]` |
| **After fix** | AES-256-GCM encrypted bytes (nonce \|\| tag \|\| ciphertext) |

The file format change means the existing empty `users.dat` is compatible — the service
already handles an empty or missing file by creating a fresh empty list.

---

## Business Logic

### US-001 — `CreateUserAsync`: hash password before saving

**Current (broken):**
```csharp
var user = new User
{
	Username = username,
	Password = password,          // plaintext stored directly
	...
};
```

**Fixed:**
```csharp
var user = new User
{
	Username = username,
	Password = HashPassword(password),   // PBKDF2 hash stored
	...
};
```

No other changes to `CreateUserAsync`.

---

### US-002 — `VerifyPassword`: replace plaintext check with PBKDF2 re-derivation

**Current (broken):**
```csharp
private static bool VerifyPassword(string password, string hashedPassword)
{
	if (password == hashedPassword)   // plaintext comparison — insecure
		return true;
	return false;
}
```

**Fixed algorithm:**

1. Base64-decode `hashedPassword` → `hashBytes` (expected length: `SaltSizeBytes + HashSizeBytes` = 48 bytes).
2. Validate `hashBytes.Length == SaltSizeBytes + HashSizeBytes`; return `false` if not.
3. Copy first `SaltSizeBytes` (16) bytes → `salt`.
4. Call `KeyDerivation.Pbkdf2` with `password`, `salt`, `KeyDerivationPrf.HMACSHA256`,
   `Pbkdf2Iterations`, `HashSizeBytes` → `computedHash`.
5. Extract stored hash: bytes `[SaltSizeBytes .. end]` of `hashBytes` → `storedHash`.
6. Return `CryptographicOperations.FixedTimeEquals(computedHash, storedHash)`.

`FixedTimeEquals` is required to prevent timing-based side-channel attacks.

---

### US-003 — `ValidateUserAsync`: remove hardcoded allowlist

**Current (broken):**
```csharp
if (user.Username == "mccannj5" || user.Username == "hoffmanj7")
{
	if (VerifyPassword(password, user.Password)) { ... }
}
_logger.LogWarning("Failed login attempt...");
return null;
```

**Fixed:**

Remove the `if (user.Username == ...)` guard entirely. Call `VerifyPassword` for every
user unconditionally:

```csharp
if (VerifyPassword(password, user.Password))
{
	_logger.LogInformation("User {Username} logged in successfully", username);
	return user;
}

_logger.LogWarning("Failed login attempt for user {Username}", username);
return null;
```

No other changes to `ValidateUserAsync`.

---

### US-004 — Startup migration: re-hash any remaining plaintext passwords

A `private async Task MigratePasswordsAsync(List<User> users)` method is added to
`UserService`. It is called once from `LoadUsersAsync` after the user list is
deserialized, before the list is cached or returned.

**Detection heuristic — is a password already hashed?**

A stored password is considered a valid PBKDF2 hash if **all** of the following are true:

1. It is a valid Base64 string (no `FormatException` on `Convert.FromBase64String`).
2. The decoded byte array length equals exactly `SaltSizeBytes + HashSizeBytes` (48 bytes).

If either condition fails, the stored value is treated as plaintext and re-hashed.

**Migration flow:**

```
foreach user in users:
	if IsAlreadyHashed(user.Password):
		skip
	else:
		user.Password = HashPassword(user.Password)
		migrated = true

if migrated:
	await SaveUsersAsync(users)   // persist updated hashes
	invalidate cache
```

**Idempotency:** Because a 48-byte Base64 string cannot be a valid PBKDF2 output of a
plaintext password that is itself 48 bytes decoded (the iteration and salt ensure
irreversibility), running migration on already-hashed data is a no-op.

**`users.dat` is currently empty** (confirmed: 0 bytes). Migration will find no users and
take no action on first run. It protects against future scenarios where plaintext data
might be re-introduced.

---

### US-005 — Re-enable `IEncryptionService` in `UserService` and DI

#### `Program.cs`

Uncomment the single commented line:

```csharp
// Before:
//builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

// After:
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
```

`EncryptionService` is registered as `Singleton` because it holds only the immutable
derived key — this is correct and must not be changed to `Scoped` or `Transient`.

#### `UserService` constructor

Uncomment the `IEncryptionService` parameter, field, and null check:

```csharp
// Before (all commented out):
//private readonly IEncryptionService _encryptionService;
//IEncryptionService encryptionService,
//ArgumentNullException.ThrowIfNull(encryptionService);
//_encryptionService = encryptionService;

// After (restored):
private readonly IEncryptionService _encryptionService;
// ...in constructor:
IEncryptionService encryptionService,
ArgumentNullException.ThrowIfNull(encryptionService);
_encryptionService = encryptionService;
```

#### `LoadUsersAsync`

Replace the raw byte read with decryption:

```csharp
// Before:
var jsonUsers = await File.ReadAllBytesAsync(_userFilePath);
users = JsonSerializer.Deserialize<List<User>>(jsonUsers) ?? [];

// After:
var encryptedData = await File.ReadAllBytesAsync(_userFilePath);
var json = _encryptionService.Decrypt(encryptedData);
users = JsonSerializer.Deserialize<List<User>>(json) ?? [];
```

**Empty file guard:** `LoadUsersAsync` must check `encryptedData.Length == 0` before
calling `Decrypt`. An empty file means no users exist — return an empty list directly
without calling `Decrypt` (which would throw on zero-length input).

```csharp
if (encryptedData.Length == 0)
{
	return [];
}
var json = _encryptionService.Decrypt(encryptedData);
```

#### `SaveUsersAsync`

Replace the raw text write with encryption:

```csharp
// Before:
File.WriteAllText(_userFilePath, json);

// After:
var encryptedData = _encryptionService.Encrypt(json);
await File.WriteAllBytesAsync(_userFilePath, encryptedData);
```

Note: `File.WriteAllText` (sync) is replaced with `await File.WriteAllBytesAsync` (async)
for consistency with the rest of the service.

---

## Implementation Order

The stories must be implemented in this exact order to ensure each step is testable and
does not break the next:

```
US-002  Fix VerifyPassword          (no side-effects; safe to fix first)
   ↓
US-001  Hash in CreateUserAsync     (depends on VerifyPassword being correct)
   ↓
US-003  Remove username allowlist   (depends on VerifyPassword being correct)
   ↓
US-004  Add MigratePasswordsAsync   (depends on HashPassword and VerifyPassword both correct)
   ↓
US-005  Re-enable EncryptionService (depends on all user data paths being stable)
```

---

## Security Considerations

| Concern | Mitigation |
|---|---|
| Timing attack on password comparison | `CryptographicOperations.FixedTimeEquals` used in `VerifyPassword` |
| Salt reuse | `RandomNumberGenerator.GetBytes` generates a new salt per `HashPassword` call |
| Weak iteration count | 100,000 PBKDF2 iterations — meets current NIST SP 800-132 guidance |
| Plaintext in logs | No password value (plaintext or hash) is ever passed to a logger |
| Key stored in source | `EncryptionKey` in `appsettings.json` is a placeholder; real key must be in `appsettings.Development.json` (gitignored) or environment variable in production |
| AES-GCM nonce reuse | `EncryptionService.Encrypt` generates a fresh `RandomNumberGenerator` nonce per call — correct |
| `users.dat` readable on disk | AES-256-GCM encryption (US-005) provides confidentiality and integrity |

---

## Technology Decisions

| Decision | Rationale |
|---|---|
| PBKDF2-HMACSHA256 | Already implemented correctly via `Microsoft.AspNetCore.Cryptography.KeyDerivation`; no new dependency |
| `CryptographicOperations.FixedTimeEquals` | Built into `System.Security.Cryptography`; no new dependency; constant-time guarantee |
| AES-256-GCM for file encryption | Already implemented in `EncryptionService`; provides authenticated encryption (prevents tampering) |
| `AddSingleton` for `EncryptionService` | Holds only immutable key bytes derived at startup — singleton lifetime is correct |
| No new NuGet packages | All required types are in `System.Security.Cryptography` and `Microsoft.AspNetCore.Cryptography.KeyDerivation` |

---

## Open Questions / Risks

| # | Question / Risk | Owner |
|---|---|---|
| 1 | Production `EncryptionKey` must be rotated from the placeholder in `appsettings.json` before deployment. Consider moving to environment variables or Azure Key Vault. | Human review |
| 2 | `users.dat` is currently empty. If the file ever contained plaintext passwords backed up elsewhere, those backups must be deleted manually. | Human review |
| 3 | Once US-005 is deployed, the file format changes to encrypted bytes. Any rollback would require decrypting the file first before downgrading. | Engineering |
