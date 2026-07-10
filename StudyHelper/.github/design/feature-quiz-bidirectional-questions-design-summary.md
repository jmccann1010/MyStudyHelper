# Bidirectional Quiz Questions - Design Summary

**Feature:** Bidirectional Quiz Questions  
**Branch:** `feature/quiz-bidirectional-questions`  
**Status:** Architecture Design Ready for Engineering  
**Last Updated:** 2025-01-27  

---

## Executive Summary

This document provides the high-level architectural design for implementing bidirectional quiz questions in StudyHelper. The feature enables quizzes to test both term→definition and definition→term recall, enhancing learning comprehensiveness.

---

## Architecture Overview

### Current Architecture

```
User Request
	↓
QuizController / GradedQuizController
	↓
IQuestionGeneratorService
	↓
IMarkdownParserService (reads study materials)
	↓
Question Model (term as prompt, definitions as answers)
	↓
View (displays term, 4 definition options)
```

### New Architecture

```
User Request
	↓
QuizController / GradedQuizController
	↓
IQuestionGeneratorService (enhanced)
	↓
QuestionDirectionRandomizer (NEW)
	↓
IMarkdownParserService (reads study materials)
	↓
Question Model (with QuestionDirection enum)
	↓
- If TermToDefinition: term as prompt, definitions as answers
- If DefinitionToTerm: definition as prompt, terms as answers
	↓
View (displays prompt, 4 options based on direction)
```

---

## Key Architectural Decisions

### 1. Question Direction Representation

**Decision:** Use an enum to represent question direction

```csharp
public enum QuestionDirection
{
	TermToDefinition = 0,  // Show term, select definition (existing behavior)
	DefinitionToTerm = 1   // Show definition, select term (new behavior)
}
```

**Rationale:**
- Clear, type-safe representation
- Easy to extend in the future (e.g., multiple-term questions)
- Default value (0) maintains backward compatibility

### 2. Randomization Strategy

**Decision:** Randomize direction at question generation time with 50/50 probability

**Implementation:**
- Use `Random` class or `RandomNumberGenerator` for cryptographic randomness
- Apply randomization per question, not per session
- Store direction with each generated question

**Rationale:**
- Simple implementation
- Ensures variety within a single quiz session
- No user configuration needed for initial release

### 3. Distractor Generation

**Decision:** Select distractors from the opposite field of the study material

**For TermToDefinition (existing behavior):**
- Prompt: Term
- Correct Answer: Term's definition
- Distractors: Definitions of 3 other terms

**For DefinitionToTerm (new behavior):**
- Prompt: Definition
- Correct Answer: The term
- Distractors: 3 other terms

**Rationale:**
- Maintains contextual relevance
- Reuses existing distractor selection logic with direction-aware field selection
- Ensures questions are appropriately challenging

### 4. Session Storage

**Decision:** Extend existing session models to include question direction

**Changes:**
- `Question` model: Add `QuestionDirection` property
- `QuizSession` / `GradedQuizSession`: No structural changes needed (questions already stored)
- Session cache/TempData: Serialize direction with questions

**Rationale:**
- Minimal impact on existing session management
- Direction persists across page requests
- Backward compatible (default to TermToDefinition)

### 5. UI Display Strategy

**Decision:** Add contextual labels to indicate question type

**Implementation:**
- Display label above answer options: "Select the correct definition:" or "Select the correct term:"
- Maintain existing UI layout and styling
- Add direction indicator on results pages

**Rationale:**
- Clear user experience without major UI overhaul
- Accessible (text-based labels work with screen readers)
- Consistent with existing design patterns

---

## Data Model Changes

### Question Model

**Before:**
```csharp
public class Question
{
	public string Term { get; set; }
	public string CorrectDefinition { get; set; }
	public List<string> AllAnswerOptions { get; set; } // definitions
	public string Explanation { get; set; }
}
```

**After:**
```csharp
public class Question
{
	public string Term { get; set; }
	public string Definition { get; set; }
	public QuestionDirection Direction { get; set; } = QuestionDirection.TermToDefinition;

	// Computed properties for clarity
	public string Prompt => Direction == QuestionDirection.TermToDefinition ? Term : Definition;
	public string CorrectAnswer => Direction == QuestionDirection.TermToDefinition ? Definition : Term;

	public List<string> AllAnswerOptions { get; set; } // terms or definitions based on direction
	public string Explanation { get; set; }
}
```

### View Model Changes

**QuizQuestionViewModel / GradedQuizQuestionViewModel:**

Add property:
```csharp
public QuestionDirection Direction { get; set; }
public string DirectionLabel => Direction == QuestionDirection.TermToDefinition 
	? "Select the correct definition:" 
	: "Select the correct term:";
```

---

## Service Layer Changes

### IQuestionGeneratorService

**New Method Signature:**
```csharp
Task<Question> GenerateQuestionAsync(
	string username, 
	QuestionDirection? direction = null // null = random
);
```

**Implementation Logic:**
1. If direction is null, randomly select `TermToDefinition` or `DefinitionToTerm`
2. Parse study material to get term/definition pairs
3. Select a random term/definition pair as the correct answer
4. Based on direction:
   - Set prompt (term or definition)
   - Set correct answer (definition or term)
   - Generate 3 distractors from the opposite field
5. Shuffle answer options
6. Return Question with direction set

### Distractor Generation

**Enhanced Logic:**
```csharp
private List<string> GenerateDistractors(
	List<TermDefinitionPair> allPairs,
	TermDefinitionPair correctPair,
	QuestionDirection direction,
	int count = 3
)
{
	if (direction == QuestionDirection.TermToDefinition)
	{
		// Select 3 definitions from other terms
		return allPairs
			.Where(p => p.Term != correctPair.Term)
			.Select(p => p.Definition)
			.OrderBy(_ => Random.Shared.Next())
			.Take(count)
			.ToList();
	}
	else // DefinitionToTerm
	{
		// Select 3 terms from other pairs
		return allPairs
			.Where(p => p.Definition != correctPair.Definition)
			.Select(p => p.Term)
			.OrderBy(_ => Random.Shared.Next())
			.Take(count)
			.ToList();
	}
}
```

---

## Controller Layer Changes

### QuizController

**Changes:**
1. Pass question direction to view model when displaying questions
2. Validate answers considering direction
3. Generate explanations based on direction

**Example:**
```csharp
public IActionResult Question()
{
	var question = _quizService.GetCurrentQuestion(sessionId);
	var viewModel = new QuizQuestionViewModel
	{
		QuestionText = question.Prompt,
		AnswerOptions = question.AllAnswerOptions,
		Direction = question.Direction,
		DirectionLabel = question.Direction == QuestionDirection.TermToDefinition 
			? "Select the correct definition:" 
			: "Select the correct term:"
	};
	return View(viewModel);
}
```

### GradedQuizController

**Changes:**
1. Store direction with each question in session
2. Display direction indicator on results page
3. Group or filter results by direction (optional enhancement)

---

## View Layer Changes

### Quiz/Question.cshtml

**Add direction label:**
```html
<div class="mb-3">
	<h5>@Model.DirectionLabel</h5>
</div>

<div class="question-prompt mb-4">
	<p class="lead">@Model.QuestionText</p>
</div>
```

### GradedQuiz/Question.cshtml

**Similar changes to display direction label**

### GradedQuiz/Results.cshtml

**Add direction indicator for each question:**
```html
<div class="question-review">
	<div class="question-header">
		<span class="badge bg-info">
			@(question.Direction == QuestionDirection.TermToDefinition 
				? "Term → Definition" 
				: "Definition → Term")
		</span>
	</div>
	<!-- Rest of question review -->
</div>
```

---

## Backward Compatibility

### Handling Existing Sessions

**Strategy:**
- Questions without a `Direction` property default to `TermToDefinition`
- Existing quiz sessions continue to work without modification
- New sessions include direction for all questions

**Implementation:**
```csharp
public QuestionDirection Direction { get; set; } = QuestionDirection.TermToDefinition;
```

### Migration Path

**No database migration needed** (sessions are in-memory/cache)
- On next quiz start, new questions with direction are generated
- Old sessions expire naturally (session timeout)
- No data loss or corruption

---

## Performance Considerations

### Question Generation

**Impact:** Minimal
- Direction randomization: O(1)
- Distractor selection: Same complexity as existing logic
- No additional database or file I/O

**Optimization:**
- Cache parsed study material in session to avoid re-parsing
- Reuse existing shuffle and selection algorithms

### Session Storage

**Impact:** Negligible
- Additional 1 byte per question (enum stored as int)
- Typical quiz: 10-50 questions = 10-50 bytes additional storage
- Well within session size limits

---

## Security Considerations

### Input Validation

**Existing validation continues to apply:**
- Answer indices are validated (0-3)
- Session IDs are validated
- User authentication required

**New validation:**
- Ensure `QuestionDirection` enum is valid (0 or 1)
- Reject invalid direction values

### Data Integrity

**Question Generation:**
- Ensure minimum 4 terms exist before allowing quiz start
- Validate that distractors are unique and different from correct answer

---

## Testing Strategy

### Unit Tests

1. **Question Generation:**
   - Generate question with `TermToDefinition` direction
   - Generate question with `DefinitionToTerm` direction
   - Generate question with null direction (random)
   - Verify distractor generation for both directions
   - Verify answer shuffling

2. **Answer Validation:**
   - Validate correct answer for `TermToDefinition` question
   - Validate correct answer for `DefinitionToTerm` question
   - Validate incorrect answers for both directions

3. **Session Management:**
   - Store questions with direction in session
   - Retrieve questions with direction from session
   - Handle backward compatibility (missing direction)

### Integration Tests

1. Start quiz and verify bidirectional questions generated
2. Answer `TermToDefinition` question and verify feedback
3. Answer `DefinitionToTerm` question and verify feedback
4. Complete graded quiz with mixed directions
5. View results and verify direction indicators displayed

### Manual Testing Scenarios

1. **Quiz Flow:**
   - Start practice quiz
   - Observe mix of question directions
   - Answer questions and verify feedback
   - Complete quiz

2. **Graded Quiz Flow:**
   - Start graded quiz
   - Complete all questions
   - View results and verify direction indicators
   - Verify score calculation

3. **Edge Cases:**
   - Study material with exactly 4 terms
   - Study material with 100+ terms
   - Very short terms/definitions
   - Very long terms/definitions

---

## Deployment Strategy

### Phase 1: Backend (1 week)
- Implement `QuestionDirection` enum
- Update `Question` model
- Enhance `IQuestionGeneratorService`
- Update controllers
- Write unit tests

### Phase 2: Frontend (3-5 days)
- Update quiz views with direction labels
- Update results views with direction indicators
- Style and accessibility review

### Phase 3: Testing (2-3 days)
- Execute integration tests
- Manual testing and QA
- Accessibility audit

### Phase 4: Deployment (1 day)
- Deploy to production
- Monitor logs for errors
- Collect initial user feedback

---

## Success Metrics

1. **Generation Success Rate:** 100% of quizzes generate with bidirectional questions
2. **Direction Distribution:** 45-55% split between directions (approximately equal)
3. **No Regression:** Existing quiz functionality unaffected
4. **Performance:** No measurable increase in page load times
5. **User Engagement:** No decrease in quiz completion rates

---

## Risks & Mitigation

| Risk | Mitigation |
|------|------------|
| Insufficient terms (< 4) for distractors | Validate term count at quiz start; show error message |
| User confusion about question type | Clear labels, update help documentation |
| Backward compatibility issues | Default direction, extensive testing |
| Performance degradation | Optimize distractor selection, monitor metrics |

---

## Future Enhancements

1. **User Preference:** Allow users to set direction ratio (e.g., 70% term→definition)
2. **Analytics:** Track performance by question direction
3. **Adaptive Difficulty:** Increase proportion of harder direction based on user performance
4. **Mixed Question Types:** Combine term/definition questions with other formats

---

## Dependencies

- ✅ No external library dependencies
- ✅ Compatible with existing study material format
- ✅ Uses existing session management infrastructure
- ✅ No database schema changes

---

## Sign-Off

**Architect:** ✅ Approved  
**Date:** 2025-01-27  
**Next Step:** Backend Design Document

---

## References

- Feature Specification: `.github/design/feature-quiz-bidirectional-questions-spec.md`
- Backend Design: `.github/design/feature-quiz-bidirectional-questions-backend-design.md` (next)
- Frontend Design: `.github/design/feature-quiz-bidirectional-questions-frontend-design.md` (next)
