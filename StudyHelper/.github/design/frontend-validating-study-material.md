# Frontend Design: Validating Study Material

## Overview

This document covers all view and UI changes required to surface file validation
feedback to the user on the **Manage Study Materials** page
(`Views/StudyMaterials/Manage.cshtml`).  
No new pages are introduced. All feedback is delivered through the existing
`TempData["SuccessMessage"]` / `TempData["ErrorMessage"]` / `TempData["WarningMessage"]`
pattern that is already rendered in `Manage.cshtml`.

---

## User Stories Addressed

- US-001: Validate Terms and Definitions File Format on Upload
- US-002: Validate Equations File Format on Upload
- US-003: Report Specific Format Errors in the Terms and Definitions File
- US-004: Report Specific Format Errors in the Equations File

---

## Component Design

### 1. `Views/StudyMaterials/Manage.cshtml` — Banner Changes

#### Existing banners (unchanged contract, new content)

| TempData key | Rendered as | Trigger |
|---|---|---|
| `TempData["SuccessMessage"]` | `alert-success` | Upload saved with counts |
| `TempData["WarningMessage"]` | `alert-warning` _(new)_ | Upload saved but zero counts |
| `TempData["ErrorMessage"]` | `alert-danger` | Upload rejected |

A **third banner type** must be added for `WarningMessage` alongside the two that
already exist. It uses the Bootstrap `alert-warning` class and the
`bi-exclamation-triangle-fill` icon, matching the existing danger banner style.

```razor
@if (TempData["WarningMessage"] != null)
{
	<div class="alert alert-warning alert-dismissible fade show" role="alert">
		<i class="bi bi-exclamation-triangle-fill"></i> @Html.Raw(TempData["WarningMessage"])
		<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
	</div>
}
```

> `Html.Raw` is required because multi-error messages are pre-formatted as an HTML
> `<ul>` list by the controller (see Backend Design). The controller is responsible
> for sanitising all content before building the HTML string — no raw user file
> content is ever written into it.

#### Success message format (US-001, US-002)

**Terms and Definitions (US-001):**
```
Upload successful. Found 3 sections and 42 terms and definitions.
```

**Equations (US-002):**
```
Upload successful. Found 12 equations.
```

#### Warning message format — zero counts (US-001, US-002)

**Terms — zero sections:**
```
Upload accepted, but no sections (## headings) were found.
Flashcards and quizzes may not work. Check your file format.
```

**Terms — zero terms:**
```
Upload accepted, but no term-definition pairs were found.
Ensure each entry is on its own line in the format  Term: Definition
under a ## section heading.
```

**Equations — zero equations:**
```
Upload accepted, but no equations were found. Ensure each equation
block has a name (Equation Name: ...), a summary (Equation Summary: ...),
and an equation line (Equation: Left = Right).
```

#### Error message format — format errors (US-003, US-004)

When the upload is rejected due to format errors the error banner renders
an introductory sentence followed by a `<ul>` list of specific messages:

```
Upload failed. The following format errors were found:
• Line 3: Term-definition pair found before any section heading (##). All terms must be under a ## heading.
• Line 14: 'Photosynthesis - The process…' appears to be a term but is missing a colon (:) separator.
• Line 22: Unexpected content. Only Term: Definition pairs and bullet points are expected under a ## heading.
[...and 7 more errors. Please review the full file.]
```

The controller caps the displayed list at **10 errors**; if the total exceeds 10 it
appends the truncation line. The existing plain-text error path (wrong extension,
empty file, malicious content) is unchanged and continues to use the single-string
`TempData["ErrorMessage"]` without a list.

---

### 2. `Views/StudyMaterials/Manage.cshtml` — Inline upload error removal

The hidden replace-file forms (lines 71–75 for terms, 139–143 for equations) use
`onchange="document.getElementById(...).submit();"` — inline event handlers which
are currently blocked by the `script-src 'self'` CSP. This is **out of scope for
this feature** and is noted as a separate defect; no change is made here.

---

## Data Flow

```
User selects file → POST /StudyMaterials/UploadTerms (or UploadEquations)
  → Controller calls UserStudyMaterialService.UploadTermsAsync / UploadEquationsAsync
  → Service calls FileValidationService.ValidateTermsFormatAsync /
	  ValidateEquationsFormatAsync (returns FileValidationResult with counts + errors)
  → Controller reads FileValidationResult:
	  IsValid=false  → builds HTML error list → TempData["ErrorMessage"]
	  IsValid=true, counts > 0 → builds count string → TempData["SuccessMessage"]
	  IsValid=true, counts = 0 → builds warning string → TempData["WarningMessage"]
  → RedirectToAction(Manage)
  → Manage.cshtml renders appropriate banner
```

---

## UI/UX Considerations

### Banner display rules

| Scenario | Banner colour | Icon |
|---|---|---|
| Saved, counts > 0 | `alert-success` (green) | `bi-check-circle-fill` |
| Saved, count = 0 (warning) | `alert-warning` (yellow) | `bi-exclamation-triangle-fill` |
| Rejected (format errors) | `alert-danger` (red) | `bi-exclamation-triangle-fill` |
| Rejected (existing: wrong ext / empty / malicious) | `alert-danger` (red) | `bi-exclamation-triangle-fill` |

### Accessibility

- All banners include `role="alert"` (already present in the existing banners).
- The error list uses a `<ul>` so screen readers enumerate items correctly.
- No JavaScript changes are required; all feedback is server-rendered.

### Layout

- No layout changes. Banners are shown at the top of the `col-lg-10` content
  column, above both upload cards, using the same positioning as today.

---

## Technology Decisions

| Decision | Rationale |
|---|---|
| Server-rendered banners via `TempData` | Consistent with every other page in the application; no JS dependency |
| `Html.Raw` for error list | Allows `<ul>/<li>` formatting; safe because the controller builds the HTML from validated, internal-only error strings — never from raw file content |
| New `TempData["WarningMessage"]` key | Keeps success and warning semantics separate; no ambiguity for the view |
| Cap errors at 10 in controller, not in view | Single responsibility; view stays a pure template |

---

## Open Questions / Risks

| # | Question / Risk | Owner |
|---|---|---|
| 1 | The inline `onchange` handlers on the replace-file forms are blocked by CSP. Should they be fixed in this branch or tracked separately? | Human approval needed |
| 2 | Should the warning banner auto-dismiss after a timeout, or require manual close? Current preference: manual close (consistent with existing banners). | Human approval needed |
