# User Stories

## Summary

| Status | Count |
|--------|-------|
| Backlog | 0 |
| In Progress | 0 |
| In Review | 5 |
| QA | 0 |
| Done | 0 |
| Blocked | 0 |

---

## Stories

### US-001: Hash passwords on user registration

- **Description:** As a developer, I want newly registered user passwords to be hashed using PBKDF2 before being saved, so that plaintext passwords are never written to disk.
- **Acceptance Criteria:**
  - [ ] `CreateUserAsync` calls `HashPassword` before storing the password on the `User` object.
  - [ ] The stored value in `users.dat` is the PBKDF2 hash string, not the plaintext password.
  - [ ] Existing `HashPassword` private method (PBKDF2-HMACSHA256, 100,000 iterations, 16-byte salt, 32-byte hash) is used without modification.
  - [ ] Plaintext password is never logged or persisted anywhere.
- **Status:** In Review
- **Blockers:** None
- **Last Updated:** 2025-07-15

---

### US-002: Fix password verification to use hash comparison

- **Description:** As a developer, I want `VerifyPassword` to perform a proper PBKDF2 hash comparison, so that login validation is secure and does not rely on plaintext equality.
- **Acceptance Criteria:**
  - [ ] `VerifyPassword` extracts the salt from the stored hash bytes (first 16 bytes).
  - [ ] `VerifyPassword` re-derives the PBKDF2 hash from the supplied plaintext password and extracted salt using the same parameters as `HashPassword`.
  - [ ] Comparison uses `CryptographicOperations.FixedTimeEquals` to prevent timing attacks.
  - [ ] The existing plaintext equality shortcut (`if (password == hashedPassword)`) is completely removed.
  - [ ] `VerifyPassword` returns `false` for any input that does not match, without throwing.
- **Status:** In Review
- **Blockers:** None
- **Last Updated:** 2025-07-15

---

### US-003: Remove hardcoded username allowlist from login

- **Description:** As a developer, I want login validation to apply uniformly to all users, so that no usernames receive special bypass treatment in code.
- **Acceptance Criteria:**
  - [ ] The `if (user.Username == "mccannj5" || user.Username == "hoffmanj7")` guard in `ValidateUserAsync` is removed.
  - [ ] `VerifyPassword` is called for every user attempting to log in, with no username-based exceptions.
  - [ ] All existing users can still log in once their stored passwords are hashed (see US-001 / US-004).
  - [ ] No other hardcoded username or password checks exist anywhere in `UserService`.
- **Status:** In Review
- **Blockers:** US-002 must be complete before this story is marked Done.
- **Last Updated:** 2025-07-15

---

### US-004: Migrate existing plaintext stored passwords to hashed passwords

- **Description:** As a developer, I want a one-time migration to re-hash any plaintext passwords already stored in `users.dat`, so that all accounts in the data file are protected after the fix is deployed.
- **Acceptance Criteria:**
  - [ ] A migration method (or startup check) detects whether a stored password value is already a valid PBKDF2 hash (base64, expected byte length) or is still plaintext.
  - [ ] Any plaintext password found is replaced with its PBKDF2 hash and saved back to `users.dat`.
  - [ ] Migration runs at application startup before the first login attempt is processed.
  - [ ] After migration, `users.dat` contains no plaintext passwords.
  - [ ] Migration is idempotent — running it multiple times produces no errors or double-hashing.
- **Status:** In Review
- **Blockers:** US-001 and US-002 must be complete first.
- **Last Updated:** 2025-07-15

---

### US-005: Re-enable file encryption for users.dat

- **Description:** As a developer, I want `users.dat` to be encrypted at rest using `IEncryptionService`, so that the user data file cannot be read if accessed directly on disk.
- **Acceptance Criteria:**
  - [ ] `IEncryptionService` is uncommented and re-registered in DI.
  - [ ] `LoadUsersAsync` decrypts the file bytes with `_encryptionService.Decrypt` before deserializing.
  - [ ] `SaveUsersAsync` encrypts the serialized JSON with `_encryptionService.Encrypt` before writing to disk.
  - [ ] All commented-out encryption lines in `UserService` are restored and functional.
  - [ ] Application starts and logs in successfully end-to-end with encryption active.
  - [ ] If the encrypted file is absent or corrupt, the service logs the error and returns an empty user list rather than crashing.
- **Status:** In Review
- **Blockers:** US-001, US-002, and US-003 must be complete first.
- **Last Updated:** 2025-07-15
