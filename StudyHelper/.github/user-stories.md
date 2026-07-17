# User Stories

## Feature: Validating Study Material
**Branch:** `feature/validating-study-material`

---

## Summary

| Status | Count |
|--------|-------|
| Backlog | 4 |
| In Progress | 0 |
| In Review | 0 |
| QA | 0 |
| Done | 0 |
| Blocked | 0 |

---

## Stories

---

### US-001: Validate Terms and Definitions File Format on Upload

- **Description:** As a student, I want the app to validate the format of my uploaded Terms and Definitions file so that I am immediately told whether the file was read correctly and will work with the flashcards and quizzes.
- **Acceptance Criteria:**
  - [ ] When a `TermsAndDefinitions.md` file is uploaded, the content is parsed using the same logic as `MarkdownParserService` to count sections and term-definition pairs (`Term: Definition` lines under `##` headings).
  - [ ] On successful upload, a success banner is shown on the Manage Study Materials page containing:
	- The number of sections found (e.g., `3 sections`)
	- The total number of term-definition pairs found (e.g., `42 terms and definitions`)
	- Example: _"Upload successful. Found 3 sections and 42 terms and definitions."_
  - [ ] If zero sections are found, the upload is still accepted but the banner clearly warns: _"Upload accepted, but no sections (## headings) were found. Flashcards and quizzes may not work. Check your file format."_
  - [ ] If zero term-definition pairs are found, the upload is still accepted but the banner warns: _"Upload accepted, but no term-definition pairs were found. Ensure each entry is on its own line in the format `Term: Definition` under a `##` section heading."_
  - [ ] The existing generic success message `"Terms and definitions uploaded successfully!"` is replaced by this detailed message.
  - [ ] No change to the upload rejection logic — files that are already rejected (wrong extension, empty, malicious content) continue to be rejected.
- **Status:** Backlog
- **Blockers:** None
- **Last Updated:** 2025-07-14

---

### US-002: Validate Equations File Format on Upload

- **Description:** As a student, I want the app to validate the format of my uploaded Equations file so that I know how many equations were found and whether the file will work with equation flashcards and exercises.
- **Acceptance Criteria:**
  - [ ] When an `Equations.md` file is uploaded, the content is parsed using the same logic as `EquationFlashcardParserService` to count successfully parsed equations (entries with a name, summary, and a valid `left = right` equation line).
  - [ ] On successful upload, a success banner is shown on the Manage Study Materials page containing the count of equations found. Example: _"Upload successful. Found 12 equations."_
  - [ ] If zero equations are parsed, the upload is still accepted but the banner clearly warns: _"Upload accepted, but no equations were found. Ensure each equation block has a name (## heading), a summary line, and an equation in the format `Left = Right`."_
  - [ ] The existing generic success message `"Equations uploaded successfully!"` is replaced by this detailed message.
  - [ ] No change to the upload rejection logic — files that are already rejected continue to be rejected.
- **Status:** Backlog
- **Blockers:** None
- **Last Updated:** 2025-07-14

---

### US-003: Report Specific Format Errors in the Terms and Definitions File

- **Description:** As a student, I want to see specific, line-level error details when my Terms and Definitions file has format problems so that I can fix my file and re-upload it successfully.
- **Acceptance Criteria:**
  - [ ] `FileValidationService.ValidateTermsFormatAsync` is enhanced to perform per-line format checking and return actionable errors, including:
	- Lines that appear to be term-definition pairs but are missing a colon separator (e.g., `"Line 14: 'Photosynthesis - The process...' appears to be a term but is missing a colon (:) separator."`)
	- Lines under a `##` heading that are not a term-definition pair, bullet point, or blank line (e.g., `"Line 22: Unexpected content found — only term-definition pairs (Term: Definition) and bullet points are expected under a section heading."`)
	- A missing or malformed `##` section heading before the first term (e.g., `"Line 3: Term-definition pair found before any section heading (##). All terms must be under a ## heading."`)
  - [ ] When format errors are found, the file is **rejected** (not saved) and the error banner on the Manage Study Materials page lists every specific error message, one per line.
  - [ ] The error banner title reads: _"Upload failed. The following format errors were found:"_ followed by the list.
  - [ ] If more than 10 errors are found, only the first 10 are shown followed by _"...and N more errors. Please review the full file."_
  - [ ] Format warnings (e.g., zero terms found) do **not** cause rejection — they are shown as a warning alongside the success message.
  - [ ] `FileValidationResult` is extended with a `ParsedSectionCount` and `ParsedTermCount` integer property so counts are available to the controller without re-parsing.
- **Status:** Backlog
- **Blockers:** None
- **Last Updated:** 2025-07-14

---

### US-004: Report Specific Format Errors in the Equations File

- **Description:** As a student, I want to see specific, line-level error details when my Equations file has format problems so that I can fix my file and re-upload it successfully.
- **Acceptance Criteria:**
  - [ ] `FileValidationService.ValidateEquationsFormatAsync` is enhanced to perform per-block format checking and return actionable errors, including:
	- An equation block that has a `##` heading but is missing a summary line (e.g., `"Equation 'Ohm's Law' (line 5): Missing summary line. Add a plain-text description line after the heading."`)
	- An equation block that has a heading and summary but is missing an equation line (e.g., `"Equation 'Ohm's Law' (line 5): No equation found. Add a line in the format 'Left = Right' after the summary."`)
	- An equation line that does not contain an `=` sign (e.g., `"Line 12: Equation line 'V IR' is missing an equals sign (=)."`)
	- An equation block where the left or right side of `=` is blank (e.g., `"Line 12: Equation 'V = ' has a blank right-hand side."`)
  - [ ] When format errors are found, the file is **rejected** (not saved) and the error banner on the Manage Study Materials page lists every specific error, one per line.
  - [ ] The error banner title reads: _"Upload failed. The following format errors were found:"_ followed by the list.
  - [ ] If more than 10 errors are found, only the first 10 are shown followed by _"...and N more errors. Please review the full file."_
  - [ ] `FileValidationResult` is extended with a `ParsedEquationCount` integer property so the count is available to the controller without re-parsing.
  - [ ] Format warnings do **not** cause rejection.
- **Status:** Backlog
- **Blockers:** None
- **Last Updated:** 2025-07-14

---
