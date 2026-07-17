# Backend Design: Validating Study Material

## Overview

This document covers all backend changes required to implement format validation
with detailed counts and per-line / per-block error reporting for the two study
material file types: **Terms and Definitions** and **Equations**.

The changes touch four existing files and add no new files:

| File | Change type |
|---|---|
| `Models/FileValidationResult.cs` | Add three integer count properties |
| `Services/FileValidationService.cs` | Rewrite `ValidateTermsFormatAsync` and `ValidateEquationsFormatAsync` |
| `Services/UserStudyMaterialService.cs` | Surface `FileValidationResult` counts back to the controller |
| `Services/IUserStudyMaterialService.cs` | Change upload method return type from `bool` to `FileValidationResult` |
| `Controllers/StudyMaterialsController.cs` | Build specific success / warning / error `TempData` messages from the result |

---

## User Stories Addressed

- US-001: Validate Terms and Definitions File Format on Upload
- US-002: Validate Equations File Format on Upload
- US-003: Report Specific Format Errors in the Terms and Definitions File
- US-004: Report Specific Format Errors in the Equations File

---

## API Endpoints

No new endpoints are added. The two existing POST endpoints change their internal
behaviour only; their URL, method, and form fields are unchanged.

### POST `/StudyMaterials/UploadTerms`

- **Request:** `multipart/form-data`, field `file` (.md)
- **Response:** `RedirectToAction("Manage")` with one of:
  - `TempData["SuccessMessage"]` — saved, counts > 0
  - `TempData["WarningMessage"]` — saved, count = 0
  - `TempData["ErrorMessage"]` — rejected (format errors or existing checks)

### POST `/StudyMaterials/UploadEquations`

- Same pattern as above.

---

## Data Model

### `Models/FileValidationResult.cs` — extended

Add three new integer properties. All existing properties (`IsValid`, `Errors`,
`Warnings`) are unchanged.

```csharp
public class FileValidationResult
{
	public bool IsValid { get; set; }
	public List<string> Errors { get; set; } = [];
	public List<string> Warnings { get; set; } = [];

	// US-001 / US-003 — populated by ValidateTermsFormatAsync
	public int ParsedSectionCount { get; set; }
	public int ParsedTermCount { get; set; }

	// US-002 / US-004 — populated by ValidateEquationsFormatAsync
	public int ParsedEquationCount { get; set; }
}
```

---

## Business Logic

### Layer responsibilities

```
Controller  →  reads FileValidationResult, builds TempData message strings
Service     →  orchestrates validation steps, decides save/reject, returns FileValidationResult
FileValidationService  →  format rules, error/warning messages, counts (single responsibility)
```

---

### `FileValidationService.ValidateTermsFormatAsync` (US-001 / US-003)

The **Terms and Definitions** file format is:

```
# Optional top-level heading (ignored)

## Section Name

Term: Definition text that must be at least 10 characters long.
Another Term: Another definition.
- Optional bullet point

## Another Section

...
```

The rewritten method must perform the following checks in a single pass over the
lines, tracking `lineNumber` (1-based):

#### Rules that produce **Errors** (cause rejection)

| Rule ID | Condition | Error message template |
|---|---|---|
| T-E1 | A `Term: Definition` line is encountered before any `##` heading has been seen | `"Line {n}: Term-definition pair found before any section heading (##). All terms must be under a ## heading."` |
| T-E2 | A line contains ` - ` (dash with spaces) but no `:` and is not a bullet point or blank — heuristic for a misformatted term | `"Line {n}: '{preview}' appears to be a term but is missing a colon (:) separator."` |
| T-E3 | A non-blank, non-bullet, non-heading line under a `##` section that does not parse as a valid `Term: Definition` pair (term length > 200, definition length < 10, or term contains a tab or starts with `http`) | `"Line {n}: Unexpected content. Only Term: Definition pairs and bullet points are expected under a ## heading."` |

> **T-E2 heuristic:** a line matches when it: (a) is not blank, (b) does not start
> with `#`, `-`, or `*`, (c) contains ` - ` but does **not** contain `:`.

> **T-E3** only fires when the line is under a `##` heading and the term is present
> (colonIndex > 0) but fails the validity criteria already applied in
> `MarkdownParserService.ParseFileAsync`. Do not flag lines that are entirely
> outside a section — they are covered by T-E1.

#### Rules that produce **Warnings** (do not cause rejection)

| Rule ID | Condition | Warning message |
|---|---|---|
| T-W1 | Zero `##` headings found in the entire file | `"No section headings (##) were found. Flashcards and quizzes may not work. Check your file format."` |
| T-W2 | Zero valid `Term: Definition` pairs parsed | `"No term-definition pairs were found. Ensure each entry is on its own line in the format  Term: Definition  under a ## section heading."` |

#### Count population

- `ParsedSectionCount` = number of `##` headings encountered.
- `ParsedTermCount` = number of lines that successfully parse as a `Term: Definition`
  pair (same criteria as `MarkdownParserService`):  
  - colon found at index > 0  
  - term is non-empty, ≤ 200 chars, no tab, does not start with `http`  
  - definition is non-empty, ≥ 10 chars

#### Rejection logic

`IsValid = Errors.Count == 0`

Warnings do not affect `IsValid`.

---

### `FileValidationService.ValidateEquationsFormatAsync` (US-002 / US-004)

The **Equations** file format is a flat sequence of named blocks:

```
Equation Name: The Accounting Equation
Equation Summary: The fundamental equation...
Equation: Assets = Liabilities + Stockholders' Equity

Equation Name: Net Income
...
```

A valid block requires all three keys in order, separated by blank lines.
The method tracks a simple state machine:

```
State: IDLE → (Equation Name:) → HAS_NAME → (Equation Summary:) → HAS_SUMMARY
	 → (Equation:) → HAS_EQUATION → (blank line) → emit block, back to IDLE
```

#### Rules that produce **Errors** (cause rejection)

| Rule ID | Condition | Error message template |
|---|---|---|
| E-E1 | An `Equation Summary:` line is encountered but the current block has no name yet | `"Line {n}: 'Equation Summary:' found without a preceding 'Equation Name:'. Each block must start with 'Equation Name:'."` |
| E-E2 | An `Equation:` line is encountered but the current block has no summary | `"Line {n}: 'Equation:' found without a preceding 'Equation Summary:' for equation '{name}'."` |
| E-E3 | A blank line (block terminator) is encountered when the current block has a name and summary but no equation line | `"Equation '{name}' (started at line {startLine}): No equation line found. Add a line in the format 'Equation: Left = Right'."` |
| E-E4 | A blank line is encountered when the current block has a name but no summary | `"Equation '{name}' (started at line {startLine}): Missing summary line. Add 'Equation Summary: ...' after the name."` |
| E-E5 | The `Equation:` value does not contain `=` | `"Line {n}: Equation '{value}' is missing an equals sign (=)."` |
| E-E6 | The left side of `=` is blank | `"Line {n}: Equation '{value}' has a blank left-hand side."` |
| E-E7 | The right side of `=` is blank | `"Line {n}: Equation '{value}' has a blank right-hand side."` |

#### Rules that produce **Warnings** (do not cause rejection)

| Rule ID | Condition | Warning message |
|---|---|---|
| E-W1 | Zero equations successfully parsed | `"No equations were found. Ensure each block has 'Equation Name:', 'Equation Summary:', and 'Equation: Left = Right'."` |

#### Count population

`ParsedEquationCount` = number of blocks where all three fields are present **and**
the equation string passes the `=` / non-blank-sides checks — i.e., the same blocks
that `EquationFlashcardParserService.TryCreateEquation` would accept.

#### Rejection logic

`IsValid = Errors.Count == 0`

---

### `Services/IUserStudyMaterialService.cs` — return type change

Both upload method pairs change their return type from `Task<bool>` to
`Task<FileValidationResult>`:

```csharp
// Legacy (no course)
Task<FileValidationResult> UploadTermsAsync(string username, IFormFile file);
Task<FileValidationResult> UploadEquationsAsync(string username, IFormFile file);

// Course-aware
Task<FileValidationResult> UploadTermsAsync(string username, string courseName, IFormFile file);
Task<FileValidationResult> UploadEquationsAsync(string username, string courseName, IFormFile file);
```

A `FileValidationResult` with `IsValid = false` and a single generic error is
returned for all early-exit paths (file too large, empty, wrong extension, malicious
content) so the controller always has a uniform result to inspect.

---

### `Services/UserStudyMaterialService.cs` — `UploadMaterialAsync` and `UploadCourseMaterialAsync`

Both private helpers change from `Task<bool>` to `Task<FileValidationResult>`.

The final `formatResult` returned by `ValidateTermsFormatAsync` /
`ValidateEquationsFormatAsync` carries the counts **and** the errors/warnings.
After the file is saved successfully this result is returned directly — the
`ParsedSectionCount`, `ParsedTermCount`, and `ParsedEquationCount` fields are
already populated.

Early-exit paths return:

```csharp
// Example: file too large
return new FileValidationResult
{
	IsValid = false,
	Errors  = ["File exceeds the maximum allowed size."]
};
```

The `ValidateMarkdownFileAsync` early-exit (wrong extension / unreadable) returns
its own `FileValidationResult` directly, which already has `IsValid = false`.

For the security scan early-exit, return the `securityResult` directly.

> The **commented-out** `ValidatePlainTextAsync` call (lines 92–98 of
> `UserStudyMaterialService.cs`) remains commented out — do not enable it in
> this feature.

---

### `Controllers/StudyMaterialsController.cs` — `UploadTerms` and `UploadEquations`

Both actions are updated to inspect `FileValidationResult` and build the
appropriate `TempData` message.

#### Helper method — error list builder (private, shared by both actions)

```
BuildErrorHtml(List<string> errors) → string
```

- Takes up to 10 errors from the list.
- Wraps them in `<ul><li>...</li></ul>`.
- If total errors > 10, appends `<li>...and {n} more errors. Please review the full file.</li>`.
- All `<li>` content is HTML-encoded (`HtmlEncoder.Default.Encode`) before insertion.
- Returns the full HTML string prefixed with the introductory sentence:
  `"Upload failed. The following format errors were found:"`.

#### `UploadTerms` logic

```
result = await _materialService.UploadTermsAsync(username [, courseName], file)

if (!result.IsValid)
	→ TempData["ErrorMessage"] = BuildErrorHtml(result.Errors)

else if (result.ParsedSectionCount == 0)
	→ TempData["WarningMessage"] = "Upload accepted, but no sections (## headings) were found. ..."

else if (result.ParsedTermCount == 0)
	→ TempData["WarningMessage"] = "Upload accepted, but no term-definition pairs were found. ..."

else
	→ TempData["SuccessMessage"] = $"Upload successful. Found {result.ParsedSectionCount} section(s) and {result.ParsedTermCount} term(s) and definition(s)."
```

#### `UploadEquations` logic

```
result = await _materialService.UploadEquationsAsync(username [, courseName], file)

if (!result.IsValid)
	→ TempData["ErrorMessage"] = BuildErrorHtml(result.Errors)

else if (result.ParsedEquationCount == 0)
	→ TempData["WarningMessage"] = "Upload accepted, but no equations were found. ..."

else
	→ TempData["SuccessMessage"] = $"Upload successful. Found {result.ParsedEquationCount} equation(s)."
```

---

## Security Considerations

- **HTML injection:** `BuildErrorHtml` HTML-encodes every error string using
  `HtmlEncoder.Default.Encode` before inserting it into `<li>` elements. Error
  strings originate entirely from the validation service (not from user file
  content) so risk is low, but encoding is applied as defence-in-depth.
- **Path traversal:** No new file paths are introduced. All existing path-safety
  guards in `UserStudyMaterialService` remain unchanged.
- **Content Security Policy:** `TempData` messages are rendered via `Html.Raw`
  in the view (see Frontend Design). The `style-src` CSP does not affect
  server-rendered HTML content. No inline event handlers or `style=` attributes
  are added.
- **Input validation:** All file-level guards (size, extension, readability,
  malicious pattern scan) run before format validation — unchanged.

---

## Technology Decisions

| Decision | Rationale |
|---|---|
| Return `FileValidationResult` instead of `bool` from upload methods | Avoids a second parse pass to get counts; keeps all validation data in one object flowing up to the controller |
| Single-pass line scan in `ValidateTermsFormatAsync` | Performance — avoids loading the entire file twice; consistent with how `MarkdownParserService.ParseFileAsync` already works |
| State-machine approach in `ValidateEquationsFormatAsync` | Mirrors the parser in `EquationFlashcardParserService`; the same block-detection logic is re-expressed as error-checking |
| Cap at 10 errors in controller, not service | Service returns all errors (useful for tests); controller truncates for display |
| No new services or files | Keeps the change surface small; all four files being modified already have tests |

---

## Open Questions / Risks

| # | Question / Risk | Owner |
|---|---|---|
| 1 | `IUserStudyMaterialService` signature change (bool → FileValidationResult) will break any existing tests that assert `true`/`false`. The QA Engineer must update those tests. | QA Engineer |
| 2 | `UploadCourseMaterialAsync` and `UploadMaterialAsync` are private helpers — both must be updated together. Ensure both legacy and course-aware paths return `FileValidationResult`. | Backend Engineer |
| 3 | The T-E3 "unexpected content" rule may produce false positives on files that use free-form paragraphs under `##` headings (e.g., module overview text). Confirm with the user whether such files exist before finalising the rule. | Human approval needed |
| 4 | If `ValidateTermsFormatAsync` produces both errors and warnings, only the error banner should be shown (warnings suppressed on rejection). The controller must not write to both `TempData["ErrorMessage"]` and `TempData["WarningMessage"]`. | Backend Engineer |
