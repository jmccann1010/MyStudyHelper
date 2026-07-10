# Graded Quiz - Backend Design Document

**Feature:** Graded Quiz  
**Branch:** `feature/graded-quiz`  
**Status:** Design Ready for Engineering  
**Last Updated:** 2025-01-25  

---

## Executive Summary

The Graded Quiz backend will implement a scoring and session management system that allows users to take a configurable multiple-choice quiz with real-time score tracking. The implementation reuses existing question generation infrastructure (`IQuestionGeneratorService`, `IMarkdownParserService`) and adds a new service layer for quiz session lifecycle and score calculation.

**Key Architectural Decisions:**
- Stateless question generation (existing services)
- In-memory session management (ASP.NET Core TempData/session)
- Service-oriented architecture with dependency injection
- No database persistence (quiz scores are ephemeral; not saved)
- Synchronous request-response pattern

---

## System Architecture

### High-Level Flow

```
User Request
	↓
GradedQuizController
	├─ GET /Setup → Display question count selection
	├─ POST /StartQuiz → Initialize session, redirect to Question
	├─ GET /Question → Retrieve current question
	├─ POST /SubmitAnswer → Validate answer, update score, advance
	├─ GET /Results → Calculate final score and display report
	└─ GET /RetakeQuiz → Clear session, redirect to Setup

	↓ (via dependency injection)

IGradedQuizService (implementation: GradedQuizService)
	├─ StartQuizAsync() → Create session, generate question list
	├─ SubmitAnswerAsync() → Validate answer, update score
	├─ GetCurrentScoreAsync() → Return score state
	├─ GetQuizProgressAsync() → Return position in quiz
	└─ GetQuizSessionAsync() → Retrieve full session

	↓ (via dependency injection)

IQuestionGeneratorService (existing)
	└─ GenerateQuestion() → Generate single question from sections

IMarkdownParserService (existing)
	└─ ParseMarkdownFilesAsync() → Parse sections for questions
```

---

## Service Layer Design

### IGradedQuizService Interface

```csharp
namespace StudyHelper.Services;

public interface IGradedQuizService
{
	/// <summary>
	/// Starts a new graded quiz session with specified question count.
	/// </summary>
	/// <param name="questionCount">Number of questions in the quiz (1-50).</param>
	/// <param name="username">Authenticated username for session scoping.</param>
	/// <returns>Quiz session ID for future references.</returns>
	Task<string> StartQuizAsync(int questionCount, string username);

	/// <summary>
	/// Submits an answer for the current question and advances quiz state.
	/// </summary>
	/// <param name="quizSessionId">Session ID.</param>
	/// <param name="selectedAnswerIndex">0-based index of selected answer (0-3).</param>
	/// <returns>Result containing validation status and updated progress.</returns>
	Task<SubmitAnswerResult> SubmitAnswerAsync(string quizSessionId, int selectedAnswerIndex);

	/// <summary>
	/// Gets current score state without modifying session.
	/// </summary>
	Task<QuizScore> GetCurrentScoreAsync(string quizSessionId);

	/// <summary>
	/// Gets quiz progress and current question.
	/// </summary>
	Task<QuizProgress> GetQuizProgressAsync(string quizSessionId);

	/// <summary>
	/// Retrieves the full quiz session for rendering.
	/// </summary>
	Task<GradedQuizSession?> GetQuizSessionAsync(string quizSessionId);

	/// <summary>
	/// Finalizes quiz session and returns final score.
	/// </summary>
	Task<QuizScore> FinishQuizAsync(string quizSessionId);

	/// <summary>
	/// Clears quiz session (e.g., for retake).
	/// </summary>
	Task<bool> ClearQuizSessionAsync(string quizSessionId);
}
```

---

## Data Models

### GradedQuizSession
```csharp
namespace StudyHelper.Models;

public class GradedQuizSession
{
	/// <summary>
	/// Unique session identifier.
	/// </summary>
	public string SessionId { get; set; } = Guid.NewGuid().ToString();

	/// <summary>
	/// Authenticated username (for multi-user scoping).
	/// </summary>
	public string Username { get; set; } = string.Empty;

	/// <summary>
	/// Total questions in this quiz.
	/// </summary>
	public int TotalQuestions { get; set; }

	/// <summary>
	/// Current question index (0-based).
	/// </summary>
	public int CurrentQuestionIndex { get; set; }

	/// <summary>
	/// List of generated questions for this session.
	/// </summary>
	public List<QuizQuestion> Questions { get; set; } = new();

	/// <summary>
	/// User's answer selections (index per question, or -1 if unanswered).
	/// </summary>
	public Dictionary<int, int> UserAnswers { get; set; } = new();

	/// <summary>
	/// Session creation timestamp.
	/// </summary>
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Last activity timestamp (for timeout detection).
	/// </summary>
	public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Whether quiz is complete.
	/// </summary>
	public bool IsComplete { get; set; }
}
```

### QuizScore
```csharp
public class QuizScore
{
	public int CorrectCount { get; set; }
	public int IncorrectCount { get; set; }
	public int TotalQuestions { get; set; }

	public decimal Percentage => TotalQuestions > 0 
		? Math.Round((decimal)CorrectCount / TotalQuestions * 100, 2) 
		: 0;

	public string PerformanceRating => Percentage switch
	{
		>= 90 => "Excellent",
		>= 80 => "Good",
		>= 70 => "Fair",
		>= 60 => "Poor",
		_ => "Needs Improvement"
	};
}
```

### QuizProgress
```csharp
public class QuizProgress
{
	public int CurrentQuestionNumber { get; set; }  // 1-based for display
	public int TotalQuestions { get; set; }
	public int CorrectCount { get; set; }
	public int IncorrectCount { get; set; }
	public decimal ProgressPercentage => TotalQuestions > 0 
		? (decimal)(CurrentQuestionNumber - 1) / TotalQuestions * 100 
		: 0;
}
```

### SubmitAnswerResult
```csharp
public class SubmitAnswerResult
{
	public bool IsValid { get; set; }
	public bool IsCorrect { get; set; }
	public string? ErrorMessage { get; set; }
	public bool IsLastQuestion { get; set; }
	public QuizProgress UpdatedProgress { get; set; } = new();
}
```

---

## GradedQuizService Implementation Details

### Initialization (StartQuizAsync)

1. **Validate input:**
   - Question count between 1 and 50
   - Username is not null/empty

2. **Parse markdown:**
   - Call `IMarkdownParserService.ParseMarkdownFilesAsync(username)`
   - Reuses user-uploaded study materials if available

3. **Generate question list:**
   - Call `IQuestionGeneratorService.GenerateQuestion()` N times
   - Each call generates a unique random question
   - Store all questions in session

4. **Create session:**
   - Generate session ID (GUID)
   - Initialize UserAnswers dictionary (empty)
   - Set CurrentQuestionIndex = 0
   - Store in cache (key = `"graded-quiz-{sessionId}"`)

5. **Return session ID** to controller for storage in TempData

**Session Storage Strategy:**
- Use `IMemoryCache` with 30-minute expiration
- Alternative: ASP.NET Core session (server-side state)
- Cache key: `$"graded-quiz-{sessionId}"`

---

### Answer Submission (SubmitAnswerAsync)

1. **Retrieve session:**
   - Get from cache by session ID
   - Validate session exists and not expired

2. **Validate answer:**
   - Check 0 ≤ selectedAnswerIndex ≤ 3
   - Check if question already answered (prevent duplicate scoring)

3. **Score calculation:**
   - Get current question from Questions[CurrentQuestionIndex]
   - Compare selectedAnswerIndex with question.CorrectAnswerIndex
   - Increment correct or incorrect counter
   - Store answer in UserAnswers[CurrentQuestionIndex]

4. **Advance state:**
   - CurrentQuestionIndex++
   - LastActivityAt = now
   - If CurrentQuestionIndex ≥ TotalQuestions, set IsComplete = true

5. **Return result:**
   - Include updated score/progress
   - Flag if last question

**Error Handling:**
- Session not found → throw InvalidOperationException
- Session expired → throw InvalidOperationException
- Invalid answer index → return SubmitAnswerResult with IsValid=false

---

### Session Retrieval (GetQuizSessionAsync)

- Retrieve from cache
- Update LastActivityAt
- Return full session or null if not found/expired

---

### Quiz Finalization (FinishQuizAsync)

1. Retrieve session
2. Mark IsComplete = true
3. Calculate final QuizScore
4. Return score (session remains in cache for Results page display)

---

## Controller Integration

### GradedQuizController

```csharp
[Authorize]
public class GradedQuizController : Controller
{
	private readonly IGradedQuizService _quizService;
	private readonly ILogger<GradedQuizController> _logger;

	public GradedQuizController(IGradedQuizService quizService, ILogger<GradedQuizController> logger)
	{
		_quizService = quizService;
		_logger = logger;
	}

	[HttpGet]
	public IActionResult Setup() => View();

	[HttpPost]
	public async Task<IActionResult> StartQuiz(int questionCount)
	{
		if (questionCount < 1 || questionCount > 50)
			return View("Setup", new { Error = "Question count must be 1-50." });

		var username = User.Identity?.Name;
		var sessionId = await _quizService.StartQuizAsync(questionCount, username);
		TempData["QuizSessionId"] = sessionId;
		return RedirectToAction(nameof(Question));
	}

	[HttpGet]
	public async Task<IActionResult> Question()
	{
		var sessionId = TempData["QuizSessionId"] as string;
		if (string.IsNullOrEmpty(sessionId))
			return RedirectToAction(nameof(Setup));

		var session = await _quizService.GetQuizSessionAsync(sessionId);
		if (session == null || session.IsComplete)
			return RedirectToAction(nameof(Results));

		var progress = await _quizService.GetQuizProgressAsync(sessionId);
		var question = session.Questions[session.CurrentQuestionIndex];

		var viewModel = new QuizQuestionViewModel
		{
			QuestionNumber = session.CurrentQuestionIndex + 1,
			TotalQuestions = session.TotalQuestions,
			QuestionText = question.QuestionText,
			AnswerOptions = question.AnswerOptions,
			CorrectCount = progress.CorrectCount,
			IncorrectCount = progress.IncorrectCount
		};

		return View(viewModel);
	}

	[HttpPost]
	public async Task<IActionResult> SubmitAnswer(int selectedAnswerIndex)
	{
		var sessionId = TempData["QuizSessionId"] as string;
		if (string.IsNullOrEmpty(sessionId))
			return RedirectToAction(nameof(Setup));

		var result = await _quizService.SubmitAnswerAsync(sessionId, selectedAnswerIndex);
		if (!result.IsValid)
			return RedirectToAction(nameof(Question));

		TempData["QuizSessionId"] = sessionId;

		if (result.IsLastQuestion)
		{
			await _quizService.FinishQuizAsync(sessionId);
			return RedirectToAction(nameof(Results));
		}

		return RedirectToAction(nameof(Question));
	}

	[HttpGet]
	public async Task<IActionResult> Results()
	{
		var sessionId = TempData["QuizSessionId"] as string;
		if (string.IsNullOrEmpty(sessionId))
			return RedirectToAction(nameof(Setup));

		var session = await _quizService.GetQuizSessionAsync(sessionId);
		if (session == null || !session.IsComplete)
			return RedirectToAction(nameof(Setup));

		var score = await _quizService.GetCurrentScoreAsync(sessionId);
		var viewModel = new QuizResultViewModel
		{
			CorrectCount = score.CorrectCount,
			IncorrectCount = score.IncorrectCount,
			TotalQuestions = score.TotalQuestions,
			Percentage = score.Percentage,
			PerformanceRating = score.PerformanceRating,
			Questions = session.Questions,
			UserAnswers = session.UserAnswers
		};

		return View(viewModel);
	}

	[HttpGet]
	public async Task<IActionResult> RetakeQuiz()
	{
		var sessionId = TempData["QuizSessionId"] as string;
		if (!string.IsNullOrEmpty(sessionId))
			await _quizService.ClearQuizSessionAsync(sessionId);

		return RedirectToAction(nameof(Setup));
	}
}
```

---

## Error Handling & Edge Cases

### Session Timeout
- **Scenario:** User idle > 30 minutes
- **Handling:** Cache expires; controller detects null session; redirects to Setup
- **Message:** "Your session expired. Please start a new quiz."

### Invalid Session State
- **Scenario:** User somehow accesses Question page with no session
- **Handling:** Check TempData; redirect to Setup
- **Message:** "No active quiz session. Please start a new quiz."

### Duplicate Answer Submission
- **Scenario:** User submits same answer twice
- **Handling:** Check UserAnswers dictionary; don't re-score
- **Result:** Treated as navigation, not answer update

### Invalid Answer Index
- **Scenario:** User posts answer index outside 0-3
- **Handling:** Return error result; re-render Question page
- **Message:** "Invalid answer selection. Please select an option."

### Out-of-Bounds Question Navigation
- **Scenario:** User tries to go back before question 0 or forward after last question
- **Handling:** Validate in service; return error
- **Message:** "Cannot navigate outside quiz bounds."

---

## Performance Considerations

- **Memory Usage:** Quiz sessions stored in-memory cache; assumes <100 concurrent users
- **Question Generation:** Generated once per session and cached; not re-generated per request
- **Scaling:** For high concurrency, migrate to distributed cache (Redis) or database
- **Session Size:** Typical session ~50KB (N questions + answer dict + metadata)

---

## Security Considerations

1. **Authentication:** `[Authorize]` attribute on all actions
2. **Session Isolation:** Session ID is GUID; impossible to guess
3. **Input Validation:** Question count 1-50; answer index 0-3
4. **XSS Prevention:** Razor templating auto-escapes by default
5. **CSRF:** `[ValidateAntiForgeryToken]` on POST actions
6. **No Database Writes:** Quiz scores not persisted; no injection vectors

---

## Testing Strategy

### Unit Tests (GradedQuizServiceTests)
- ✅ Valid quiz initialization (question count, session ID generation)
- ✅ Score calculation (correct/incorrect answers)
- ✅ Duplicate answer handling (no re-scoring)
- ✅ Quiz completion detection (IsComplete flag)
- ✅ Session timeout/expiration
- ✅ Edge cases (1 question, 50 questions, invalid counts)

### Integration Tests (GradedQuizControllerTests)
- ✅ Full quiz flow (Setup → Questions → Results)
- ✅ Navigation (forward/backward between questions)
- ✅ Session persistence across requests
- ✅ Error handling (expired session, invalid input)
- ✅ TempData storage and retrieval

---

## Dependencies & Registration

### In Program.cs
```csharp
// Add after existing service registrations
services.AddMemoryCache();
services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

services.AddScoped<IGradedQuizService, GradedQuizService>();
```

### Constructor Injection (in GradedQuizService)
```csharp
private readonly IQuestionGeneratorService _questionGeneratorService;
private readonly IMarkdownParserService _markdownParserService;
private readonly IMemoryCache _cache;
private readonly ILogger<GradedQuizService> _logger;

public GradedQuizService(
	IQuestionGeneratorService questionGeneratorService,
	IMarkdownParserService markdownParserService,
	IMemoryCache cache,
	ILogger<GradedQuizService> logger)
{
	// Assign and validate
}
```

---

## Files to Create

1. **Services/IGradedQuizService.cs** — Interface definition
2. **Services/GradedQuizService.cs** — Service implementation
3. **Models/GradedQuizSession.cs** — Session model
4. **Models/QuizScore.cs** — Score model
5. **Models/QuizProgress.cs** — Progress model
6. **Models/SubmitAnswerResult.cs** — Answer result model
7. **Controllers/GradedQuizController.cs** — Controller
8. **ViewModels/QuizQuestionViewModel.cs** — Setup/Question view model
9. **ViewModels/QuizResultViewModel.cs** — Results view model

---

## Definition of Done (Backend)

- ✅ All service methods implemented and tested
- ✅ All models created with validation
- ✅ Controller actions route correctly
- ✅ Error handling in place for all edge cases
- ✅ Unit tests ≥80% coverage
- ✅ Integration tests verify full flow
- ✅ No external dependencies added (uses existing services)
- ✅ Session management secure
- ✅ Code review approved

---

## Next Steps

1. **Frontend Design Review** — Proceed to frontend design document
2. **Engineering Handoff** — Backend + Frontend engineers implement from these designs
3. **Code Review** — Review implementation against this design
4. **QA Testing** — Verify functionality and coverage

