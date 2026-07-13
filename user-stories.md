# User Stories

## Feature: Ability to Have Multiple Courses

**Branch:** `feature/ability-to-have-multiple-courses`

---

## Summary

| Status | Count |
|--------|-------|
| Backlog | 7 |
| In Progress | 0 |
| In Review | 0 |
| QA | 0 |
| Done | 0 |
| Blocked | 0 |

---

## Stories

### US-001: View Course Settings Page

- **Description:** As a user, I want a Course Settings page accessible from the Settings menu so that I can view and manage all my courses in one place.
- **Acceptance Criteria:**
  - [ ] A "Course Settings" link appears under a Settings navigation menu item.
  - [ ] The Course Settings page lists all courses currently configured for the logged-in user.
  - [ ] The page is only accessible to authenticated users.
  - [ ] The page displays each course's Course Name, Instructor, and Created/Updated dates.
- **Status:** Backlog
- **Blockers:** None
- **Last Updated:** 2025-07-14

---

### US-002: Add a Course

- **Description:** As a user, I want to add a new course so that I can organise my study materials by subject.
- **Acceptance Criteria:**
  - [ ] A user can add a new course from the Course Settings page.
  - [ ] Each course requires a Course Name and Instructor field; both are mandatory.
  - [ ] Course Name may not contain spaces or special characters that would be invalid in a directory name (only letters, numbers, hyphens, and underscores allowed).
  - [ ] A user may have a maximum of 10 courses; attempting to add an eleventh is rejected with a clear error message.
  - [ ] On save, the course is persisted to `App_Data/{username}/course_settings.dat` as JSON.
  - [ ] On save, the course directory `App_Data/{username}/{coursename}/` is created automatically.
  - [ ] `CreatedDate` is set to the current UTC date/time on creation.
  - [ ] The new course appears immediately in the course list.
- **Status:** Backlog
- **Blockers:** None
- **Last Updated:** 2025-07-14

---

### US-003: Remove a Course

- **Description:** As a user, I want to remove a course I no longer need so that my course list stays relevant.
- **Acceptance Criteria:**
  - [ ] Each course in the list has a Remove action.
  - [ ] The user is asked to confirm before a course is deleted.
  - [ ] On confirmation, the course entry is removed from `course_settings.dat`.
  - [ ] The course's directory under `App_Data/{username}/{coursename}/` and all its files are deleted.
  - [ ] If the removed course was the currently active course, the active course selection is cleared.
  - [ ] The course list refreshes immediately to reflect the removal.
- **Status:** Backlog
- **Blockers:** None
- **Last Updated:** 2025-07-14

---

### US-004: Select the Active Course

- **Description:** As a user, I want to select which course I am currently working on so that all study activity is associated with the correct course.
- **Acceptance Criteria:**
  - [ ] From the Course Settings page the user can designate one course as the active/current course.
  - [ ] The active course selection is persisted (e.g. in session or in `course_settings.dat`).
  - [ ] The active course name is visible in the UI (e.g. navigation bar or page header) while a course is selected.
  - [ ] All subsequent study-material and file-upload operations use the active course's directory.
  - [ ] If no active course is selected, the user is prompted to select one before accessing course-specific features.
- **Status:** Backlog
- **Blockers:** None
- **Last Updated:** 2025-07-14

---

### US-005: Course-Specific Study Material Storage

- **Description:** As a user, I want my study materials (Terms & Definitions and Equations) to be saved per course so that materials from different courses do not mix.
- **Acceptance Criteria:**
  - [ ] Terms & Definitions content is saved to `App_Data/{username}/{coursename}/TermsAndDefinitions.md`.
  - [ ] Equations content is saved to `App_Data/{username}/{coursename}/Equations.md`.
  - [ ] No spaces appear anywhere in the directory or file path.
  - [ ] Reading and writing study materials uses the active course's directory.
  - [ ] Existing study-material logic is updated to resolve file paths from the active course rather than a global location.
  - [ ] If the course directory does not exist it is created automatically before writing.
- **Status:** Backlog
- **Blockers:** US-004 must be complete (active course selection) before file paths can be resolved.
- **Last Updated:** 2025-07-14

---

### US-006: Course-Specific File Upload Storage

- **Description:** As a user, I want uploaded study files to be stored in my active course's directory so that uploads are kept separate per course.
- **Acceptance Criteria:**
  - [ ] Uploaded files are stored under `App_Data/{username}/{coursename}/`.
  - [ ] No spaces appear in the upload destination path.
  - [ ] The upload destination directory is created automatically if it does not exist.
  - [ ] Existing file-upload logic is updated to resolve the destination from the active course.
  - [ ] Files uploaded to one course are not visible or accessible from another course.
- **Status:** Backlog
- **Blockers:** US-004 must be complete (active course selection) before upload paths can be resolved.
- **Last Updated:** 2025-07-14

---

### US-007: Persist Course Settings to File

- **Description:** As a developer, I want course settings to be stored in a structured JSON file per user so that course data survives application restarts without requiring a database.
- **Acceptance Criteria:**
  - [ ] Course settings are stored at `App_Data/{username}/course_settings.dat` in valid JSON format.
  - [ ] Each entry in the file contains: `Username`, `CourseName`, `Instructor`, `CreatedDate`, `UpdatedDate`.
  - [ ] `UpdatedDate` is updated on every save.
  - [ ] No spaces appear in the file path (usernames containing spaces are sanitised to underscores or rejected at registration).
  - [ ] The file is created automatically on the first course save if it does not exist.
  - [ ] Concurrent read/write access is handled safely (file lock or equivalent).
  - [ ] A maximum of 10 course entries is enforced at the persistence layer as well as the UI layer.
- **Status:** Backlog
- **Blockers:** None
- **Last Updated:** 2025-07-14

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
