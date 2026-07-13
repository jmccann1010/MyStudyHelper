# Backend Design: Ability to Have Multiple Courses

## Overview

This document covers all backend changes required to support per-user, per-course study material
organisation. The work introduces a new `ICourseService` / `CourseService` pair, a new
`Course` model, and targeted modifications to `UserStudyMaterialService`,
`MarkdownParserService`, and `EquationParserService` so that file-path resolution is
course-aware. `SettingsController` gains four new actions. No database or new NuGet packages
are required.

---

## User Stories Addressed

- US-002: Add a Course
- US-003: Remove a Course
- US-004: Select the Active Course
- US-005: Course-Specific Study Material Storage
- US-006: Course-Specific File Upload Storage
- US-007: Persist Course Settings to File

---

## Directory & File Layout

```
App_Data/
  {username}/                          ← per-user root (username is already alphanumeric per registration rules)
	course_settings.dat                ← JSON array of CourseRecord (US-007)
	{coursename}/                      ← per-course directory (no spaces, letters/numbers/hyphens/underscores)
	  TermsAndDefinitions.md           ← course-specific terms (US-005)
	  Equations.md                     ← course-specific equations (US-005)
	  metadata.json                    ← study-materials metadata (migrated from StudyMaterials/{username}/)
	  <uploaded files>                 ← any other uploaded files (US-006)
  StudyMaterials/                      ← legacy path (kept read-only for migration; not written to after this feature)
	{username}/
	  TermsAndDefinitions.md
	  Equations.md
	  metadata.json
```

**Path-safety rule:** `coursename` is the value stored in `Course.CourseName`, which is
validated at registration time to match `^[a-zA-Z0-9_-]+$` (enforced by `AddCourseViewModel`
and by `CourseService` before writing to disk). No sanitisation is applied at the service
layer; invalid names are rejected before they reach the service.

---

## API Endpoints

All endpoints are in `SettingsController` under the `[Authorize]` attribute.

| Method | Route | Action | Description |
|--------|-------|--------|-------------|
| GET  | `/Settings/CourseSettings`      | `CourseSettings()`            | Load and display all courses |
| POST | `/Settings/AddCourse`           | `AddCourse(AddCourseViewModel)` | Validate and create a course |
| POST | `/Settings/SetActiveCourse`     | `SetActiveCourse(string courseName)` | Persist active course to session |
| POST | `/Settings/RemoveCourse`        | `RemoveCourse(string courseName)` | Delete course entry and directory |

### Request / Response Shapes

#### GET `/Settings/CourseSettings`
- **Response:** `View(CourseSettingsViewModel)`
- Calls `ICourseService.GetCoursesAsync(username)` and reads active course from session.

#### POST `/Settings/AddCourse`
```
Form fields:
  AddCourse.CourseName   string  required  ^[a-zA-Z0-9_-]+$  max 50
  AddCourse.Instructor   string  required  max 100
```
- On validation failure: `View("CourseSettings", viewModel)` with `ModelState` errors.
- On success: `RedirectToAction("CourseSettings")` with `TempData["SuccessMessage"]`.
- On max-courses violation (≥ 10): returns model error; does not call service.

#### POST `/Settings/SetActiveCourse`
```
Form fields:
  courseName   string  required
```
- Sets `HttpContext.Session` keys `ActiveCourseName` and `ActiveCourseNameSafe`.
- Calls `ICourseService.SetActiveCourseAsync(username, courseName)` to persist to
  `course_settings.dat`.
- `RedirectToAction("CourseSettings")`.

#### POST `/Settings/RemoveCourse`
```
Form fields:
  courseName   string  required
```
- Calls `ICourseService.RemoveCourseAsync(username, courseName)`.
- If the removed course matches `Session["ActiveCourseName"]`, clears both session keys.
- `RedirectToAction("CourseSettings")`.

---

## Data Model

### `Models/Course.cs` (new)

```csharp
namespace StudyHelper.Models;

/// <summary>
/// In-memory representation of a single course belonging to a user.
/// </summary>
public class Course
{
	public required string Username     { get; set; }
	public required string CourseName   { get; set; }   // filesystem-safe; no spaces
	public required string Instructor   { get; set; }
	public DateTime        CreatedDate  { get; set; }
	public DateTime        UpdatedDate  { get; set; }
	public bool            IsActive     { get; set; }   // true = currently selected course
}
```

### `Models/CourseRecord.cs` (new — on-disk DTO)

```csharp
namespace StudyHelper.Models;

/// <summary>
/// On-disk representation stored in course_settings.dat.
/// IsActive is persisted so the selection survives app restarts.
/// </summary>
internal sealed class CourseRecord
{
	public required string Username     { get; set; }
	public required string CourseName   { get; set; }
	public required string Instructor   { get; set; }
	public DateTime        CreatedDate  { get; set; }
	public DateTime        UpdatedDate  { get; set; }
	public bool            IsActive     { get; set; }
}
```

### `course_settings.dat` — JSON format

```json
[
  {
	"Username":    "jmccann",
	"CourseName":  "Biology",
	"Instructor":  "Dr. Smith",
	"CreatedDate": "2025-07-14T10:00:00Z",
	"UpdatedDate": "2025-07-14T10:00:00Z",
	"IsActive":    true
  }
]
```

---

## Business Logic

### New: `Services/ICourseService.cs`

```csharp
public interface ICourseService
{
	Task<List<Course>>  GetCoursesAsync(string username);
	Task<Course?>       GetActiveCourseAsync(string username);
	Task<bool>          AddCourseAsync(string username, string courseName, string instructor);
	Task                SetActiveCourseAsync(string username, string courseName);
	Task<bool>          RemoveCourseAsync(string username, string courseName);
	string              GetCourseDirectory(string username, string courseName);
}
```

### New: `Services/CourseService.cs`

**Responsibilities:**
- Read/write `App_Data/{username}/course_settings.dat` as a JSON array of `CourseRecord`.
- Enforce the 10-course maximum at the service layer (defence-in-depth, UI also enforces it).
- Create the course directory on `AddCourseAsync`; delete it recursively on `RemoveCourseAsync`.
- Use a `SemaphoreSlim(1,1)` per-user file lock to prevent concurrent write corruption.
  Because the lock is keyed per username, a `ConcurrentDictionary<string, SemaphoreSlim>`
  is used to manage per-user semaphores.
- Invalidate the memory-cache entry for the course list after every write.

**Key method contracts:**

| Method | Behaviour |
|--------|-----------|
| `GetCoursesAsync` | Reads `course_settings.dat`; returns `[]` if file absent. Cache TTL: 5 min. |
| `GetActiveCourseAsync` | Returns the single `Course` where `IsActive == true`, or `null`. |
| `AddCourseAsync` | Validates count ≤ 9 before adding. Sets `CreatedDate = UpdatedDate = UtcNow`. Creates `App_Data/{username}/{courseName}/`. Returns `false` if course name already exists (case-insensitive). |
| `SetActiveCourseAsync` | Clears `IsActive` on all records, sets it on the named record, saves. |
| `RemoveCourseAsync` | Removes record from list, saves, then `Directory.Delete(path, recursive: true)`. Returns `false` if course not found. |
| `GetCourseDirectory` | Pure path helper: `Path.Combine(contentRootPath, "App_Data", username, courseName)`. Used by `UserStudyMaterialService`. |

---

### Modified: `Services/UserStudyMaterialService.cs`

**Current behaviour:** Resolves file paths as `App_Data/StudyMaterials/{username}/{file}`.

**Required change:** File paths must be resolved as `App_Data/{username}/{courseName}/{file}`.

The service signature changes are **additive only** — existing `username`-only overloads are
kept for backward compatibility during migration and will fall back to the legacy path when no
course name is supplied.

#### Interface additions to `IUserStudyMaterialService`

```csharp
// New overloads — course-aware
Task<bool>    UploadTermsAsync(string username, string courseName, IFormFile file);
Task<bool>    UploadEquationsAsync(string username, string courseName, IFormFile file);
Task<string>  GetEffectiveFilePathAsync(string username, string courseName, StudyMaterialType type);
Task<bool>    HasCustomMaterialAsync(string username, string courseName, StudyMaterialType type);
Task<string?> GetDecryptedContentAsync(string username, string courseName, StudyMaterialType type);
Task<bool>    DeleteUserMaterialAsync(string username, string courseName, StudyMaterialType type);
Task<List<UserStudyMaterial>> GetUserMaterialsAsync(string username, string courseName);
```

#### Path-resolution helpers (internal changes)

```csharp
// New helper — course-scoped
private string GetCourseFolder(string username, string courseName)
	=> Path.Combine(_environment.ContentRootPath, "App_Data", username, courseName);

private string GetCourseFilePath(string username, string courseName, StudyMaterialType type)
	=> Path.Combine(GetCourseFolder(username, courseName), GetFileName(type));

private string GetCourseMetadataPath(string username, string courseName)
	=> Path.Combine(GetCourseFolder(username, courseName), "metadata.json");
```

The existing `GetUserFolder` / `GetCustomFilePath` (pointing to `StudyMaterials/`) remain
unchanged for the legacy fallback path.

#### Fallback strategy for `GetEffectiveFilePathAsync` (course-aware overload)

```
1. If App_Data/{username}/{courseName}/{file} exists → return that path.
2. Else if App_Data/StudyMaterials/{username}/{file} exists → return legacy path (migration window).
3. Else → return App_Data/{file} (global default).
```

---

### Modified: `Services/MarkdownParserService.cs`

**Current signature:** `ParseMarkdownFilesAsync(string? username = null)`

**New signature:** `ParseMarkdownFilesAsync(string? username = null, string? courseName = null)`

- Cache key becomes `$"{CacheKey}_{username}_{courseName}"` when both are supplied.
- When `courseName` is provided, calls the new course-aware `IUserStudyMaterialService`
  overloads.
- Existing `username`-only path is unchanged (backward compatible).

---

### Modified: `Services/EquationParserService.cs`

**Current signature:** `ParseEquationsAsync(string? username = null)`

**New signature:** `ParseEquationsAsync(string? username = null, string? courseName = null)`

- Cache key becomes `$"{CacheKeyPrefix}{username}_{courseName}"` when both are supplied.
- When `courseName` is provided, resolves the equations file via the new course-aware
  `IUserStudyMaterialService` overloads.
- Existing `username`-only path is unchanged (backward compatible).

---

### Modified: Feature Controllers

The following controllers currently call services with only `username`. They must be updated
to also pass `courseName` from the session.

| Controller | Service call to update |
|---|---|
| `QuizController` | `_markdownParserService.ParseMarkdownFilesAsync(username, courseName)` |
| `FlashcardController` | `_markdownParserService.ParseMarkdownFilesAsync(username, courseName)` |
| `EquationFlashcardController` | `_equationParserService.ParseEquationsAsync(username, courseName)` |
| `ExerciseController` | `_equationParserService.ParseEquationsAsync(username, courseName)` |
| `GradedQuizController` | `_markdownParserService.ParseMarkdownFilesAsync(username, courseName)` |
| `GradedExerciseController` | `_equationParserService.ParseEquationsAsync(username, courseName)` |
| `SuperQuizController` | Both parser services |
| `StudyMaterialsController` | All `_materialService` calls gain `courseName` |

**Active course resolution helper (shared pattern):**

```csharp
// Added as a private helper on each affected controller, or extracted to a base controller
private string? GetActiveCourseNameSafe()
	=> HttpContext.Session.GetString("ActiveCourseNameSafe");
```

When `courseName` is `null` or empty (no active course), the controller redirects to
`Settings/CourseSettings` with `TempData["NoCourseWarning"] = true` rather than proceeding
with a null course name.

---

### Modified: `Program.cs`

Register the new service:

```csharp
builder.Services.AddScoped<ICourseService, CourseService>();
```

Ensure `AddSession` is already present (it is) — no changes needed there.

---

### Active Course: Session vs. Persistence

| Concern | Solution |
|---|---|
| Active course known within a session | `HttpContext.Session` keys `ActiveCourseName` / `ActiveCourseNameSafe` |
| Active course survives app restart / new login | `IsActive` field persisted in `course_settings.dat` |
| Session populated on login | `AccountController.Login` (post-sign-in) calls `ICourseService.GetActiveCourseAsync(username)` and writes session keys if a persisted active course exists |

---

### Active Course Session Hydration on Login

`AccountController.Login` POST, after `HttpContext.SignInAsync`, adds:

```csharp
var activeCourse = await _courseService.GetActiveCourseAsync(user.Username);
if (activeCourse != null)
{
	HttpContext.Session.SetString("ActiveCourseName",     activeCourse.CourseName);
	HttpContext.Session.SetString("ActiveCourseNameSafe", activeCourse.CourseName); // already safe
}
```

`AccountController` gains an `ICourseService` constructor dependency.

---

### Migration: Existing Study Materials

Users who already have files at `App_Data/StudyMaterials/{username}/` will not lose them.
The fallback step in `GetEffectiveFilePathAsync` (point 2 above) ensures legacy files continue
to be served until the user uploads new files into a course directory. No automated migration
script is required.

---

## Security Considerations

| Risk | Mitigation |
|---|---|
| Path traversal via `courseName` | `CourseService.AddCourseAsync` rejects any `courseName` that does not match `^[a-zA-Z0-9_-]+$` and throws `ArgumentException`. `GetCourseDirectory` calls `Path.GetFullPath` and asserts the result starts with the expected `App_Data/{username}/` prefix before any file I/O. |
| Accessing another user's course directory | All path helpers derive the root from `User.Identity.Name` (authenticated claim). `CourseService` always scopes reads/writes to `App_Data/{authenticatedUsername}/`. |
| Concurrent writes corrupting `course_settings.dat` | Per-user `SemaphoreSlim` in `CourseService` serialises all writes for a given user. |
| Deleting another user's course | `RemoveCourse` action reads `username` from `User.Identity.Name`, not from a form field. |
| Max-course enforcement bypass | Enforced at both UI (form hidden when count ≥ 10) and service layer (throws / returns false). |

---

## Technology Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Persistence | Plain JSON file (`course_settings.dat`) | Consistent with `users.dat` pattern; no DB dependency |
| Serialiser | `System.Text.Json` | Already used throughout the project |
| Concurrency | `SemaphoreSlim` per user (same pattern as `UserService`) | Proven pattern in the codebase; avoids locking unrelated users |
| Active course storage | Session (runtime) + `IsActive` field (persistence) | Session gives fast per-request access; `IsActive` survives restarts |
| Service registration | `AddScoped` | Consistent with other service registrations in `Program.cs` |
| No new NuGet packages | — | All required types are available in .NET 10 BCL and existing packages |

---

## Open Questions / Risks

| # | Question / Risk | Owner |
|---|---|---|
| 1 | **Active course on login:** If a user has exactly one course and no `IsActive` flag is set, should it auto-activate? Recommend yes — reduces friction for new users. | Human approval required |
| 2 | **Legacy metadata.json migration:** The existing `metadata.json` at `StudyMaterials/{username}/` tracks upload history. Should it be copied into the first course directory, or abandoned? Recommend: read-only fallback only; do not copy. | Human approval required |
| 3 | **`StudyMaterials:StorageFolder` config key** (`"StudyMaterials"`) will become unused for new writes. It should be left in `appsettings.json` during this feature (for the legacy read fallback) and removed in a follow-up cleanup. | Backend Engineer to note |
| 4 | **`ExerciseSettings:EquationsFilePath`** (`"App_Data/Equations.md"`) remains the global default fallback for equations. No change needed in this feature. | Backend Engineer to note |
| 5 | **Case sensitivity on Linux hosting:** `courseName` stored and compared case-sensitively. The `AddCourseAsync` duplicate check uses `OrdinalIgnoreCase` to prevent `Biology` and `biology` creating two directories that would collide on a case-insensitive (Windows) file system but differ on Linux. | Backend Engineer to implement |
