# Frontend Design: Ability to Have Multiple Courses

## Overview

This document covers all UI changes required to support per-user, per-course study material
organisation. The primary touchpoints are a new **Course Settings** page under Settings, a
persistent **active course indicator** in the navigation bar, and an **active course selection
gate** that prevents access to course-specific features before a course has been chosen.

No new third-party libraries are introduced. All new views follow the existing Bootstrap 5 /
Razor pattern used throughout the application.

---

## User Stories Addressed

- US-001: View Course Settings Page
- US-002: Add a Course
- US-003: Remove a Course
- US-004: Select the Active Course

---

## Component Design

### 1. `Views/Settings/CourseSettings.cshtml`

**Responsibility:** Primary management page for the logged-in user's courses.

**Sections:**

| Section | Description |
|---|---|
| Course list table | Columns: Course Name, Instructor, Created, Updated, Active, Actions |
| "Set Active" button | Per-row POST button; marks that course as the active course |
| "Remove" button | Per-row POST button; triggers a confirmation modal before delete |
| "Add Course" form | Inline form (or collapsible panel) with `CourseName` and `Instructor` inputs |
| Max-courses notice | Informational alert when the user has reached the 10-course limit; hides the Add form |
| Active course badge | Visual indicator (Bootstrap `badge bg-success`) on the currently active row |

**ViewModel:** `CourseSettingsViewModel` (see ViewModels section below).

**Validation:** Client-side via jQuery Unobtrusive Validation (already present in the project).
Server-side via data annotations on `AddCourseViewModel`.

---

### 2. `Views/Shared/_Layout.cshtml` — Active Course Indicator

**Responsibility:** Show the currently active course name in the navigation bar so the user
always knows which course's data they are working with.

**Change:** Add a small badge/text element inside the authenticated nav section:

```
[Username ▾]   |   Course: [CourseName]
```

- Rendered only when a course is active (session key `ActiveCourseName` is set).
- If no course is active, show: `No course selected` with a link to Settings → Course Settings.
- Placement: between the Settings dropdown and the user dropdown, visible only to authenticated
  users.

---

### 3. `Views/Shared/_NoCourseSelected.cshtml` (partial)

**Responsibility:** Reusable alert partial rendered at the top of any course-specific feature
page (Quiz, Exercise, Flashcard, Study Materials) when no active course is in session.

```
⚠ No course selected. Please go to Settings → Course Settings to select a course before
  continuing.   [Go to Course Settings]
```

- Rendered via `@await Html.PartialAsync("_NoCourseSelected")` at the top of affected views.
- Controllers will check for an active course and return an appropriate redirect or pass a flag
  to the ViewModel; the partial is shown when `ViewData["NoCourseSelected"] == true`.

---

### 4. New ViewModels

#### `CourseSettingsViewModel`

```
List<CourseViewModel>  Courses         // All courses for this user
string?                ActiveCourseName // Currently active course (null = none)
bool                   AtMaxCapacity   // true when Courses.Count == 10
AddCourseViewModel     AddCourse       // Bound to the Add form
```

#### `CourseViewModel` (read-only display)

```
string    CourseName
string    Instructor
DateTime  CreatedDate
DateTime  UpdatedDate
bool      IsActive
```

#### `AddCourseViewModel`

```
[Required]
[StringLength(50, MinimumLength = 1)]
[RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Course name may only contain letters, numbers, hyphens, and underscores.")]
string CourseName

[Required]
[StringLength(100)]
string Instructor
```

---

## Data Flow

```
GET  /Settings/CourseSettings
  -> SettingsController.CourseSettings()
  -> ICourseService.GetCoursesAsync(username)
  -> CourseSettingsViewModel populated
  -> View rendered

POST /Settings/AddCourse
  -> [ValidateAntiForgeryToken]
  -> SettingsController.AddCourse(AddCourseViewModel)
  -> ICourseService.AddCourseAsync(username, model)
  -> RedirectToAction("CourseSettings")

POST /Settings/SetActiveCourse
  -> [ValidateAntiForgeryToken]
  -> SettingsController.SetActiveCourse(string courseName)
  -> ICourseService.SetActiveCourseAsync(username, courseName)
  -> Session["ActiveCourseName"] = courseName
  -> Session["ActiveCourseNameSafe"] = sanitised courseName
  -> RedirectToAction("CourseSettings")

POST /Settings/RemoveCourse
  -> [ValidateAntiForgeryToken]
  -> SettingsController.RemoveCourse(string courseName)
  -> ICourseService.RemoveCourseAsync(username, courseName)
  -> If removed course was active: clear Session["ActiveCourseName"]
  -> RedirectToAction("CourseSettings")
```

Active course values stored in server-side **session** (already configured in `Program.cs`):

| Key | Value |
|---|---|
| `ActiveCourseName` | Display name (e.g. `Biology`) |
| `ActiveCourseNameSafe` | Filesystem-safe name, spaces replaced (e.g. `Biology`) |

Both values are written by `SetActiveCourse` and cleared by `RemoveCourse` when appropriate.

---

## UI/UX Considerations

### Navigation prompt
When a user navigates to a course-specific feature (Quiz, Exercise, Flashcards, Study Materials)
without an active course, they see the `_NoCourseSelected` partial with a clear call-to-action
link rather than an error page.

### Add Course form
- `CourseName` field: live client-side regex feedback preventing spaces/special characters.
- `Instructor` field: free text, max 100 characters.
- Submit button disabled when `AtMaxCapacity == true`; the max-courses alert explains why.

### Remove Course confirmation
Uses a Bootstrap modal (`id="confirmRemoveModal"`) populated via JavaScript data attributes
(`data-course-name`) on the Remove button. This avoids a separate confirm page and matches the
style of other destructive actions in the app.

```html
<button type="button" class="btn btn-danger btn-sm"
		data-bs-toggle="modal"
		data-bs-target="#confirmRemoveModal"
		data-course-name="@course.CourseName">
	Remove
</button>
```

The modal contains a hidden form that POSTs to `/Settings/RemoveCourse` with the course name.

### Active course badge
Active row highlighted with `table-success` Bootstrap row class and a `✓ Active` badge.
The "Set Active" button is hidden for the already-active row.

### No-course gate placement
Affected controllers: `QuizController`, `ExerciseController`, `FlashcardController`,
`EquationFlashcardController`, `GradedQuizController`, `GradedExerciseController`,
`SuperQuizController`, `StudyMaterialsController`.

Each affected controller action reads `HttpContext.Session.GetString("ActiveCourseName")`.
If null/empty, it sets `TempData["NoCourseWarning"]` and redirects to
`Settings/CourseSettings` (or renders the partial in the view — implementation team to choose
the consistent approach across controllers).

---

## Technology Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Session storage for active course | `ISession` (already configured) | No new infrastructure; survives page navigation within a browser session |
| Confirmation modal | Bootstrap 5 modal (already in project) | No new JS dependency |
| Form validation | jQuery Unobtrusive Validation (already in project) | Consistent with all other forms |
| New CSS | Extend `site.css` or add `course-settings.css` | Keep scoped styles out of global file |

---

## Open Questions / Risks

| # | Question | Owner |
|---|---|---|
| 1 | Should the active course persist across browser sessions (stored in `course_settings.dat`) or only within a session? | Human approval required |
| 2 | If a user has only one course, should it be auto-selected as active on login? | Human approval required |
| 3 | The `_NoCourseSelected` gate will redirect mid-flow for existing users who have not yet created a course. A migration/onboarding step may be needed. | Solutions Architect to note for backend design |
