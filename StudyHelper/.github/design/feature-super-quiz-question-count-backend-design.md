# Super Quiz Question Count Selection - Backend Design

## Overview

This document details the backend architecture for allowing users to select the number of questions in a Super Quiz session. The design maintains backward compatibility while adding new functionality for question count selection.

---

## Architecture Summary

### Key Changes

1. **New Enum:** `SuperQuizQuestionCountOption` to represent selection choices
2. **Enhanced Service Interface:** `ISuperQuizService.StartSuperQuizAsync` accepts optional parameter
3. **Updated Service Logic:** `SuperQuizService` handles question limiting and random selection
4. **Enhanced ViewModel:** `SuperQuizStartViewModel` includes available options and counts

---

## Data Models

### 1. New Enum: SuperQuizQuestionCountOption

**Location:** `Models/SuperQuizQuestionCountOption.cs`

```csharp
namespace StudyHelper.Models;

/// <summary>
/// Represents the user's question count selection for Super Quiz.
/// </summary>
public enum SuperQuizQuestionCountOption
{
	/// <summary>
	/// Exactly 10 questions (default).
	/// Randomly selected from available terms.
	/// </summary>
	Fixed10 = 0,

	/// <summary>
	/// Half of available terms (rounded down).
	/// Minimum 4 questions.
	/// </summary>
	Half = 1,

	/// <summary>
	/// All available terms.
	/// Existing Super Quiz behavior.
	/// </summary>
	All = 2
}
```

**Design Rationale:**
- Enum provides type safety and clear intent
- Default value `Fixed10` ensures existing behavior if not specified
- Values map cleanly to radio button values in HTML form

---

### 2. Enhanced SuperQuizStartViewModel

**Location:** `ViewModels/SuperQuizStartViewModel.cs`

```csharp
namespace StudyHelper.ViewModels;

public class SuperQuizStartViewModel
{
	/// <summary>
	/// Total number of available terms/definitions.
	/// Used to calculate Half and All options.
	/// </summary>
	public int TotalAvailableTerms { get; set; }

	/// <summary>
	/// User's selected question count option (default: Fixed10).
	/// </summary>
	public SuperQuizQuestionCountOption SelectedOption { get; set; } = SuperQuizQuestionCountOption.Fixed10;

	/// <summary>
	/// Number of questions for the Fixed10 option.
	/// </summary>
	public int Fixed10Count => 10;

	/// <summary>
	/// Number of questions for the Half option.
	/// Calculated as TotalAvailableTerms / 2 (integer division).
	/// </summary>
	public int HalfCount => TotalAvailableTerms / 2;

	/// <summary>
	/// Number of questions for the All option.
	/// </summary>
	public int AllCount => TotalAvailableTerms;

	/// <summary>
	/// Gets the question count based on the selected option.
	/// </summary>
	public int GetSelectedQuestionCount()
	{
		return SelectedOption switch
		{
			SuperQuizQuestionCountOption.Fixed10 => Fixed10Count,
			SuperQuizQuestionCountOption.Half => HalfCount,
			SuperQuizQuestionCountOption.All => AllCount,
			_ => Fixed10Count
		};
	}

	/// <summary>
	/// Estimated time in minutes based on selected option.
	/// Calculation: 15 seconds per question = 0.25 minutes per question.
	/// </summary>
	public double EstimatedTimeMinutes => GetSelectedQuestionCount() * 0.25;

	/// <summary>
	/// Formatted estimated time string.
	/// </summary>
	public string EstimatedTimeFormatted =>
		EstimatedTimeMinutes < 60
			? $"{EstimatedTimeMinutes:F0} minutes"
			: $"{EstimatedTimeMinutes / 60:F1} hours";
}
```

**Design Rationale:**
- Computed properties eliminate duplication and ensure consistency
- `GetSelectedQuestionCount()` centralizes question count logic
- `EstimatedTimeMinutes` dynamically calculates based on selection
- Backward compatible: existing time calculation preserved

---

## Service Layer Changes

### 3. Updated ISuperQuizService Interface

**Location:** `Services/ISuperQuizService.cs`

**Change:** Update `StartSuperQuizAsync` signature

```csharp
/// <summary>
/// Starts a new Super Quiz session with the specified question count option.
/// </summary>
/// <param name="username">Authenticated username for session scoping.</param>
/// <param name="questionCountOption">Question count selection (default: All for backward compatibility).</param>
/// <returns>Session ID for future references.</returns>
/// <exception cref="ArgumentException">Username is null or empty.</exception>
/// <exception cref="InvalidOperationException">
/// No study materials found, insufficient content, or invalid option for available terms.
/// </exception>
Task<string> StartSuperQuizAsync(
	string username, 
	SuperQuizQuestionCountOption questionCountOption = SuperQuizQuestionCountOption.All);
```

**Backward Compatibility Note:**
- Default parameter value `SuperQuizQuestionCountOption.All` ensures existing callers continue to work
- If no parameter provided, behavior is identical to current implementation

---

### 4. Updated SuperQuizService Implementation

**Location:** `Services/SuperQuizService.cs`

**Method:** `StartSuperQuizAsync`

#### High-Level Flow

```
1. Validate username (existing)
2. Parse markdown files to get all available sections (existing)
3. Validate minimum content (existing)
4. **NEW:** Calculate target question count based on questionCountOption
5. **NEW:** Validate that target count is achievable
6. **NEW:** Randomly select sections if count < total available
7. Generate questions for selected sections (existing logic)
8. Create and store session (existing)
9. Return session ID (existing)
```

#### Detailed Implementation

```csharp
public async Task<string> StartSuperQuizAsync(
	string username, 
	SuperQuizQuestionCountOption questionCountOption = SuperQuizQuestionCountOption.All)
{
	// Step 1: Validate username (EXISTING)
	if (string.IsNullOrEmpty(username))
	{
		throw new ArgumentException("Username cannot be null or empty.", nameof(username));
	}

	_logger.LogInformation(
		"Starting Super Quiz session for user {Username} with option {Option}", 
		username, 
		questionCountOption);

	// Step 2: Parse markdown files (EXISTING)
	var sections = await _markdownParserService.ParseMarkdownFilesAsync(username);

	// Step 3: Validate minimum content (EXISTING)
	if (sections.Count == 0)
	{
		_logger.LogWarning("No study materials found for user {Username}", username);
		throw new InvalidOperationException(
			"No study materials found. Please upload study materials first.");
	}

	if (sections.Count < MinQuestionsRequired)
	{
		_logger.LogWarning(
			"Insufficient content for user {Username}: {Count} sections", 
			username, 
			sections.Count);
		throw new InvalidOperationException(
			$"At least {MinQuestionsRequired} terms required for Super Quiz. " +
			$"You currently have {sections.Count} term(s). Please add more study materials.");
	}

	// Step 4: Calculate target question count (NEW)
	int targetQuestionCount = CalculateTargetQuestionCount(sections.Count, questionCountOption);

	// Step 5: Validate target count is achievable (NEW)
	if (targetQuestionCount > sections.Count)
	{
		throw new InvalidOperationException(
			$"Cannot generate {targetQuestionCount} questions. " +
			$"Only {sections.Count} terms available.");
	}

	if (targetQuestionCount < MinQuestionsRequired)
	{
		throw new InvalidOperationException(
			$"At least {MinQuestionsRequired} questions required. " +
			$"Selected option would generate {targetQuestionCount} questions.");
	}

	// Step 6: Randomly select sections if limiting (NEW)
	List<MarkdownSection> selectedSections;
	if (targetQuestionCount < sections.Count)
	{
		selectedSections = sections
			.OrderBy(_ => Guid.NewGuid()) // Random shuffle
			.Take(targetQuestionCount)
			.ToList();

		_logger.LogInformation(
			"Randomly selected {Selected} sections from {Total} available for user {Username}",
			targetQuestionCount,
			sections.Count,
			username);
	}
	else
	{
		selectedSections = sections;
	}

	// Step 7: Enforce maximum limit (EXISTING, moved after selection)
	if (selectedSections.Count > MaxQuestionsLimit)
	{
		_logger.LogWarning(
			"Question count {Count} exceeds limit {Limit}, truncating for user {Username}",
			selectedSections.Count,
			MaxQuestionsLimit,
			username);
		selectedSections = selectedSections.Take(MaxQuestionsLimit).ToList();
	}

	// Step 8: Generate questions (EXISTING LOGIC)
	var allQuestions = new List<QuizQuestion>();
	foreach (var section in selectedSections)
	{
		try
		{
			var question = _questionGeneratorService.GenerateQuestion(
				new List<MarkdownSection> { section });
			allQuestions.Add(question);
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex, 
				"Failed to generate question for section in module {Module}", 
				section.Module);
			// Continue with other sections
		}
	}

	// Step 9: Final validation (EXISTING)
	if (allQuestions.Count < MinQuestionsRequired)
	{
		throw new InvalidOperationException(
			$"Failed to generate sufficient questions. " +
			$"Only {allQuestions.Count} questions could be created.");
	}

	// Step 10: Create session (EXISTING)
	var session = new SuperQuizSession
	{
		SessionId = Guid.NewGuid().ToString(),
		Username = username,
		AllQuestions = allQuestions,
		CurrentRound = 1,
		CreatedAt = DateTime.UtcNow,
		LastActivityAt = DateTime.UtcNow
	};

	// Step 11: Randomize into queue (EXISTING)
	RandomizeQuestionsIntoQueue(session, allQuestions);

	// Step 12: Cache session (EXISTING)
	var cacheKey = GetCacheKey(session.SessionId);
	_memoryCache.Set(cacheKey, session, GetCacheOptions());

	_logger.LogInformation(
		"Super Quiz session {SessionId} started for user {Username} with {QuestionCount} questions (option: {Option})",
		session.SessionId,
		username,
		allQuestions.Count,
		questionCountOption);

	return session.SessionId;
}

/// <summary>
/// Calculates the target question count based on the user's selection.
/// </summary>
private int CalculateTargetQuestionCount(
	int totalAvailable, 
	SuperQuizQuestionCountOption option)
{
	return option switch
	{
		SuperQuizQuestionCountOption.Fixed10 => 10,
		SuperQuizQuestionCountOption.Half => totalAvailable / 2, // Integer division
		SuperQuizQuestionCountOption.All => totalAvailable,
		_ => totalAvailable // Fallback to All
	};
}
```

**Design Rationale:**
- Minimal changes to existing flow
- Random selection uses `OrderBy(_ => Guid.NewGuid())` for simplicity and performance
- Validation ensures user experience is clear (early failure with helpful messages)
- Logging captures option used for debugging and analytics
- Helper method `CalculateTargetQuestionCount` keeps logic centralized and testable

---

## Controller Layer Changes

### 5. Updated SuperQuizController

**Location:** `Controllers/SuperQuizController.cs`

#### GET /SuperQuiz/Start

**Purpose:** Display start page with question count options

```csharp
[HttpGet]
public async Task<IActionResult> Start()
{
	try
	{
		var username = User.Identity?.Name;
		if (string.IsNullOrEmpty(username))
		{
			return RedirectToAction("Login", "Account");
		}

		// Get available terms count
		var sections = await _markdownParserService.ParseMarkdownFilesAsync(username);

		if (sections.Count < 4)
		{
			ViewBag.ErrorMessage = 
				$"At least 4 terms required for Super Quiz. " +
				$"You currently have {sections.Count} term(s). " +
				$"Please add more study materials.";
			return View("InsufficientContent");
		}

		// Build view model with all options
		var viewModel = new SuperQuizStartViewModel
		{
			TotalAvailableTerms = sections.Count,
			SelectedOption = SuperQuizQuestionCountOption.Fixed10 // Default
		};

		return View(viewModel);
	}
	catch (FileNotFoundException)
	{
		ViewBag.ErrorMessage = 
			"No study materials found. Please upload study materials first.";
		return View("NoStudyMaterials");
	}
	catch (Exception ex)
	{
		_logger.LogError(
			ex, 
			"Error loading Super Quiz start page for user {Username}", 
			User.Identity?.Name);
		return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
	}
}
```

#### POST /SuperQuiz/Start

**Purpose:** Create session with selected question count option

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Start(SuperQuizStartViewModel model)
{
	try
	{
		var username = User.Identity?.Name;
		if (string.IsNullOrEmpty(username))
		{
			return RedirectToAction("Login", "Account");
		}

		// Validate model
		if (!ModelState.IsValid)
		{
			// Reload available terms for display
			var sections = await _markdownParserService.ParseMarkdownFilesAsync(username);
			model.TotalAvailableTerms = sections.Count;
			return View(model);
		}

		// Validate Fixed10 option has enough terms
		if (model.SelectedOption == SuperQuizQuestionCountOption.Fixed10)
		{
			var sections = await _markdownParserService.ParseMarkdownFilesAsync(username);
			if (sections.Count < 10)
			{
				ModelState.AddModelError(
					string.Empty, 
					$"At least 10 terms required for '10 Questions' option. " +
					$"You currently have {sections.Count} term(s). " +
					$"Please select a different option or add more study materials.");
				model.TotalAvailableTerms = sections.Count;
				return View(model);
			}
		}

		// Start session with selected option
		var sessionId = await _superQuizService.StartSuperQuizAsync(
			username, 
			model.SelectedOption);

		_logger.LogInformation(
			"Super Quiz session {SessionId} started for user {Username} with option {Option}",
			sessionId,
			username,
			model.SelectedOption);

		return RedirectToAction(nameof(Question), new { sessionId });
	}
	catch (InvalidOperationException ex)
	{
		_logger.LogWarning(
			ex, 
			"Failed to start Super Quiz for user {Username} with option {Option}",
			User.Identity?.Name,
			model.SelectedOption);

		ModelState.AddModelError(string.Empty, ex.Message);

		// Reload available terms for display
		try
		{
			var sections = await _markdownParserService.ParseMarkdownFilesAsync(
				User.Identity?.Name ?? string.Empty);
			model.TotalAvailableTerms = sections.Count;
		}
		catch
		{
			model.TotalAvailableTerms = 0;
		}

		return View(model);
	}
	catch (Exception ex)
	{
		_logger.LogError(
			ex, 
			"Error starting Super Quiz for user {Username}",
			User.Identity?.Name);
		return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
	}
}
```

**Design Rationale:**
- POST action receives full view model with user's selection
- Early validation provides clear error messages
- Model state errors preserve user's selection on validation failure
- Service layer handles actual question limiting logic

---

## Validation Rules

### Business Rules

| Scenario | Rule | Error Message |
|----------|------|---------------|
| No study materials | Cannot start any Super Quiz | "No study materials found. Please upload study materials first." |
| < 4 terms total | Cannot start any Super Quiz | "At least 4 terms required for Super Quiz. You currently have X term(s)." |
| < 10 terms + Fixed10 selected | Cannot use Fixed10 option | "At least 10 terms required for '10 Questions' option. You currently have X term(s)." |
| Half option < 4 questions | Cannot use Half option | "At least 4 questions required. Selected option would generate X questions." |
| Exceeds MaxQuestionsLimit (50) | Truncate to 50 | Log warning, proceed with 50 |

### Edge Cases

| Terms Available | Fixed10 | Half | All |
|----------------|---------|------|-----|
| 4 | ❌ Error | 2 → ❌ Error | ✅ 4 questions |
| 8 | ❌ Error | 4 → ✅ 4 questions | ✅ 8 questions |
| 10 | ✅ 10 questions | 5 → ✅ 5 questions | ✅ 10 questions |
| 30 | ✅ 10 questions | 15 → ✅ 15 questions | ✅ 30 questions |
| 60 | ✅ 10 questions | 30 → ✅ 30 questions | ✅ 50 questions (truncated) |

---

## Testing Strategy

### Unit Tests

**File:** `SuperQuizServiceTests.cs` (new tests added)

1. **StartSuperQuizAsync_WithFixed10Option_Generates10Questions**
   - Arrange: 20 terms available
   - Act: Start with Fixed10
   - Assert: Session has exactly 10 questions

2. **StartSuperQuizAsync_WithHalfOption_GeneratesHalfQuestions**
   - Arrange: 20 terms available
   - Act: Start with Half
   - Assert: Session has exactly 10 questions

3. **StartSuperQuizAsync_WithAllOption_GeneratesAllQuestions**
   - Arrange: 20 terms available
   - Act: Start with All
   - Assert: Session has exactly 20 questions

4. **StartSuperQuizAsync_WithFixed10Option_And8Terms_ThrowsException**
   - Arrange: 8 terms available
   - Act: Start with Fixed10
   - Assert: Throws InvalidOperationException with clear message

5. **StartSuperQuizAsync_HalfOptionRoundsDown_With15Terms_Generates7Questions**
   - Arrange: 15 terms available
   - Act: Start with Half
   - Assert: Session has exactly 7 questions (15 / 2 = 7.5 → 7)

6. **StartSuperQuizAsync_RandomlySelectsQuestions_WithFixed10**
   - Arrange: 20 terms available
   - Act: Start with Fixed10 twice
   - Assert: Different questions selected in each session

7. **CalculateTargetQuestionCount_ReturnsCorrectValues**
   - Test each enum value returns expected count

### Controller Tests

**File:** `SuperQuizControllerTests.cs` (new tests added)

1. **Start_GET_ReturnsViewWithCorrectCounts**
   - Assert: View model has correct TotalAvailableTerms
   - Assert: Fixed10, Half, All counts calculated correctly

2. **Start_POST_WithFixed10_StartsSessionWith10Questions**
   - Assert: Service called with Fixed10 option
   - Assert: Redirects to Question action

3. **Start_POST_WithFixed10And8Terms_ReturnsViewWithError**
   - Assert: ModelState has error
   - Assert: Returns view (not redirect)

---

## Backward Compatibility

### Existing Code Impact

| Component | Change Required | Backward Compatible? |
|-----------|----------------|----------------------|
| `ISuperQuizService` | Method signature updated | ✅ Yes (default parameter) |
| `SuperQuizService` | Implementation updated | ✅ Yes (default behavior = All) |
| `SuperQuizController` | GET/POST updated | ✅ Yes (new form fields optional) |
| `SuperQuizStartViewModel` | Properties added | ⚠️ Views must be updated |
| `SuperQuizSession` | No changes | ✅ Yes |
| Other controllers | No changes | ✅ Yes |

### Migration Path

1. **Phase 1:** Add enum and update service signature with default parameter
2. **Phase 2:** Update controller and view model
3. **Phase 3:** Update view
4. **Phase 4:** Add tests

Existing sessions in cache are unaffected—they continue to work without changes.

---

## Performance Considerations

1. **Random Selection Performance:**
   - `OrderBy(_ => Guid.NewGuid())` is O(n log n)
   - For typical study material sizes (< 100 terms), performance impact is negligible
   - Alternative: Fisher-Yates shuffle if performance becomes a concern

2. **Additional Parsing Call:**
   - Controller now calls `ParseMarkdownFilesAsync` twice (GET and POST validation)
   - Consider caching parsed sections in TempData or session for POST validation
   - Current approach prioritizes correctness over optimization

3. **No Changes to Session Storage:**
   - Session size unchanged (same number of questions stored, just fewer selected initially)
   - Cache behavior unchanged

---

## Security Considerations

1. **Input Validation:**
   - Enum validation ensures only valid options accepted
   - MVC model binding handles enum parsing securely

2. **User Isolation:**
   - Existing username-based isolation preserved
   - No cross-user data leakage possible

3. **No New Attack Vectors:**
   - Selection is an enum, not free-form input
   - Existing anti-forgery token protection applies

---

## Future Enhancements (Out of Scope)

1. **Custom Question Count:**
   - Allow user to enter any number (e.g., 15, 25)
   - Requires additional validation and UI

2. **Remember User Preference:**
   - Save user's last selection in database
   - Load as default on next visit

3. **Per-File Selection:**
   - Allow different counts per study material file
   - Requires more complex UI and backend logic

---

## Implementation Checklist

### Backend Tasks
- [ ] Create `Models/SuperQuizQuestionCountOption.cs`
- [ ] Update `ViewModels/SuperQuizStartViewModel.cs`
- [ ] Update `Services/ISuperQuizService.cs` interface
- [ ] Update `Services/SuperQuizService.cs` implementation
- [ ] Add `CalculateTargetQuestionCount` helper method
- [ ] Update `Controllers/SuperQuizController.cs` GET action
- [ ] Update `Controllers/SuperQuizController.cs` POST action
- [ ] Write unit tests for service layer
- [ ] Write unit tests for controller layer

### Integration
- [ ] Verify backward compatibility
- [ ] Test edge cases (< 10 terms, exactly 10, > 50)
- [ ] Verify logging captures option selection
- [ ] Test with real study materials

---

## Appendix: Sample Code Snippets

### Helper Method: Random Selection Alternative (Fisher-Yates)

If `OrderBy(_ => Guid.NewGuid())` becomes a performance concern:

```csharp
private List<T> RandomSelect<T>(List<T> source, int count)
{
	var random = new Random();
	var result = new List<T>(count);
	var pool = new List<T>(source);

	for (int i = 0; i < count && pool.Count > 0; i++)
	{
		int index = random.Next(pool.Count);
		result.Add(pool[index]);
		pool.RemoveAt(index);
	}

	return result;
}
```

This is O(n) and produces a true random sample without replacement.
