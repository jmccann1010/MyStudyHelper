# Backend Implementation Complete: Super Quiz Question Count Selection

**Feature Branch:** `feature/super-quiz-select-number-of-questions`  
**Completion Date:** 2025-01-22  
**Status:** ✅ Backend Complete - Ready for Testing

---

## Summary

The backend implementation for user-selectable Super Quiz question counts is complete and compiling successfully. Users can now choose between:

- **10 Questions** (Quick Practice)
- **Half** (Moderate Practice - dynamically calculated)
- **All** (Complete Mastery - existing behavior)

---

## Files Created

### 1. `Models/SuperQuizQuestionCountOption.cs`
**Purpose:** Enum defining the three selectable question count options.

**Key Members:**
- `Fixed10 = 0` - Exactly 10 questions (default)
- `Half = 1` - Half of available terms (rounded down, minimum 4)
- `All = 2` - All available terms (existing behavior)

**Design Notes:**
- Default value is `Fixed10` for new sessions
- Backward compatibility preserved via service default parameter

---

## Files Modified

### 2. `ViewModels/SuperQuizStartViewModel.cs`
**Previous Behavior:** Simple model with `TotalQuestions` and `EstimatedTimeMinutes` properties.

**New Behavior:** Selection-aware model that computes question counts and estimated time based on user selection.

**Key Changes:**
- Added `TotalAvailableTerms` - Total number of terms/definitions available
- Added `SelectedOption` - User's selected question count option (defaults to `Fixed10`)
- Added computed properties:
  - `Fixed10Count` - Always returns 10
  - `HalfCount` - Returns `TotalAvailableTerms / 2` (integer division)
  - `AllCount` - Returns `TotalAvailableTerms`
- Added `GetSelectedQuestionCount()` - Returns count based on selected option
- Updated `EstimatedTimeMinutes` - Calculates 0.25 minutes per question (15 seconds)
- Retained `EstimatedTimeFormatted` - Formats as "X minutes" or "X.X hours"

**Backward Compatibility:** None required - view model is consumed only by controller and view.

---

### 3. `Services/ISuperQuizService.cs`
**Previous Signature:**
```csharp
Task<string> StartSuperQuizAsync(string username);
```

**New Signature:**
```csharp
Task<string> StartSuperQuizAsync(
	string username,
	SuperQuizQuestionCountOption questionCountOption = SuperQuizQuestionCountOption.All);
```

**Key Changes:**
- Added optional `questionCountOption` parameter with default value `All`
- Updated XML documentation to reflect new behavior
- Added exception documentation for invalid option scenarios

**Backward Compatibility:** ✅ Existing callers without the parameter will default to `All` (previous behavior).

---

### 4. `Services/SuperQuizService.cs`
**Previous Behavior:** Always generated questions from all available terms/definitions.

**New Behavior:** Calculates target question count based on selected option, randomly selects sections when limiting, generates questions, and creates session.

**Key Changes:**

#### Method Signature Update
- Added `questionCountOption` parameter to `StartSuperQuizAsync` with default value `All`

#### New Helper Method
```csharp
private int CalculateTargetQuestionCount(int totalAvailable, SuperQuizQuestionCountOption option)
```
- Returns:
  - `Fixed10` → 10
  - `Half` → `totalAvailable / 2` (integer division rounds down)
  - `All` → `totalAvailable`
  - Default fallback → `totalAvailable`

#### Enhanced Session Creation Logic
1. **Parse markdown files** - Get all available sections
2. **Validate minimum content** - Require at least 4 terms
3. **Calculate target count** - Use helper to determine question count
4. **Validate target is achievable** - Ensure enough terms exist
5. **Validate minimum requirement** - Target must be >= 4 questions
6. **Random section selection** - When limiting (Fixed10/Half):
   - Shuffle all sections using `OrderBy(_ => Guid.NewGuid())`
   - Take target count
   - Log selection details
7. **Enforce maximum limit** - Cap at 500 questions (existing constraint)
8. **Generate questions** - Create quiz questions from selected sections
9. **Create and cache session** - Standard session initialization
10. **Enhanced logging** - Log selected option throughout

**Validation Rules:**
- Minimum 4 terms required (existing rule)
- Target count must be achievable with available terms
- Target count must meet minimum requirement (4 questions)
- Maximum 500 questions enforced (existing limit)

**Error Messages:**
- "Cannot generate {count} questions. Only {available} terms available."
- "At least 4 questions required. Selected option would generate {count} questions."
- "At least 4 terms required for Super Quiz. You currently have {count} term(s)."
- "Failed to generate sufficient questions. Only {count} questions could be created."

**Backward Compatibility:** ✅ Default parameter ensures existing behavior when option not specified.

---

### 5. `Controllers/SuperQuizController.cs`
**Previous Behavior:** GET displayed static preview; POST started session with all questions.

**New Behavior:** GET builds selection model; POST accepts selected option and starts session.

#### GET `/SuperQuiz/Start` Changes
**Before:**
```csharp
var viewModel = new SuperQuizStartViewModel
{
	TotalQuestions = sections.Count,
	EstimatedTimeMinutes = sections.Count * 0.5
};
```

**After:**
```csharp
var viewModel = new SuperQuizStartViewModel
{
	TotalAvailableTerms = sections.Count,
	SelectedOption = SuperQuizQuestionCountOption.Fixed10
};
```

**Key Changes:**
- Sets `TotalAvailableTerms` instead of `TotalQuestions`
- Sets default `SelectedOption` to `Fixed10`
- View model automatically computes counts and estimated time

#### POST `/SuperQuiz/Start` Changes
**Before:**
```csharp
public async Task<IActionResult> Start([FromForm] bool confirmed)
{
	var sessionId = await _superQuizService.StartSuperQuizAsync(username);
	// ...
}
```

**After:**
```csharp
public async Task<IActionResult> Start([FromForm] SuperQuizQuestionCountOption selectedOption)
{
	var sessionId = await _superQuizService.StartSuperQuizAsync(username, selectedOption);
	// ...
}
```

**Key Changes:**
- Removed `bool confirmed` parameter (no longer needed)
- Added `SuperQuizQuestionCountOption selectedOption` parameter
- Passes selected option to service method
- Enhanced logging includes selected option

**Model Binding:** ASP.NET Core automatically binds the radio button value (0/1/2) to the enum parameter.

**Backward Compatibility:** None required - controller actions are not public APIs.

---

### 6. `Views/SuperQuiz/Start.cshtml`
**Previous Behavior:** Static preview with total questions and estimated time; hidden "confirmed" field.

**New Behavior:** Radio buttons for selection; dynamic preview updates via JavaScript.

**Key Changes:**

#### Added Question Count Selection UI
```razor
<div class="form-check mb-2">
	<input class="form-check-input" type="radio" name="selectedOption" 
		   id="option-fixed10" value="0" checked data-count="@Model.Fixed10Count" />
	<label class="form-check-label" for="option-fixed10">
		<strong>10 Questions</strong> (Quick Practice)
	</label>
</div>
<div class="form-check mb-2">
	<input class="form-check-input" type="radio" name="selectedOption" 
		   id="option-half" value="1" data-count="@Model.HalfCount" />
	<label class="form-check-label" for="option-half">
		<strong>Half (@Model.HalfCount Questions)</strong> (Moderate Practice)
	</label>
</div>
<div class="form-check mb-2">
	<input class="form-check-input" type="radio" name="selectedOption" 
		   id="option-all" value="2" data-count="@Model.AllCount" />
	<label class="form-check-label" for="option-all">
		<strong>All (@Model.AllCount Questions)</strong> (Complete Mastery)
	</label>
</div>
```

**Radio Button Attributes:**
- `name="selectedOption"` - Binds to controller parameter
- `value="0"` / `value="1"` / `value="2"` - Maps to enum integer values
- `data-count` - Stores question count for JavaScript preview updates
- `checked` - Default selection is Fixed10 (10 questions)

#### Updated Preview Cards
**Before:**
```razor
<h3 class="text-primary">@Model.TotalQuestions</h3>
<h3 class="text-primary">@Model.EstimatedTimeFormatted</h3>
```

**After:**
```razor
<h3 class="text-primary" id="preview-count">@Model.Fixed10Count</h3>
<h3 class="text-primary" id="preview-time">@Model.EstimatedTimeFormatted</h3>
```

**Key Changes:**
- Added `id` attributes for JavaScript targeting
- Initial values reflect default selection (Fixed10)

#### Added Dynamic Preview JavaScript
```javascript
document.addEventListener('DOMContentLoaded', function () {
	const radioButtons = document.querySelectorAll('input[name="selectedOption"]');
	const previewCount = document.getElementById('preview-count');
	const previewTime = document.getElementById('preview-time');

	function updatePreview() {
		const selectedRadio = document.querySelector('input[name="selectedOption"]:checked');
		if (selectedRadio) {
			const count = parseInt(selectedRadio.dataset.count);
			const timeMinutes = count * 0.25; // 15 seconds per question

			previewCount.textContent = count;

			if (timeMinutes < 60) {
				previewTime.textContent = Math.round(timeMinutes) + ' minutes';
			} else {
				previewTime.textContent = (timeMinutes / 60).toFixed(1) + ' hours';
			}
		}
	}

	radioButtons.forEach(radio => {
		radio.addEventListener('change', updatePreview);
	});

	updatePreview(); // Initial update on page load
});
```

**JavaScript Behavior:**
- Listens for radio button changes
- Reads `data-count` from selected radio
- Calculates estimated time (0.25 minutes per question)
- Updates preview cards immediately (no page reload)
- Formats time as "X minutes" or "X.X hours"
- Runs on page load to set initial values

#### Updated Form Submission
**Before:**
```razor
<input type="hidden" name="confirmed" value="true" />
```

**After:**
```razor
<!-- Radio buttons POST directly as 'selectedOption' -->
```

**Key Changes:**
- Removed hidden `confirmed` field
- Radio button selection posts as `selectedOption` parameter
- ASP.NET Core model binding converts value (0/1/2) to enum

#### Updated Instructions Text
**Before:**
- "Answer questions from **all** your study materials"

**After:**
- "Choose how many questions you want to practice"

**Accessibility:**
- Standard HTML radio buttons (keyboard navigable)
- Proper `<label for="...">` associations
- Screen reader friendly form controls

**Backward Compatibility:** None required - view is not a public API.

---

## Technical Decisions

### 1. Random Section Selection Strategy
**Decision:** Random selection happens **before** question generation.

**Rationale:**
- More efficient than generating all questions then trimming
- Ensures true random sampling across all available content
- Reduces memory footprint for large study material sets

**Implementation:**
```csharp
selectedSections = sections
	.OrderBy(_ => Guid.NewGuid())
	.Take(targetQuestionCount)
	.ToList();
```

**Trade-offs:**
- `Guid.NewGuid()` is not cryptographically secure randomness (acceptable for this use case)
- Each Super Quiz run with same option may produce different questions (desired behavior)

---

### 2. Integer Division for Half Option
**Decision:** Use integer division for calculating half: `totalAvailable / 2`.

**Rationale:**
- Rounds down automatically (15 terms → 7 questions)
- Simple, predictable behavior
- No floating-point precision issues

**Edge Cases:**
- 1 term → 0 questions (caught by validation: "At least 4 questions required")
- 9 terms → 4 questions (meets minimum requirement)
- 500+ terms → capped at 250 for Half, 500 for All (maximum limit enforcement)

---

### 3. Default Selection: Fixed10
**Decision:** Default to `Fixed10` in view model and UI, but `All` in service interface.

**Rationale:**
- **UI Default (`Fixed10`):** Better user experience for new users; quick practice mode encourages engagement
- **Service Default (`All`):** Preserves existing behavior for any code paths that call service directly without specifying option
- Decouples UI preference from API contract

**Backward Compatibility Impact:**
- Existing direct service callers (if any) maintain "All" behavior
- New UI users start with "10 questions" experience

---

### 4. JavaScript-Based Dynamic Preview
**Decision:** Use vanilla JavaScript (no framework) for live preview updates.

**Rationale:**
- No additional dependencies required
- Simple event-driven update logic
- Works in all modern browsers
- Progressively enhances the form (works without JavaScript, just no live preview)

**Implementation Note:**
- Time calculation duplicates server-side logic (0.25 minutes per question)
- Considered acceptable trade-off for better UX (no round-trip to server)

---

## Validation & Error Handling

### Service-Level Validation
1. **Username validation** - Must be non-empty
2. **Study materials exist** - At least one markdown file
3. **Minimum content** - At least 4 terms required
4. **Target achievability** - Selected option must not exceed available terms
5. **Minimum questions** - Target must be >= 4
6. **Maximum limit** - Enforces 500 question cap (existing constraint)
7. **Question generation** - Handles individual section failures gracefully

### Controller-Level Error Handling
- `InvalidOperationException` → User-friendly error via `TempData`, redirect to Start
- Generic exceptions → Error view with trace identifier
- Authentication failures → Redirect to login

### User-Facing Error Messages
| Scenario | Message |
|---|---|
| No study materials | "No study materials found. Please upload study materials first." |
| < 4 terms total | "At least 4 terms required for Super Quiz. You currently have X term(s). Please add more study materials." |
| Target > available | "Cannot generate X questions. Only Y terms available." |
| Target < 4 | "At least 4 questions required. Selected option would generate X questions." |
| Question generation failure | "Failed to generate sufficient questions. Only X questions could be created." |

---

## Testing Strategy

### Unit Tests Required (User Story #5021)
**File:** `FileConverterTests/Services/SuperQuizService_StartSuperQuizAsync_Tests.cs`

**Test Cases:**
1. ✅ **Fixed10 option with 20 available terms**
   - Verify exactly 10 questions generated
   - Verify session created successfully
   - Verify logging includes option

2. ✅ **Half option with 20 available terms**
   - Verify exactly 10 questions generated (20 / 2)
   - Verify random selection (run multiple times, verify different questions)

3. ✅ **All option with 15 available terms**
   - Verify all 15 questions generated
   - Verify backward compatibility (existing behavior)

4. ✅ **Half option with 9 terms → 4 questions**
   - Verify 4 questions generated (edge case: rounds down, meets minimum)

5. ✅ **Fixed10 option with 5 terms → validation error**
   - Verify `InvalidOperationException` thrown
   - Verify error message: "Cannot generate 10 questions. Only 5 terms available."

6. ✅ **Half option with 3 terms → validation error**
   - Verify `InvalidOperationException` thrown
   - Verify error message: "At least 4 questions required. Selected option would generate 1 questions."

7. ✅ **All option with 600 terms → capped at 500**
   - Verify maximum limit enforcement (existing behavior, verify still works)

8. ✅ **Service called without option parameter**
   - Verify defaults to `All` (backward compatibility)
   - Verify all available questions generated

**Testing Approach:**
- Use `environment = "dev"` to prevent real DB writes
- Mock `IMarkdownParserService` to return controlled term counts
- Mock `IQuestionGeneratorService` to return predictable questions
- Use `IMemoryCache` test double (or real in-memory instance)
- Verify logging via `ILogger<T>` mock

---

### Integration Tests Required (User Story #5022)
**File:** `FileConverterTests/Controllers/SuperQuizController_Start_Tests.cs`

**Test Cases:**
1. ✅ **GET Start - 20 available terms**
   - Verify `TotalAvailableTerms = 20`
   - Verify `Fixed10Count = 10`
   - Verify `HalfCount = 10`
   - Verify `AllCount = 20`
   - Verify `SelectedOption = Fixed10` (default)

2. ✅ **POST Start - Fixed10 selected**
   - Verify controller calls `StartSuperQuizAsync(username, Fixed10)`
   - Verify redirect to `Question` action with sessionId

3. ✅ **POST Start - Half selected**
   - Verify controller calls `StartSuperQuizAsync(username, Half)`
   - Verify session created with correct question count

4. ✅ **POST Start - All selected**
   - Verify controller calls `StartSuperQuizAsync(username, All)`
   - Verify backward-compatible behavior

5. ✅ **POST Start - insufficient terms error**
   - Verify `TempData["ErrorMessage"]` populated
   - Verify redirect back to Start page

6. ✅ **Model binding - radio value to enum**
   - Verify `value="0"` → `SuperQuizQuestionCountOption.Fixed10`
   - Verify `value="1"` → `SuperQuizQuestionCountOption.Half`
   - Verify `value="2"` → `SuperQuizQuestionCountOption.All`

**Testing Approach:**
- Use `WebApplicationFactory<Program>` for integration tests
- Mock authentication to provide test username
- Seed test study materials with known term counts
- Verify service method calls via mock/spy
- Verify view model properties in GET response
- Verify form submission and redirect in POST

---

### Manual UI Tests Required (User Story #5023)
**Checklist:**

#### Visual Verification
- [ ] Radio buttons render correctly on desktop (Chrome, Edge, Firefox)
- [ ] Radio buttons render correctly on mobile (responsive layout)
- [ ] Preview cards display initial values (10 questions, X minutes)
- [ ] Labels show dynamic question counts (e.g., "Half (15 Questions)")
- [ ] Bootstrap styling matches existing theme
- [ ] Dark theme compatibility (text colors, contrast)

#### Interaction Testing
- [ ] Clicking Fixed10 radio updates preview to 10 questions
- [ ] Clicking Half radio updates preview to half count
- [ ] Clicking All radio updates preview to all count
- [ ] Preview time updates correctly (< 60 minutes: "X minutes", >= 60: "X.X hours")
- [ ] Keyboard navigation works (Tab, Space, Arrow keys)
- [ ] Screen reader announces radio labels correctly

#### Form Submission
- [ ] Submitting with Fixed10 selected starts quiz with 10 questions
- [ ] Submitting with Half selected starts quiz with half count
- [ ] Submitting with All selected starts quiz with all questions
- [ ] Error messages display correctly (insufficient terms, etc.)
- [ ] Validation errors redirect back to Start page with message
- [ ] Anti-forgery token validation works

#### Edge Cases
- [ ] Exactly 4 terms available (minimum): all options work
- [ ] 5-9 terms: Fixed10 disabled/error, Half works, All works
- [ ] 10 terms: all options work
- [ ] 500+ terms: capped at 500 for All, 250 for Half

#### Backward Compatibility
- [ ] Existing Super Quiz sessions (if any) continue working
- [ ] Direct service calls without option default to All
- [ ] Existing links to `/SuperQuiz/Start` still work

---

## Build Verification

✅ **Build Status:** Successful  
✅ **Compilation Errors:** None  
✅ **Warnings:** None  

**Command Used:**
```powershell
msbuild /t:Build
```

**Verified Files Compile:**
- `Models/SuperQuizQuestionCountOption.cs`
- `ViewModels/SuperQuizStartViewModel.cs`
- `Services/ISuperQuizService.cs`
- `Services/SuperQuizService.cs`
- `Controllers/SuperQuizController.cs`
- `Views/SuperQuiz/Start.cshtml`

---

## Next Steps

### 1. ✅ Backend Implementation (Current)
**Status:** Complete  
**Deliverables:**
- New enum, view model, service logic, controller updates, view updates
- All files compiling successfully
- Build verification passed

---

### 2. ⏳ Unit Tests (Next - User Story #5021)
**Owner:** QA Engineer  
**Tasks:**
- Create `SuperQuizService_StartSuperQuizAsync_Tests.cs`
- Implement 8 required test cases
- Verify >= 80% code coverage for new/modified code
- Use `environment = "dev"` to prevent real DB writes

**Acceptance Criteria:**
- All test cases pass
- Edge cases validated (minimum terms, validation errors, caps)
- Backward compatibility verified (default parameter behavior)

---

### 3. ⏳ Integration Tests (User Story #5022)
**Owner:** QA Engineer  
**Tasks:**
- Create `SuperQuizController_Start_Tests.cs`
- Implement 6 required test cases
- Verify controller-service integration
- Verify model binding (radio values → enum)

**Acceptance Criteria:**
- All test cases pass
- GET and POST actions verified
- Error handling paths tested

---

### 4. ⏳ Manual UI Testing (User Story #5023)
**Owner:** QA Engineer  
**Tasks:**
- Complete manual UI testing checklist (see above)
- Test on multiple browsers (Chrome, Edge, Firefox)
- Test responsive behavior (desktop, tablet, mobile)
- Verify accessibility (keyboard navigation, screen readers)
- Validate dark theme compatibility

**Acceptance Criteria:**
- All checklist items verified
- No UI regressions found
- Accessibility standards met

---

### 5. ⏳ Documentation Updates (User Story #5024)
**Owner:** Technical Writer  
**Tasks:**
- Update `Views/Help/SuperQuiz.cshtml` with new selection feature
- Add screenshots showing radio button options
- Update step-by-step instructions
- Document question count behavior (10 / Half / All)
- Clarify random selection for Fixed10 and Half

**Acceptance Criteria:**
- Help page reflects new feature
- Instructions are clear and accurate
- Screenshots match current UI

---

### 6. ⏳ Final Review & PR
**Owner:** Code Review Specialist, Security Specialist  
**Tasks:**
- Code review for quality, maintainability, security
- Security review for input validation, error handling
- Human approval/denial for each finding
- Address any approved changes

**Acceptance Criteria:**
- All review findings addressed or accepted
- Human approval obtained
- Ready for merge to `main`

---

## Backward Compatibility Summary

| Component | Strategy | Impact |
|---|---|---|
| `ISuperQuizService` | Default parameter `= SuperQuizQuestionCountOption.All` | ✅ Existing callers work unchanged |
| `SuperQuizService` | Default parameter + existing behavior for `All` | ✅ No breaking changes |
| `SuperQuizController` | New form binding, no external API | ✅ Internal change only |
| `SuperQuizStartViewModel` | Complete replacement, controller-specific | ✅ Not a public API |
| `Start.cshtml` | UI redesign, user-facing only | ✅ No API impact |

**Risk Assessment:** 🟢 **LOW RISK**  
- Service interface changes are additive (optional parameter)
- Controller and view changes are internal (not public APIs)
- Default behavior preserves existing "All questions" mode
- No database schema changes required
- No breaking changes to existing sessions (session model unchanged)

---

## Known Limitations & Future Enhancements

### Current Limitations
1. **Custom question count not supported** - User can only choose 10 / Half / All
2. **No "remember my preference"** - Selection resets to Fixed10 each visit
3. **Random selection is uniform** - No weighting by difficulty, recency, or mastery
4. **Half always rounds down** - 15 terms → 7 questions (not 8)

### Potential Future Enhancements (Out of Scope)
- Custom number input (e.g., "25 questions")
- User preference persistence (cookie or database)
- Smart selection algorithms (prioritize weak areas)
- Question pool rotation (avoid repeating same 10 questions)
- Time-based quiz mode (e.g., "5 minutes of practice")

---

## Conclusion

✅ **Backend implementation for Super Quiz question count selection is complete and ready for testing.**

All required code changes have been implemented, compiled successfully, and are ready for QA validation. The feature preserves backward compatibility while introducing flexible quiz length options for improved user experience.

**Build Status:** ✅ Successful  
**Compilation Status:** ✅ No errors or warnings  
**Backward Compatibility:** ✅ Preserved via default parameters  
**Next Phase:** QA Engineer - Unit and Integration Tests

---

**Implementation Completed By:** GitHub Copilot Backend Development Engineer  
**Document Created:** 2025-01-22  
**Last Updated:** 2025-01-22
