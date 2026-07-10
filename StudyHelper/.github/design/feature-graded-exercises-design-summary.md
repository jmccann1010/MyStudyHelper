# Graded Exercises - Design Summary

**Feature:** Graded Exercises  
**Branch:** `feature/graded-exercises`  
**Status:** Design Ready for Engineering  
**Last Updated:** 2025-01-27  

---

## Executive Summary

The Graded Exercises feature extends the StudyHelper platform with scored equation-solving practice. It mirrors the successful Graded Quiz architecture but generates numerical calculation problems from the user's equation files instead of multiple-choice questions.

**Key Differentiators from Graded Quiz:**
- Numerical answer input instead of multiple-choice selection
- Decimal validation with tolerance (±0.01) for rounding differences
- Uses `IExerciseProblemGeneratorService` instead of `IQuestionGeneratorService`
- Step-by-step solution display in results
- Currency/ratio formatting support

---

## Architecture Overview

### System Components

```
┌─────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                       │
├─────────────────────────────────────────────────────────────────┤
│  GradedExerciseController                                       │
│  ├─ Setup (GET)           → Display problem count selection     │
│  ├─ StartExercise (POST)  → Initialize session                  │
│  ├─ Problem (GET)         → Display current problem             │
│  ├─ SubmitAnswer (POST)   → Validate answer, advance            │
│  ├─ Results (GET)         → Display final score & review        │
│  └─ RetakeExercise (GET)  → Clear session, restart              │
└─────────────────────────────────────────────────────────────────┘
							  ↓
┌─────────────────────────────────────────────────────────────────┐
│                         SERVICE LAYER                            │
├─────────────────────────────────────────────────────────────────┤
│  IGradedExerciseService (NEW)                                   │
│  ├─ StartExerciseAsync()      → Create session, generate probs  │
│  ├─ SubmitAnswerAsync()       → Validate, score, advance        │
│  ├─ GetExerciseSessionAsync() → Retrieve session state          │
│  ├─ GetExerciseProgressAsync()→ Get score/progress              │
│  ├─ FinishExerciseAsync()     → Finalize & calculate score      │
│  └─ ClearExerciseSessionAsync()→ Remove session                 │
│                                                                  │
│  IExerciseProblemGeneratorService (EXISTING)                    │
│  ├─ GenerateProblem()         → Create calculation problem      │
│  └─ ValidateAnswer()          → Check answer with tolerance     │
│                                                                  │
│  IEquationParserService (EXISTING)                              │
│  └─ ParseEquationFileAsync()  → Load equations from files       │
│                                                                  │
│  IUserStudyMaterialService (EXISTING)                           │
│  └─ GetStudyMaterialPathAsync()→ Get user's equation file       │
└─────────────────────────────────────────────────────────────────┘
							  ↓
┌─────────────────────────────────────────────────────────────────┐
│                         DATA LAYER                               │
├─────────────────────────────────────────────────────────────────┤
│  IMemoryCache                                                    │
│  └─ Session Storage (30-minute TTL)                             │
│                                                                  │
│  TempData (Session-backed)                                      │
│  └─ Session ID persistence across redirects                     │
└─────────────────────────────────────────────────────────────────┘
```

---

## Core Models

### GradedExerciseSession
Represents an active exercise session with user progress.

```csharp
public class GradedExerciseSession
{
	public string SessionId { get; set; }          // Unique GUID
	public string Username { get; set; }            // For user scoping
	public int TotalProblems { get; set; }          // 1-50
	public int CurrentProblemIndex { get; set; }    // 0-based
	public List<ExerciseProblem> Problems { get; set; }
	public Dictionary<int, decimal> UserAnswers { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime LastActivityAt { get; set; }
	public bool IsComplete { get; set; }
}
```

### ExerciseScore
Calculates and stores score metrics.

```csharp
public class ExerciseScore
{
	public int CorrectCount { get; set; }
	public int IncorrectCount { get; set; }
	public int TotalProblems { get; set; }
	public decimal Percentage => TotalProblems > 0 
		? (decimal)CorrectCount / TotalProblems * 100 
		: 0;
	public string PerformanceRating { get; set; }   // Calculated
}
```

### SubmitAnswerResult
Returned from answer validation.

```csharp
public class SubmitAnswerResult
{
	public bool IsValid { get; set; }               // Input validation
	public bool IsCorrect { get; set; }             // Answer correctness
	public bool IsLastProblem { get; set; }         // Session completion
	public string ErrorMessage { get; set; }        // Validation errors
	public decimal CorrectAnswer { get; set; }      // For display
}
```

---

## Key Design Decisions

### 1. Numerical Input Validation
**Challenge:** Users may enter values with different formatting or rounding.

**Solution:**
- Accept input as string, parse to decimal
- Remove common formatting: `$`, `,`, `%`, spaces
- Validate within ±0.01 tolerance of correct answer
- Provide clear error messages for non-numeric input

```csharp
// Example validation logic
public bool ValidateAnswer(decimal correctAnswer, string userInput)
{
	if (!decimal.TryParse(userInput.Replace("$", "")
								   .Replace(",", "")
								   .Replace("%", "")
								   .Trim(), 
						 out decimal userAnswer))
	{
		return false; // Invalid format
	}

	return Math.Abs(userAnswer - correctAnswer) <= 0.01m;
}
```

### 2. Session Management
**Pattern:** Follow GradedQuiz implementation exactly.

**Rationale:**
- Proven pattern already in production
- Uses `TempData.Peek()` and `TempData.Keep()` to avoid premature consumption
- Session ID stored in TempData with key `"GradedExerciseSessionId"`
- Full session cached in `IMemoryCache` with 30-minute sliding expiration

### 3. Problem Generation
**Reuse:** Leverage existing `IExerciseProblemGeneratorService`

**Flow:**
1. Load user's equation file via `IUserStudyMaterialService`
2. Parse equations via `IEquationParserService`
3. For each problem:
   - Call `GenerateProblem(equations)` to get random problem
   - Store in `GradedExerciseSession.Problems` list
4. No problem regeneration during session (consistency)

### 4. Score Calculation
**Performance Rating Scale:**
- **Excellent:** 90-100%
- **Good:** 80-89%
- **Fair:** 70-79%
- **Poor:** 60-69%
- **Needs Improvement:** <60%

Same as Graded Quiz for consistency.

### 5. Security
**Requirements:**
- All controller actions require `[Authorize]` attribute
- Sessions scoped by username (prevent cross-user access)
- CSRF protection on all POST actions (`[ValidateAntiForgeryToken]`)
- Input sanitization for decimal parsing
- Logging includes username and session ID for audit

---

## User Flow

### Happy Path
```
1. User clicks "Start Graded Exercises" on home page
   ↓
2. Setup page displays, user selects problem count (e.g., 10)
   ↓
3. System generates 10 problems from user's equation file
   ↓
4. Problem 1/10 displays with equation and given values
   ↓
5. User enters answer (e.g., "1250.50") and clicks Submit
   ↓
6. System validates (correct), increments score, advances
   ↓
7. Problem 2/10 displays... repeat until Problem 10/10
   ↓
8. Results page displays: "8/10 - 80% - Good"
   ↓
9. User reviews all problems, sees solution steps
   ↓
10. User clicks "Retake Exercises" or "Back to Home"
```

### Error Scenarios
**No Equation File:**
- Display error: "No equation file uploaded. Please upload equations first."
- Redirect to Study Materials page

**Invalid Answer Input:**
- Display error: "Please enter a valid number."
- Stay on same problem, allow retry

**Session Expired:**
- Display message: "Your session expired. Please start a new exercise."
- Redirect to Setup page

**Insufficient Equations:**
- Display error: "Not enough equations. Please select fewer problems."
- Stay on Setup page

---

## Performance Considerations

### Caching Strategy
- **Session Storage:** In-memory cache (IMemoryCache)
- **TTL:** 30 minutes sliding expiration
- **Size:** ~1-5 KB per session (10 problems × ~500 bytes)
- **Expected Load:** <100 concurrent sessions per server

### Equation Parsing
- Parse equation file once at session start
- Cache equations in session object (no re-parsing)
- File I/O limited to session initialization

### Problem Generation
- Pre-generate all problems at session start
- No dynamic generation during session (predictable performance)
- Random seed per session for reproducibility

---

## Testing Strategy

### Unit Tests
**Target Coverage:** >80%

**Key Test Cases:**
1. `StartExerciseAsync()` with valid/invalid problem counts
2. `SubmitAnswerAsync()` with correct/incorrect/invalid answers
3. Decimal tolerance validation (±0.01)
4. Session expiration handling
5. Score calculation accuracy
6. Performance rating calculation
7. Session lifecycle (start → progress → finish → clear)

### Integration Tests
1. Full exercise flow from setup to results
2. Session persistence across redirects
3. User authentication and authorization
4. TempData and cache interaction
5. Multiple concurrent user sessions

### Manual Testing
1. Complete exercise with all correct answers
2. Complete exercise with mixed correct/incorrect
3. Test decimal rounding edge cases (e.g., 100.004 vs 100.00)
4. Test session timeout scenarios
5. Test with different problem counts (1, 10, 50)
6. Test with various equation types (currency, ratios)
7. Verify responsive design on mobile/tablet/desktop

---

## Deployment Checklist

- [ ] All services registered in `Program.cs`
- [ ] All controller actions have `[Authorize]` attribute
- [ ] All POST actions have `[ValidateAntiForgeryToken]`
- [ ] Session timeout configured (30 minutes)
- [ ] Logging configured for all service methods
- [ ] Error handling implemented for all edge cases
- [ ] Unit tests passing (>80% coverage)
- [ ] Manual testing completed
- [ ] Performance testing (100 concurrent sessions)
- [ ] Security review completed
- [ ] Documentation updated (user guide, API docs)
- [ ] Home page panel added and tested
- [ ] Merge conflicts resolved
- [ ] Code review approved

---

## Future Enhancements (Out of Scope)

1. **Persistent Scoring:** Save exercise history to database
2. **Leaderboard:** Compare scores with other users
3. **Difficulty Levels:** Easy/Medium/Hard problem selection
4. **Hints:** Show partial solution steps before submission
5. **Timed Mode:** Add countdown timer per problem
6. **Practice Mode:** Allow retries without affecting score
7. **Analytics:** Track which equation types users struggle with
8. **Export Results:** Download PDF report of exercise session

---

## Related Documents

- Backend Design: [feature-graded-exercises-backend-design.md](./feature-graded-exercises-backend-design.md)
- Frontend Design: [feature-graded-exercises-frontend-design.md](./feature-graded-exercises-frontend-design.md)
- User Stories: [../.github/project-management/feature-graded-exercises-spec.md](../project-management/feature-graded-exercises-spec.md)
- Graded Quiz Reference: [feature-graded-quiz-backend-design.md](./feature-graded-quiz-backend-design.md)

---

## Approval

**Architect:** ✅ Approved  
**Date:** 2025-01-27  
**Next Step:** Proceed with backend implementation (Story 6, 3, 7)
