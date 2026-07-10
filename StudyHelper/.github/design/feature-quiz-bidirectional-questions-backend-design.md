# Bidirectional Quiz Questions - Backend Design Document

**Feature:** Bidirectional Quiz Questions  
**Branch:** `feature/quiz-bidirectional-questions`  
**Status:** Design Ready for Engineering  
**Last Updated:** 2025-01-27  

---

## Overview

This document details the backend implementation for bidirectional quiz questions, including models, services, controllers, and data flow.

---

## 1. Models

### 1.1 QuestionDirection Enum

**File:** `Models/QuestionDirection.cs` (NEW)

```csharp
namespace StudyHelper.Models;

/// <summary>
/// Defines the direction of a quiz question.
/// </summary>
public enum QuestionDirection
{
	/// <summary>
	/// Question displays a term and asks for the definition.
	/// This is the traditional quiz format.
	/// </summary>
	TermToDefinition = 0,

	/// <summary>
	/// Question displays a definition and asks for the term.
	/// This is the new bidirectional format.
	/// </summary>
	DefinitionToTerm = 1
}
```

**Rationale:**
- Default value of `0` ensures backward compatibility
- Enum provides type safety and clarity
- Summary comments document intent for each direction

---

### 1.2 Question Model

**File:** `Models/Question.cs` (MODIFY)

**Current:**
```csharp
public class Question
{
	public string Term { get; set; } = string.Empty;
	public string CorrectDefinition { get; set; } = string.Empty;
	public List<string> AllAnswerOptions { get; set; } = new();
	public string Explanation { get; set; } = string.Empty;
}
```

**New:**
```csharp
namespace StudyHelper.Models;

/// <summary>
/// Represents a quiz question with support for bidirectional formats.
/// </summary>
public class Question
{
	/// <summary>
	/// The term from the study material.
	/// </summary>
	public string Term { get; set; } = string.Empty;

	/// <summary>
	/// The definition from the study material.
	/// </summary>
	public string Definition { get; set; } = string.Empty;

	/// <summary>
	/// The direction of the question (term→definition or definition→term).
	/// Defaults to TermToDefinition for backward compatibility.
	/// </summary>
	public QuestionDirection Direction { get; set; } = QuestionDirection.TermToDefinition;

	/// <summary>
	/// Gets the question prompt based on direction.
	/// For TermToDefinition: returns the term.
	/// For DefinitionToTerm: returns the definition.
	/// </summary>
	public string Prompt => Direction == QuestionDirection.TermToDefinition ? Term : Definition;

	/// <summary>
	/// Gets the correct answer based on direction.
	/// For TermToDefinition: returns the definition.
	/// For DefinitionToTerm: returns the term.
	/// </summary>
	public string CorrectAnswer => Direction == QuestionDirection.TermToDefinition ? Definition : Term;

	/// <summary>
	/// All answer options (1 correct + 3 distractors), shuffled.
	/// Contains definitions for TermToDefinition questions.
	/// Contains terms for DefinitionToTerm questions.
	/// </summary>
	public List<string> AllAnswerOptions { get; set; } = new();

	/// <summary>
	/// Explanation for the correct answer, contextualized to the question direction.
	/// </summary>
	public string Explanation { get; set; } = string.Empty;

	/// <summary>
	/// DEPRECATED: Use Definition property instead.
	/// Maintained for backward compatibility.
	/// </summary>
	[Obsolete("Use Definition property instead. This property will be removed in a future version.")]
	public string CorrectDefinition
	{
		get => Definition;
		set => Definition = value;
	}
}
```

**Changes:**
- ✅ Added `Definition` property (replaces `CorrectDefinition`)
- ✅ Added `Direction` property with default value
- ✅ Added `Prompt` computed property
- ✅ Added `CorrectAnswer` computed property
- ✅ Deprecated `CorrectDefinition` for backward compatibility
- ✅ Updated XML documentation

---

### 1.3 TermDefinitionPair Model

**File:** `Models/TermDefinitionPair.cs` (EXISTS - NO CHANGES NEEDED)

```csharp
public class TermDefinitionPair
{
	public string Term { get; set; } = string.Empty;
	public string Definition { get; set; } = string.Empty;
}
```

**Usage:**
- Used internally by services for parsing study materials
- No changes required

---

## 2. Services

### 2.1 IQuestionGeneratorService Interface

**File:** `Services/IQuestionGeneratorService.cs` (MODIFY)

**Add method overload:**

```csharp
/// <summary>
/// Generates a single quiz question with the specified or random direction.
/// </summary>
/// <param name="username">The username to retrieve study materials for.</param>
/// <param name="direction">
/// The question direction. If null, a random direction is chosen (50/50 split).
/// </param>
/// <returns>A generated question with answers and explanation.</returns>
Task<Question> GenerateQuestionAsync(string username, QuestionDirection? direction = null);
```

**Backward Compatibility:**
- Existing `GenerateQuestionAsync(string username)` method continues to work
- Internally calls new method with `direction: null`

---

### 2.2 QuestionGeneratorService Implementation

**File:** `Services/QuestionGeneratorService.cs` (MODIFY)

**New/Modified Methods:**

#### 2.2.1 GenerateQuestionAsync (Enhanced)

```csharp
public async Task<Question> GenerateQuestionAsync(string username, QuestionDirection? direction = null)
{
	_logger.LogInformation("Generating question for user {Username} with direction {Direction}", 
		username, direction?.ToString() ?? "Random");

	// Get study materials
	var pairs = await _markdownParser.ParseTermsAsync(username);

	if (pairs == null || pairs.Count < 4)
	{
		throw new InvalidOperationException(
			"Insufficient study material. At least 4 terms are required to generate quiz questions.");
	}

	// Determine direction (random if not specified)
	var questionDirection = direction ?? RandomizeDirection();

	// Select a random term/definition pair as the correct answer
	var correctPair = pairs[Random.Shared.Next(pairs.Count)];

	// Generate distractors based on direction
	var distractors = GenerateDistractors(pairs, correctPair, questionDirection, 3);

	// Combine correct answer with distractors
	var allOptions = new List<string> { GetCorrectAnswerForDirection(correctPair, questionDirection) };
	allOptions.AddRange(distractors);

	// Shuffle options
	allOptions = allOptions.OrderBy(_ => Random.Shared.Next()).ToList();

	// Generate explanation
	var explanation = GenerateExplanation(correctPair, questionDirection);

	return new Question
	{
		Term = correctPair.Term,
		Definition = correctPair.Definition,
		Direction = questionDirection,
		AllAnswerOptions = allOptions,
		Explanation = explanation
	};
}
```

#### 2.2.2 RandomizeDirection (NEW)

```csharp
/// <summary>
/// Randomly selects a question direction with 50/50 probability.
/// </summary>
/// <returns>Either TermToDefinition or DefinitionToTerm.</returns>
private QuestionDirection RandomizeDirection()
{
	return Random.Shared.Next(2) == 0 
		? QuestionDirection.TermToDefinition 
		: QuestionDirection.DefinitionToTerm;
}
```

#### 2.2.3 GenerateDistractors (MODIFY)

```csharp
/// <summary>
/// Generates plausible incorrect answer options (distractors) based on question direction.
/// </summary>
/// <param name="allPairs">All available term/definition pairs.</param>
/// <param name="correctPair">The correct term/definition pair.</param>
/// <param name="direction">The question direction.</param>
/// <param name="count">Number of distractors to generate (default 3).</param>
/// <returns>List of distractor strings.</returns>
private List<string> GenerateDistractors(
	List<TermDefinitionPair> allPairs,
	TermDefinitionPair correctPair,
	QuestionDirection direction,
	int count = 3)
{
	List<string> candidates;

	if (direction == QuestionDirection.TermToDefinition)
	{
		// Question asks for definition, so distractors are other definitions
		candidates = allPairs
			.Where(p => p.Term != correctPair.Term)
			.Select(p => p.Definition)
			.Distinct()
			.ToList();
	}
	else // DefinitionToTerm
	{
		// Question asks for term, so distractors are other terms
		candidates = allPairs
			.Where(p => p.Definition != correctPair.Definition)
			.Select(p => p.Term)
			.Distinct()
			.ToList();
	}

	if (candidates.Count < count)
	{
		throw new InvalidOperationException(
			$"Insufficient unique terms/definitions for distractors. Required: {count}, Available: {candidates.Count}");
	}

	// Randomly select 'count' distractors
	return candidates
		.OrderBy(_ => Random.Shared.Next())
		.Take(count)
		.ToList();
}
```

#### 2.2.4 GetCorrectAnswerForDirection (NEW)

```csharp
/// <summary>
/// Gets the correct answer string based on question direction.
/// </summary>
private string GetCorrectAnswerForDirection(TermDefinitionPair pair, QuestionDirection direction)
{
	return direction == QuestionDirection.TermToDefinition 
		? pair.Definition 
		: pair.Term;
}
```

#### 2.2.5 GenerateExplanation (MODIFY)

```csharp
/// <summary>
/// Generates a contextual explanation for the question.
/// </summary>
private string GenerateExplanation(TermDefinitionPair pair, QuestionDirection direction)
{
	if (direction == QuestionDirection.TermToDefinition)
	{
		return $"The term '{pair.Term}' is defined as: {pair.Definition}";
	}
	else // DefinitionToTerm
	{
		return $"The definition '{pair.Definition}' refers to the term: {pair.Term}";
	}
}
```

---

### 2.3 QuizService (Session Management)

**File:** `Services/QuizService.cs` (MINOR MODIFICATIONS)

**Changes:**
- No structural changes needed
- Questions stored in session already include `Direction` property
- Session serialization/deserialization handles new property automatically

**Verification:**
- Ensure `Question` objects are serialized/deserialized correctly with `Direction`
- Test session persistence across page requests

---

### 2.4 GradedQuizService (Session Management)

**File:** `Services/GradedQuizService.cs` (MINOR MODIFICATIONS)

**Changes:**
- Similar to `QuizService`, no structural changes needed
- Questions in graded quiz sessions automatically include `Direction`

---

## 3. Controllers

### 3.1 QuizController

**File:** `Controllers/QuizController.cs` (MODIFY)

#### 3.1.1 Question Action (MODIFY)

```csharp
[HttpGet]
public IActionResult Question()
{
	var sessionId = TempData.Peek("QuizSessionId") as string;
	if (string.IsNullOrEmpty(sessionId))
	{
		TempData["ErrorMessage"] = "No active quiz session found.";
		return RedirectToAction(nameof(Index));
	}

	var question = _quizService.GetCurrentQuestion(sessionId);
	if (question == null)
	{
		TempData["ErrorMessage"] = "Unable to retrieve question.";
		return RedirectToAction(nameof(Index));
	}

	var viewModel = new QuizQuestionViewModel
	{
		QuestionText = question.Prompt, // Uses Direction-aware Prompt property
		AnswerOptions = question.AllAnswerOptions,
		Direction = question.Direction,
		DirectionLabel = GetDirectionLabel(question.Direction)
	};

	TempData.Keep("QuizSessionId");
	return View(viewModel);
}
```

#### 3.1.2 SubmitAnswer Action (MODIFY)

```csharp
[HttpPost]
public IActionResult SubmitAnswer(int selectedAnswerIndex)
{
	var sessionId = TempData.Peek("QuizSessionId") as string;
	if (string.IsNullOrEmpty(sessionId))
	{
		TempData["ErrorMessage"] = "Session expired.";
		return RedirectToAction(nameof(Index));
	}

	var question = _quizService.GetCurrentQuestion(sessionId);
	if (question == null)
	{
		TempData["ErrorMessage"] = "Unable to validate answer.";
		return RedirectToAction(nameof(Index));
	}

	// Validate answer
	var selectedAnswer = question.AllAnswerOptions[selectedAnswerIndex];
	var isCorrect = selectedAnswer == question.CorrectAnswer; // Uses Direction-aware CorrectAnswer property

	// Prepare feedback
	var feedbackViewModel = new QuizFeedbackViewModel
	{
		IsCorrect = isCorrect,
		SelectedAnswer = selectedAnswer,
		CorrectAnswer = question.CorrectAnswer,
		Explanation = question.Explanation,
		Term = question.Term,
		Definition = question.Definition,
		Direction = question.Direction
	};

	TempData.Keep("QuizSessionId");
	return View("Feedback", feedbackViewModel);
}
```

#### 3.1.3 GetDirectionLabel Helper (NEW)

```csharp
/// <summary>
/// Gets a user-friendly label for the question direction.
/// </summary>
private string GetDirectionLabel(QuestionDirection direction)
{
	return direction == QuestionDirection.TermToDefinition
		? "Select the correct definition:"
		: "Select the correct term:";
}
```

---

### 3.2 GradedQuizController

**File:** `Controllers/GradedQuizController.cs` (MODIFY)

#### 3.2.1 Question Action (MODIFY)

```csharp
[HttpGet]
public IActionResult Question()
{
	var sessionId = TempData.Peek(SessionKey) as string;
	if (string.IsNullOrEmpty(sessionId))
	{
		return RedirectToAction(nameof(Setup));
	}

	var session = _gradedQuizService.GetQuizSessionAsync(sessionId).Result;
	if (session == null || session.IsComplete)
	{
		return RedirectToAction(nameof(Results));
	}

	var currentQuestion = session.Questions[session.CurrentQuestionIndex];
	var progress = _gradedQuizService.GetQuizProgressAsync(sessionId).Result;

	var viewModel = new GradedQuizQuestionViewModel
	{
		QuestionNumber = session.CurrentQuestionIndex + 1,
		TotalQuestions = session.TotalQuestions,
		QuestionText = currentQuestion.Prompt, // Direction-aware
		AnswerOptions = currentQuestion.AllAnswerOptions,
		Direction = currentQuestion.Direction,
		DirectionLabel = GetDirectionLabel(currentQuestion.Direction),
		CorrectCount = progress.CorrectCount,
		IncorrectCount = progress.IncorrectCount,
		ProgressPercentage = progress.ProgressPercentage
	};

	TempData.Keep(SessionKey);
	return View(viewModel);
}
```

#### 3.2.2 Results Action (MODIFY)

Ensure results view model includes question direction for each question:

```csharp
[HttpGet]
public IActionResult Results()
{
	var sessionId = TempData.Peek(SessionKey) as string;
	// ... existing validation ...

	var viewModel = new GradedQuizResultViewModel
	{
		// ... existing properties ...
		Questions = session.Questions.Select(q => new QuestionReviewItem
		{
			QuestionText = q.Prompt,
			CorrectAnswer = q.CorrectAnswer,
			UserAnswer = session.UserAnswers[session.Questions.IndexOf(q)],
			IsCorrect = session.UserAnswers[session.Questions.IndexOf(q)] == q.CorrectAnswer,
			Explanation = q.Explanation,
			Direction = q.Direction,
			DirectionLabel = GetDirectionLabel(q.Direction)
		}).ToList()
	};

	return View(viewModel);
}
```

---

## 4. View Models

### 4.1 QuizQuestionViewModel

**File:** `ViewModels/QuizQuestionViewModel.cs` (MODIFY)

```csharp
namespace StudyHelper.ViewModels;

public class QuizQuestionViewModel
{
	public string QuestionText { get; set; } = string.Empty;
	public List<string> AnswerOptions { get; set; } = new();

	/// <summary>
	/// The direction of the question.
	/// </summary>
	public QuestionDirection Direction { get; set; }

	/// <summary>
	/// User-friendly label indicating what type of answer is expected.
	/// </summary>
	public string DirectionLabel { get; set; } = string.Empty;
}
```

---

### 4.2 QuizFeedbackViewModel

**File:** `ViewModels/QuizFeedbackViewModel.cs` (MODIFY)

```csharp
namespace StudyHelper.ViewModels;

public class QuizFeedbackViewModel
{
	public bool IsCorrect { get; set; }
	public string SelectedAnswer { get; set; } = string.Empty;
	public string CorrectAnswer { get; set; } = string.Empty;
	public string Explanation { get; set; } = string.Empty;

	/// <summary>
	/// The term (always included for context).
	/// </summary>
	public string Term { get; set; } = string.Empty;

	/// <summary>
	/// The definition (always included for context).
	/// </summary>
	public string Definition { get; set; } = string.Empty;

	/// <summary>
	/// The direction of the question.
	/// </summary>
	public QuestionDirection Direction { get; set; }
}
```

---

### 4.3 GradedQuizQuestionViewModel

**File:** `ViewModels/GradedQuizQuestionViewModel.cs` (MODIFY)

```csharp
namespace StudyHelper.ViewModels;

public class GradedQuizQuestionViewModel
{
	public int QuestionNumber { get; set; }
	public int TotalQuestions { get; set; }
	public string QuestionText { get; set; } = string.Empty;
	public List<string> AnswerOptions { get; set; } = new();
	public int CorrectCount { get; set; }
	public int IncorrectCount { get; set; }
	public double ProgressPercentage { get; set; }

	/// <summary>
	/// The direction of the question.
	/// </summary>
	public QuestionDirection Direction { get; set; }

	/// <summary>
	/// User-friendly label indicating what type of answer is expected.
	/// </summary>
	public string DirectionLabel { get; set; } = string.Empty;
}
```

---

### 4.4 QuestionReviewItem

**File:** `ViewModels/QuestionReviewItem.cs` (NEW or MODIFY)

```csharp
namespace StudyHelper.ViewModels;

/// <summary>
/// Represents a single question in the graded quiz results review.
/// </summary>
public class QuestionReviewItem
{
	public string QuestionText { get; set; } = string.Empty;
	public string CorrectAnswer { get; set; } = string.Empty;
	public string UserAnswer { get; set; } = string.Empty;
	public bool IsCorrect { get; set; }
	public string Explanation { get; set; } = string.Empty;

	/// <summary>
	/// The direction of the question.
	/// </summary>
	public QuestionDirection Direction { get; set; }

	/// <summary>
	/// User-friendly label for the question direction.
	/// </summary>
	public string DirectionLabel { get; set; } = string.Empty;
}
```

---

## 5. Data Flow

### 5.1 Question Generation Flow

```
1. User starts quiz
   ↓
2. QuizController.StartQuiz() or GradedQuizController.StartQuiz()
   ↓
3. Call IQuestionGeneratorService.GenerateQuestionAsync(username, direction: null)
   ↓
4. Service randomizes direction (50/50)
   ↓
5. Service loads study materials via IMarkdownParserService
   ↓
6. Service selects random term/definition pair
   ↓
7. Service generates 3 distractors based on direction
   ↓
8. Service shuffles answer options
   ↓
9. Service returns Question with Direction set
   ↓
10. Controller stores question in session
   ↓
11. Redirect to Question view
```

### 5.2 Question Display Flow

```
1. User navigates to Question page
   ↓
2. Controller retrieves question from session
   ↓
3. Controller creates view model with:
   - QuestionText = question.Prompt (term or definition based on direction)
   - AnswerOptions = question.AllAnswerOptions (definitions or terms)
   - Direction = question.Direction
   - DirectionLabel = "Select the correct definition:" or "Select the correct term:"
   ↓
4. View displays direction label and question
   ↓
5. User selects answer
```

### 5.3 Answer Validation Flow

```
1. User submits answer
   ↓
2. Controller retrieves question from session
   ↓
3. Controller validates: selectedAnswer == question.CorrectAnswer
   - CorrectAnswer property returns definition or term based on direction
   ↓
4. Controller creates feedback view model
   ↓
5. Display feedback with explanation
```

---

## 6. Validation & Error Handling

### 6.1 Input Validation

**Minimum Terms Check:**
```csharp
if (pairs == null || pairs.Count < 4)
{
	throw new InvalidOperationException(
		"Insufficient study material. At least 4 terms are required to generate quiz questions.");
}
```

**Direction Validation:**
```csharp
public QuestionDirection Direction { get; set; } = QuestionDirection.TermToDefinition;

// In controller or service:
if (!Enum.IsDefined(typeof(QuestionDirection), direction))
{
	throw new ArgumentException("Invalid question direction", nameof(direction));
}
```

### 6.2 Error Messages

| Scenario | Error Message |
|----------|---------------|
| < 4 terms in study material | "You need at least 4 terms in your study material to take a quiz." |
| Insufficient distractors | "Unable to generate question. Please add more terms to your study material." |
| Invalid session | "Your quiz session has expired. Please start a new quiz." |
| Invalid direction value | "Invalid question configuration. Please try again." |

---

## 7. Logging

### 7.1 Log Points

**Question Generation:**
```csharp
_logger.LogInformation("Generating question for user {Username} with direction {Direction}", 
	username, direction?.ToString() ?? "Random");

_logger.LogDebug("Selected direction: {Direction}, Term: {Term}", 
	questionDirection, correctPair.Term);
```

**Answer Validation:**
```csharp
_logger.LogInformation("User {Username} answered question {Direction} - Result: {IsCorrect}", 
	username, question.Direction, isCorrect);
```

**Errors:**
```csharp
_logger.LogWarning("Insufficient study material for user {Username}. Term count: {Count}", 
	username, pairs.Count);
```

---

## 8. Testing

### 8.1 Unit Tests

**File:** `Tests/Services/QuestionGeneratorServiceTests.cs` (NEW TESTS)

```csharp
[Fact]
public async Task GenerateQuestionAsync_WithTermToDefinitionDirection_ReturnsCorrectFormat()
{
	// Arrange
	var service = CreateService();
	var direction = QuestionDirection.TermToDefinition;

	// Act
	var question = await service.GenerateQuestionAsync("testuser", direction);

	// Assert
	Assert.Equal(direction, question.Direction);
	Assert.NotEmpty(question.Term);
	Assert.Equal(question.Term, question.Prompt);
	Assert.Equal(question.Definition, question.CorrectAnswer);
	Assert.Contains(question.Definition, question.AllAnswerOptions);
}

[Fact]
public async Task GenerateQuestionAsync_WithDefinitionToTermDirection_ReturnsCorrectFormat()
{
	// Similar test for DefinitionToTerm direction
}

[Fact]
public async Task GenerateQuestionAsync_WithNullDirection_RandomizesDirection()
{
	// Generate 100 questions, verify roughly 50/50 split
}

[Fact]
public async Task GenerateDistractors_ForTermToDefinition_ReturnsDefinitions()
{
	// Verify distractors are definitions, not terms
}

[Fact]
public async Task GenerateDistractors_ForDefinitionToTerm_ReturnsTerms()
{
	// Verify distractors are terms, not definitions
}
```

### 8.2 Integration Tests

**File:** `Tests/Integration/QuizFlowTests.cs` (NEW TESTS)

```csharp
[Fact]
public async Task CompleteQuiz_WithBidirectionalQuestions_Succeeds()
{
	// Start quiz, answer multiple questions of both directions, complete quiz
}

[Fact]
public async Task GradedQuiz_WithBidirectionalQuestions_CalculatesScoreCorrectly()
{
	// Complete graded quiz with mixed directions, verify score calculation
}
```

---

## 9. Deployment Checklist

### Backend Deployment Steps

- [ ] Create `QuestionDirection` enum
- [ ] Update `Question` model with new properties
- [ ] Update `IQuestionGeneratorService` interface
- [ ] Implement enhanced question generation logic
- [ ] Update `QuizController` for direction support
- [ ] Update `GradedQuizController` for direction support
- [ ] Update view models
- [ ] Write unit tests for new logic
- [ ] Write integration tests
- [ ] Code review
- [ ] Merge to main branch

---

## 10. Sign-Off

**Backend Architect:** ✅ Approved  
**Date:** 2025-01-27  
**Next Step:** Frontend Design Document

---

## References

- Design Summary: `.github/design/feature-quiz-bidirectional-questions-design-summary.md`
- Feature Spec: `.github/design/feature-quiz-bidirectional-questions-spec.md`
- Frontend Design: `.github/design/feature-quiz-bidirectional-questions-frontend-design.md` (next)
