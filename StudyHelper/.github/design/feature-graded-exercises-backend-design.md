# Graded Exercises - Backend Design Document

**Feature:** Graded Exercises  
**Branch:** `feature/graded-exercises`  
**Status:** Design Ready for Engineering  
**Last Updated:** 2025-01-27  

---

## Executive Summary

The Graded Exercises backend implements a scoring and session management system for equation-based calculation problems. It reuses existing exercise generation infrastructure (`IExerciseProblemGeneratorService`, `IEquationParserService`) and follows the proven patterns from the Graded Quiz implementation.

**Key Architectural Decisions:**
- Reuse existing exercise problem generation (no new generators)
- In-memory session management (IMemoryCache + TempData)
- Service-oriented architecture with dependency injection
- No database persistence (ephemeral scoring only)
- Decimal validation with ±0.01 tolerance for rounding

---

## System Architecture

### High-Level Flow

```
User Request
	↓
GradedExerciseController
	├─ GET  /GradedExercise/Setup          → Display problem count selection
	├─ POST /GradedExercise/StartExercise  → Initialize session, redirect
	├─ GET  /GradedExercise/Problem        → Retrieve current problem
	├─ POST /GradedExercise/SubmitAnswer   → Validate, score, advance
	├─ GET  /GradedExercise/Results        → Calculate final score
	└─ GET  /GradedExercise/RetakeExercise → Clear session, restart

	↓ (via dependency injection)

IGradedExerciseService (implementation: GradedExerciseService)
	├─ StartExerciseAsync()       → Create session, generate problems
	├─ SubmitAnswerAsync()        → Validate answer, update score
	├─ GetCurrentScoreAsync()     → Return score state
	├─ GetExerciseProgressAsync() → Return position in exercise
	├─ GetExerciseSessionAsync()  → Retrieve full session
	├─ FinishExerciseAsync()      → Finalize session, calculate score
	└─ ClearExerciseSessionAsync()→ Remove session from cache

	↓ (via dependency injection)

IExerciseProblemGeneratorService (existing)
	├─ GenerateProblem()    → Generate single problem from equations
	└─ ValidateAnswer()     → Check answer with tolerance

IEquationParserService (existing)
	└─ ParseEquationFileAsync() → Parse equations from file

IUserStudyMaterialService (existing)
	└─ GetStudyMaterialPathAsync() → Get user's equation file path
```

---

## Service Layer Design

### IGradedExerciseService Interface

**Namespace:** `StudyHelper.Services`  
**File:** `Services/IGradedExerciseService.cs`

```csharp
namespace StudyHelper.Services;

/// <summary>
/// Service for managing graded exercise sessions, score tracking, and session lifecycle.
/// </summary>
public interface IGradedExerciseService
{
	/// <summary>
	/// Starts a new graded exercise session with specified problem count.
	/// </summary>
	/// <param name="problemCount">Number of problems in the session (1-50).</param>
	/// <param name="username">Authenticated username for session scoping.</param>
	/// <returns>Exercise session ID for future references.</returns>
	/// <exception cref="ArgumentException">Thrown when problemCount is outside 1-50 range or username is null/empty.</exception>
	/// <exception cref="InvalidOperationException">Thrown when unable to generate problems (no equations, insufficient equations, etc.).</exception>
	Task<string> StartExerciseAsync(int problemCount, string username);

	/// <summary>
	/// Submits an answer for the current problem and advances exercise state.
	/// </summary>
	/// <param name="sessionId">Session ID.</param>
	/// <param name="userAnswer">User's submitted answer (decimal).</param>
	/// <returns>Result containing validation status, correctness, and updated progress.</returns>
	/// <exception cref="InvalidOperationException">Thrown when session not found or expired.</exception>
	/// <exception cref="ArgumentException">Thrown when answer format is invalid.</exception>
	Task<SubmitAnswerResult> SubmitAnswerAsync(string sessionId, string userAnswer);

	/// <summary>
	/// Gets current score state without modifying session.
	/// </summary>
	/// <param name="sessionId">Session ID.</param>
	/// <returns>Current exercise score.</returns>
	/// <exception cref="InvalidOperationException">Thrown when session not found.</exception>
	Task<ExerciseScore> GetCurrentScoreAsync(string sessionId);

	/// <summary>
	/// Gets exercise progress and current problem information.
	/// </summary>
	/// <param name="sessionId">Session ID.</param>
	/// <returns>Current exercise progress.</returns>
	/// <exception cref="InvalidOperationException">Thrown when session not found.</exception>
	Task<ExerciseProgress> GetExerciseProgressAsync(string sessionId);

	/// <summary>
	/// Retrieves the full exercise session for rendering.
	/// </summary>
	/// <param name="sessionId">Session ID.</param>
	/// <returns>Full exercise session or null if not found/expired.</returns>
	Task<GradedExerciseSession?> GetExerciseSessionAsync(string sessionId);

	/// <summary>
	/// Finalizes exercise session and returns final score.
	/// </summary>
	/// <param name="sessionId">Session ID.</param>
	/// <returns>Final exercise score with performance rating.</returns>
	/// <exception cref="InvalidOperationException">Thrown when session not found.</exception>
	Task<ExerciseScore> FinishExerciseAsync(string sessionId);

	/// <summary>
	/// Clears exercise session (e.g., for retake).
	/// </summary>
	/// <param name="sessionId">Session ID.</param>
	/// <returns>True if session was cleared, false if not found.</returns>
	Task<bool> ClearExerciseSessionAsync(string sessionId);
}
```

---

### GradedExerciseService Implementation

**Namespace:** `StudyHelper.Services`  
**File:** `Services/GradedExerciseService.cs`

```csharp
namespace StudyHelper.Services;

/// <summary>
/// Service for managing graded exercise sessions, score tracking, and session lifecycle.
/// </summary>
public class GradedExerciseService : IGradedExerciseService
{
	private readonly IExerciseProblemGeneratorService _problemGeneratorService;
	private readonly IEquationParserService _equationParserService;
	private readonly IUserStudyMaterialService _studyMaterialService;
	private readonly IMemoryCache _cache;
	private readonly ILogger<GradedExerciseService> _logger;

	private const int SessionTimeoutMinutes = 30;
	private const string CacheKeyPrefix = "graded-exercise-";

	public GradedExerciseService(
		IExerciseProblemGeneratorService problemGeneratorService,
		IEquationParserService equationParserService,
		IUserStudyMaterialService studyMaterialService,
		IMemoryCache cache,
		ILogger<GradedExerciseService> logger)
	{
		_problemGeneratorService = problemGeneratorService ?? throw new ArgumentNullException(nameof(problemGeneratorService));
		_equationParserService = equationParserService ?? throw new ArgumentNullException(nameof(equationParserService));
		_studyMaterialService = studyMaterialService ?? throw new ArgumentNullException(nameof(studyMaterialService));
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	// Implementation methods below...
}
```

#### StartExerciseAsync Implementation

```csharp
public async Task<string> StartExerciseAsync(int problemCount, string username)
{
	// Validate inputs
	if (problemCount < 1 || problemCount > 50)
	{
		_logger.LogWarning("Invalid problem count: {ProblemCount}", problemCount);
		throw new ArgumentException("Problem count must be between 1 and 50.", nameof(problemCount));
	}

	if (string.IsNullOrWhiteSpace(username))
	{
		_logger.LogWarning("Invalid username for exercise start");
		throw new ArgumentException("Username cannot be null or empty.", nameof(username));
	}

	try
	{
		_logger.LogInformation("Starting graded exercise for user {Username} with {ProblemCount} problems", 
			username, problemCount);

		// Get user's equation file path
		var equationFilePath = await _studyMaterialService.GetStudyMaterialPathAsync(
			username, 
			StudyMaterialType.Equations);

		if (string.IsNullOrEmpty(equationFilePath) || !File.Exists(equationFilePath))
		{
			_logger.LogWarning("No equation file found for user {Username}", username);
			throw new InvalidOperationException("No equation file found. Please upload an equations file first.");
		}

		// Parse equations from file
		var equations = await _equationParserService.ParseEquationFileAsync(equationFilePath);

		if (equations == null || equations.Count == 0)
		{
			_logger.LogWarning("No equations parsed from file for user {Username}", username);
			throw new InvalidOperationException("No valid equations found in your equations file.");
		}

		if (equations.Count < problemCount)
		{
			_logger.LogWarning("Insufficient equations: {EquationCount} available, {ProblemCount} requested", 
				equations.Count, problemCount);
			throw new InvalidOperationException(
				$"Not enough equations. You have {equations.Count} equations but requested {problemCount} problems.");
		}

		// Generate problems
		var problems = new List<ExerciseProblem>();
		for (int i = 0; i < problemCount; i++)
		{
			var problem = _problemGeneratorService.GenerateProblem(equations);
			problems.Add(problem);
		}

		// Create session
		var session = new GradedExerciseSession
		{
			SessionId = Guid.NewGuid().ToString(),
			Username = username,
			TotalProblems = problemCount,
			CurrentProblemIndex = 0,
			Problems = problems,
			UserAnswers = new Dictionary<int, decimal>(),
			CreatedAt = DateTime.UtcNow,
			LastActivityAt = DateTime.UtcNow,
			IsComplete = false
		};

		// Store in cache
		var cacheKey = $"{CacheKeyPrefix}{session.SessionId}";
		var cacheOptions = new MemoryCacheEntryOptions()
			.SetSlidingExpiration(TimeSpan.FromMinutes(SessionTimeoutMinutes));

		_cache.Set(cacheKey, session, cacheOptions);

		_logger.LogInformation("Exercise session created: {SessionId} for user {Username}", 
			session.SessionId, username);

		return session.SessionId;
	}
	catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
	{
		_logger.LogError(ex, "Unexpected error starting exercise for user {Username}", username);
		throw new InvalidOperationException("Unable to start exercise. Please try again.", ex);
	}
}
```

#### SubmitAnswerAsync Implementation

```csharp
public async Task<SubmitAnswerResult> SubmitAnswerAsync(string sessionId, string userAnswer)
{
	ArgumentNullException.ThrowIfNull(sessionId);
	ArgumentNullException.ThrowIfNull(userAnswer);

	var session = await GetExerciseSessionAsync(sessionId);
	if (session == null)
	{
		_logger.LogWarning("Exercise session not found: {SessionId}", sessionId);
		throw new InvalidOperationException("Exercise session not found or expired.");
	}

	if (session.IsComplete)
	{
		_logger.LogWarning("Attempt to submit answer to completed session: {SessionId}", sessionId);
		throw new InvalidOperationException("Exercise session is already complete.");
	}

	if (session.CurrentProblemIndex >= session.TotalProblems)
	{
		_logger.LogWarning("Problem index out of range: {Index}/{Total}", 
			session.CurrentProblemIndex, session.TotalProblems);
		throw new InvalidOperationException("No more problems in this session.");
	}

	// Parse user answer
	var cleanedAnswer = userAnswer.Replace("$", "")
								  .Replace(",", "")
								  .Replace("%", "")
								  .Trim();

	if (!decimal.TryParse(cleanedAnswer, out decimal parsedAnswer))
	{
		_logger.LogWarning("Invalid answer format from user {Username}: {Answer}", 
			session.Username, userAnswer);
		return new SubmitAnswerResult
		{
			IsValid = false,
			IsCorrect = false,
			IsLastProblem = false,
			ErrorMessage = "Please enter a valid number.",
			CorrectAnswer = 0
		};
	}

	// Get current problem and validate
	var currentProblem = session.Problems[session.CurrentProblemIndex];
	var validationResult = _problemGeneratorService.ValidateAnswer(currentProblem, parsedAnswer);

	// Store user answer
	session.UserAnswers[session.CurrentProblemIndex] = parsedAnswer;

	// Update session state
	session.LastActivityAt = DateTime.UtcNow;
	var isLastProblem = session.CurrentProblemIndex == session.TotalProblems - 1;

	if (!isLastProblem)
	{
		session.CurrentProblemIndex++;
	}

	// Update cache
	var cacheKey = $"{CacheKeyPrefix}{sessionId}";
	var cacheOptions = new MemoryCacheEntryOptions()
		.SetSlidingExpiration(TimeSpan.FromMinutes(SessionTimeoutMinutes));
	_cache.Set(cacheKey, session, cacheOptions);

	_logger.LogInformation("Answer submitted for session {SessionId}, problem {ProblemIndex}, correct: {IsCorrect}",
		sessionId, session.CurrentProblemIndex - 1, validationResult.IsCorrect);

	return new SubmitAnswerResult
	{
		IsValid = true,
		IsCorrect = validationResult.IsCorrect,
		IsLastProblem = isLastProblem,
		ErrorMessage = string.Empty,
		CorrectAnswer = currentProblem.CorrectAnswer
	};
}
```

#### GetExerciseSessionAsync Implementation

```csharp
public async Task<GradedExerciseSession?> GetExerciseSessionAsync(string sessionId)
{
	ArgumentNullException.ThrowIfNull(sessionId);

	var cacheKey = $"{CacheKeyPrefix}{sessionId}";
	if (_cache.TryGetValue(cacheKey, out GradedExerciseSession? session))
	{
		_logger.LogDebug("Exercise session retrieved from cache: {SessionId}", sessionId);
		return session;
	}

	_logger.LogDebug("Exercise session not found in cache: {SessionId}", sessionId);
	return null;
}
```

#### GetCurrentScoreAsync Implementation

```csharp
public async Task<ExerciseScore> GetCurrentScoreAsync(string sessionId)
{
	var session = await GetExerciseSessionAsync(sessionId);
	if (session == null)
	{
		_logger.LogWarning("Session not found for score retrieval: {SessionId}", sessionId);
		throw new InvalidOperationException("Exercise session not found.");
	}

	int correctCount = 0;
	int incorrectCount = 0;

	foreach (var (problemIndex, userAnswer) in session.UserAnswers)
	{
		var problem = session.Problems[problemIndex];
		var result = _problemGeneratorService.ValidateAnswer(problem, userAnswer);

		if (result.IsCorrect)
			correctCount++;
		else
			incorrectCount++;
	}

	var score = new ExerciseScore
	{
		CorrectCount = correctCount,
		IncorrectCount = incorrectCount,
		TotalProblems = session.TotalProblems,
		PerformanceRating = CalculatePerformanceRating(correctCount, session.TotalProblems)
	};

	_logger.LogDebug("Score calculated for session {SessionId}: {CorrectCount}/{TotalProblems}",
		sessionId, correctCount, session.TotalProblems);

	return score;
}
```

#### GetExerciseProgressAsync Implementation

```csharp
public async Task<ExerciseProgress> GetExerciseProgressAsync(string sessionId)
{
	var session = await GetExerciseSessionAsync(sessionId);
	if (session == null)
	{
		_logger.LogWarning("Session not found for progress retrieval: {SessionId}", sessionId);
		throw new InvalidOperationException("Exercise session not found.");
	}

	var score = await GetCurrentScoreAsync(sessionId);

	return new ExerciseProgress
	{
		CurrentProblemIndex = session.CurrentProblemIndex,
		TotalProblems = session.TotalProblems,
		CorrectCount = score.CorrectCount,
		IncorrectCount = score.IncorrectCount,
		IsComplete = session.IsComplete
	};
}
```

#### FinishExerciseAsync Implementation

```csharp
public async Task<ExerciseScore> FinishExerciseAsync(string sessionId)
{
	var session = await GetExerciseSessionAsync(sessionId);
	if (session == null)
	{
		_logger.LogWarning("Session not found for finish: {SessionId}", sessionId);
		throw new InvalidOperationException("Exercise session not found.");
	}

	session.IsComplete = true;
	session.LastActivityAt = DateTime.UtcNow;

	// Update cache
	var cacheKey = $"{CacheKeyPrefix}{sessionId}";
	var cacheOptions = new MemoryCacheEntryOptions()
		.SetSlidingExpiration(TimeSpan.FromMinutes(SessionTimeoutMinutes));
	_cache.Set(cacheKey, session, cacheOptions);

	var finalScore = await GetCurrentScoreAsync(sessionId);

	_logger.LogInformation("Exercise session finished: {SessionId}, score: {CorrectCount}/{TotalProblems} ({Percentage}%)",
		sessionId, finalScore.CorrectCount, finalScore.TotalProblems, finalScore.Percentage);

	return finalScore;
}
```

#### ClearExerciseSessionAsync Implementation

```csharp
public async Task<bool> ClearExerciseSessionAsync(string sessionId)
{
	ArgumentNullException.ThrowIfNull(sessionId);

	var cacheKey = $"{CacheKeyPrefix}{sessionId}";
	if (_cache.TryGetValue(cacheKey, out GradedExerciseSession? _))
	{
		_cache.Remove(cacheKey);
		_logger.LogInformation("Exercise session cleared: {SessionId}", sessionId);
		return true;
	}

	_logger.LogDebug("Exercise session not found for clearing: {SessionId}", sessionId);
	return false;
}
```

#### Helper Method: CalculatePerformanceRating

```csharp
private static string CalculatePerformanceRating(int correctCount, int totalProblems)
{
	if (totalProblems == 0) return "N/A";

	var percentage = (decimal)correctCount / totalProblems * 100;

	return percentage switch
	{
		>= 90 => "Excellent",
		>= 80 => "Good",
		>= 70 => "Fair",
		>= 60 => "Poor",
		_ => "Needs Improvement"
	};
}
```

---

## Model Definitions

### GradedExerciseSession

**Namespace:** `StudyHelper.Models`  
**File:** `Models/GradedExerciseSession.cs`

```csharp
namespace StudyHelper.Models;

/// <summary>
/// Represents an active graded exercise session with user progress and scoring.
/// </summary>
public class GradedExerciseSession
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
	/// Total problems in this exercise session.
	/// </summary>
	public int TotalProblems { get; set; }

	/// <summary>
	/// Current problem index (0-based).
	/// </summary>
	public int CurrentProblemIndex { get; set; }

	/// <summary>
	/// List of generated problems for this session.
	/// </summary>
	public List<ExerciseProblem> Problems { get; set; } = new();

	/// <summary>
	/// User's answer submissions (problem index to answer mapping).
	/// </summary>
	public Dictionary<int, decimal> UserAnswers { get; set; } = new();

	/// <summary>
	/// Session creation timestamp.
	/// </summary>
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Last activity timestamp (for timeout detection).
	/// </summary>
	public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Whether exercise session is complete.
	/// </summary>
	public bool IsComplete { get; set; }
}
```

### ExerciseScore

**Namespace:** `StudyHelper.Models`  
**File:** `Models/ExerciseScore.cs`

```csharp
namespace StudyHelper.Models;

/// <summary>
/// Represents the score and performance metrics for a graded exercise session.
/// </summary>
public class ExerciseScore
{
	/// <summary>
	/// Number of problems answered correctly.
	/// </summary>
	public int CorrectCount { get; set; }

	/// <summary>
	/// Number of problems answered incorrectly.
	/// </summary>
	public int IncorrectCount { get; set; }

	/// <summary>
	/// Total number of problems in the exercise.
	/// </summary>
	public int TotalProblems { get; set; }

	/// <summary>
	/// Score as a percentage (0-100).
	/// </summary>
	public decimal Percentage => TotalProblems > 0 
		? Math.Round((decimal)CorrectCount / TotalProblems * 100, 2) 
		: 0;

	/// <summary>
	/// Performance rating based on percentage.
	/// </summary>
	public string PerformanceRating { get; set; } = string.Empty;
}
```

### ExerciseProgress

**Namespace:** `StudyHelper.Models`  
**File:** `Models/ExerciseProgress.cs`

```csharp
namespace StudyHelper.Models;

/// <summary>
/// Represents the current progress through a graded exercise session.
/// </summary>
public class ExerciseProgress
{
	/// <summary>
	/// Current problem index (0-based).
	/// </summary>
	public int CurrentProblemIndex { get; set; }

	/// <summary>
	/// Total problems in the session.
	/// </summary>
	public int TotalProblems { get; set; }

	/// <summary>
	/// Number of problems answered correctly so far.
	/// </summary>
	public int CorrectCount { get; set; }

	/// <summary>
	/// Number of problems answered incorrectly so far.
	/// </summary>
	public int IncorrectCount { get; set; }

	/// <summary>
	/// Whether the session is complete.
	/// </summary>
	public bool IsComplete { get; set; }

	/// <summary>
	/// Progress as a percentage (0-100).
	/// </summary>
	public decimal ProgressPercentage => TotalProblems > 0 
		? Math.Round((decimal)CurrentProblemIndex / TotalProblems * 100, 2) 
		: 0;
}
```

### SubmitAnswerResult (Enhanced)

**Namespace:** `StudyHelper.Models`  
**File:** `Models/SubmitAnswerResult.cs` (modify existing)

```csharp
namespace StudyHelper.Models;

/// <summary>
/// Result of submitting an answer in a graded exercise or quiz.
/// </summary>
public class SubmitAnswerResult
{
	/// <summary>
	/// Whether the answer input is valid (parseable, within constraints).
	/// </summary>
	public bool IsValid { get; set; }

	/// <summary>
	/// Whether the answer is correct (only meaningful if IsValid is true).
	/// </summary>
	public bool IsCorrect { get; set; }

	/// <summary>
	/// Whether this is the last problem/question in the session.
	/// </summary>
	public bool IsLastProblem { get; set; }

	/// <summary>
	/// Error message if IsValid is false.
	/// </summary>
	public string ErrorMessage { get; set; } = string.Empty;

	/// <summary>
	/// The correct answer (for display after submission).
	/// </summary>
	public decimal CorrectAnswer { get; set; }
}
```

---

## Controller Design

### GradedExerciseController

**Namespace:** `StudyHelper.Controllers`  
**File:** `Controllers/GradedExerciseController.cs`

```csharp
using StudyHelper.Models;
using StudyHelper.Services;
using StudyHelper.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudyHelper.Controllers;

/// <summary>
/// Controller for handling graded exercise operations including problem display, 
/// answer validation, and scoring.
/// </summary>
[Authorize]
public class GradedExerciseController : Controller
{
	private readonly IGradedExerciseService _exerciseService;
	private readonly ILogger<GradedExerciseController> _logger;
	private const string SessionKey = "GradedExerciseSessionId";

	public GradedExerciseController(
		IGradedExerciseService exerciseService, 
		ILogger<GradedExerciseController> logger)
	{
		_exerciseService = exerciseService ?? throw new ArgumentNullException(nameof(exerciseService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <summary>
	/// GET: /GradedExercise/Setup
	/// Displays the setup page where user selects problem count.
	/// </summary>
	[HttpGet]
	public IActionResult Setup()
	{
		_logger.LogInformation("Setup page accessed");
		return View();
	}

	/// <summary>
	/// POST: /GradedExercise/StartExercise
	/// Validates problem count and initializes a new exercise session.
	/// </summary>
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> StartExercise(int problemCount)
	{
		try
		{
			// Validate input
			if (problemCount < 1 || problemCount > 50)
			{
				_logger.LogWarning("Invalid problem count submitted: {ProblemCount}", problemCount);
				ViewBag.Error = "Please select a valid number of problems (1-50).";
				return View("Setup");
			}

			// Get current user
			var username = User.Identity?.Name;
			if (string.IsNullOrEmpty(username))
			{
				_logger.LogWarning("No authenticated user found");
				return RedirectToAction("Login", "Account");
			}

			// Start exercise
			var sessionId = await _exerciseService.StartExerciseAsync(problemCount, username);
			TempData[SessionKey] = sessionId;

			_logger.LogInformation("Exercise started for user {Username} with {ProblemCount} problems", 
				username, problemCount);

			return RedirectToAction(nameof(Problem));
		}
		catch (ArgumentException ex)
		{
			_logger.LogError(ex, "Argument error in StartExercise");
			ViewBag.Error = ex.Message;
			return View("Setup");
		}
		catch (InvalidOperationException ex)
		{
			_logger.LogError(ex, "Invalid operation in StartExercise");
			ViewBag.Error = ex.Message;
			return View("Setup");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unexpected error in StartExercise");
			return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
		}
	}

	/// <summary>
	/// GET: /GradedExercise/Problem
	/// Displays the current problem in the exercise.
	/// </summary>
	[HttpGet]
	public async Task<IActionResult> Problem()
	{
		try
		{
			var sessionId = TempData.Peek(SessionKey)?.ToString();
			if (string.IsNullOrEmpty(sessionId))
			{
				_logger.LogWarning("No session ID in TempData");
				return RedirectToAction(nameof(Setup));
			}

			var session = await _exerciseService.GetExerciseSessionAsync(sessionId);
			if (session == null)
			{
				_logger.LogWarning("Exercise session not found: {SessionId}", sessionId);
				TempData["ErrorMessage"] = "Your exercise session expired. Please start a new exercise.";
				return RedirectToAction(nameof(Setup));
			}

			if (session.IsComplete)
			{
				_logger.LogInformation("Exercise already complete: {SessionId}", sessionId);
				TempData.Keep(SessionKey);
				return RedirectToAction(nameof(Results));
			}

			var progress = await _exerciseService.GetExerciseProgressAsync(sessionId);
			var problem = session.Problems[session.CurrentProblemIndex];

			var viewModel = new GradedExerciseProblemViewModel
			{
				ProblemNumber = session.CurrentProblemIndex + 1,
				TotalProblems = session.TotalProblems,
				ProblemText = problem.ProblemText,
				GivenValues = problem.GivenValues,
				SolveForVariable = problem.SolveForVariable,
				CorrectCount = progress.CorrectCount,
				IncorrectCount = progress.IncorrectCount
			};

			// Preserve SessionId in TempData for next request
			TempData.Keep(SessionKey);

			_logger.LogInformation("Problem page displayed for session {SessionId}, problem {ProblemNumber}", 
				sessionId, session.CurrentProblemIndex + 1);

			return View(viewModel);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error displaying problem");
			return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
		}
	}

	/// <summary>
	/// POST: /GradedExercise/SubmitAnswer
	/// Validates the submitted answer and advances to the next problem or results page.
	/// </summary>
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> SubmitAnswer(string userAnswer)
	{
		try
		{
			var sessionId = TempData.Peek(SessionKey)?.ToString();
			if (string.IsNullOrEmpty(sessionId))
			{
				_logger.LogWarning("No session ID in TempData during SubmitAnswer");
				return RedirectToAction(nameof(Setup));
			}

			var result = await _exerciseService.SubmitAnswerAsync(sessionId, userAnswer);

			if (!result.IsValid)
			{
				_logger.LogWarning("Invalid answer submission: {ErrorMessage}", result.ErrorMessage);
				TempData["ErrorMessage"] = result.ErrorMessage;
				TempData.Keep(SessionKey);
				return RedirectToAction(nameof(Problem));
			}

			_logger.LogInformation("Answer submitted for session {SessionId}, correct: {IsCorrect}", 
				sessionId, result.IsCorrect);

			// Persist session ID for next page
			TempData.Keep(SessionKey);

			if (result.IsLastProblem)
			{
				await _exerciseService.FinishExerciseAsync(sessionId);
				return RedirectToAction(nameof(Results));
			}

			return RedirectToAction(nameof(Problem));
		}
		catch (InvalidOperationException ex)
		{
			_logger.LogError(ex, "Session expired during answer submission");
			TempData["ErrorMessage"] = "Your exercise session expired. Please start a new exercise.";
			return RedirectToAction(nameof(Setup));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error submitting answer");
			return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
		}
	}

	/// <summary>
	/// GET: /GradedExercise/Results
	/// Displays the final score and problem review.
	/// </summary>
	[HttpGet]
	public async Task<IActionResult> Results()
	{
		try
		{
			var sessionId = TempData.Peek(SessionKey)?.ToString();
			if (string.IsNullOrEmpty(sessionId))
			{
				_logger.LogWarning("No session ID in TempData for Results");
				return RedirectToAction(nameof(Setup));
			}

			var session = await _exerciseService.GetExerciseSessionAsync(sessionId);
			if (session == null || !session.IsComplete)
			{
				_logger.LogWarning("Session not found or not complete for Results: {SessionId}", sessionId);
				return RedirectToAction(nameof(Setup));
			}

			var score = await _exerciseService.GetCurrentScoreAsync(sessionId);

			var viewModel = new GradedExerciseResultViewModel
			{
				CorrectCount = score.CorrectCount,
				IncorrectCount = score.IncorrectCount,
				TotalProblems = score.TotalProblems,
				Percentage = score.Percentage,
				PerformanceRating = score.PerformanceRating,
				Problems = session.Problems,
				UserAnswers = session.UserAnswers
			};

			_logger.LogInformation("Results page displayed for session {SessionId}: {CorrectCount}/{TotalProblems} ({Percentage}%)", 
				sessionId, score.CorrectCount, score.TotalProblems, score.Percentage);

			return View(viewModel);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error displaying results");
			return View("Error", new ErrorViewModel { RequestId = HttpContext?.TraceIdentifier ?? "unknown" });
		}
	}

	/// <summary>
	/// GET: /GradedExercise/RetakeExercise
	/// Clears the current exercise session and redirects to setup.
	/// </summary>
	[HttpGet]
	public async Task<IActionResult> RetakeExercise()
	{
		try
		{
			var sessionId = TempData.Peek(SessionKey)?.ToString();
			if (!string.IsNullOrEmpty(sessionId))
			{
				await _exerciseService.ClearExerciseSessionAsync(sessionId);
				_logger.LogInformation("Exercise session cleared for retake: {SessionId}", sessionId);
			}

			return RedirectToAction(nameof(Setup));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error clearing exercise session");
			return RedirectToAction(nameof(Setup));
		}
	}
}
```

---

## Registration in Program.cs

Add the following line to `Program.cs` in the service registration section:

```csharp
// Register graded exercise services
builder.Services.AddScoped<IGradedExerciseService, GradedExerciseService>();
```

**Location:** After the line registering `IGradedQuizService`

---

## Error Handling Strategy

### Exception Types

1. **ArgumentException**
   - Invalid problem count (<1 or >50)
   - Null/empty username
   - Return to Setup page with error message

2. **InvalidOperationException**
   - No equation file found
   - Insufficient equations
   - Session not found/expired
   - Return to Setup page or redirect with error message

3. **General Exception**
   - Unexpected errors
   - Return to Error page with request ID

### Logging Levels

- **Information:** Session start, completion, normal operations
- **Warning:** Invalid inputs, session not found, insufficient data
- **Error:** Exceptions, unexpected errors, service failures
- **Debug:** Cache hits/misses, intermediate calculations

---

## Security Considerations

### Authentication & Authorization
- All controller actions require `[Authorize]` attribute
- Sessions scoped by authenticated username
- Username from `User.Identity.Name` (guaranteed non-null after [Authorize])

### Input Validation
- Problem count: 1-50 range validation
- User answer: Decimal parsing with format sanitization
- Session ID: GUID format (no injection risk)

### CSRF Protection
- All POST actions have `[ValidateAntiForgeryToken]`
- Form submissions include anti-forgery token

### Session Security
- Sessions stored server-side in IMemoryCache
- No sensitive data in TempData (only session GUID)
- 30-minute sliding expiration (auto-logout)
- Clear session on retake (no data leakage)

### Logging Security
- Log username and session ID for audit trail
- Do not log user answers or problem solutions
- Mask sensitive data in error messages

---

## Performance Characteristics

### Memory Usage
- **Per Session:** ~5-10 KB (50 problems × ~200 bytes each)
- **Max Concurrent Sessions:** ~1000 (10 MB total)
- **Cache Overhead:** Negligible (<1% of total memory)

### Response Times (Expected)
- **StartExercise:** <500ms (file I/O + problem generation)
- **Problem Display:** <50ms (cache retrieval)
- **SubmitAnswer:** <100ms (validation + cache update)
- **Results:** <100ms (score calculation + cache retrieval)

### Scalability
- **Horizontal:** Stateless controller, scalable with load balancer
- **Vertical:** Limited by server memory (1000 sessions ≈ 10 MB)
- **Bottleneck:** File I/O during session start (mitigated by caching)

---

## Testing Requirements

### Unit Tests

**File:** `StudyHelper.Tests/Services/GradedExerciseServiceTests.cs`

**Test Cases:**

1. **StartExerciseAsync Tests**
   - Valid problem count (1, 10, 50)
   - Invalid problem count (0, -1, 51, 100)
   - Null/empty username
   - No equation file found
   - Insufficient equations (3 equations, request 5)
   - Successful session creation

2. **SubmitAnswerAsync Tests**
   - Correct answer (exact match)
   - Correct answer (within ±0.01 tolerance)
   - Incorrect answer
   - Invalid format ("abc", "", null)
   - Currency formatting ("$1,250.00")
   - Percentage formatting ("25%")
   - Session not found
   - Last problem vs. intermediate problem

3. **GetCurrentScoreAsync Tests**
   - All correct answers
   - All incorrect answers
   - Mixed correct/incorrect
   - Session not found

4. **GetExerciseProgressAsync Tests**
   - First problem
   - Middle problem
   - Last problem
   - Session not found

5. **FinishExerciseAsync Tests**
   - Normal completion
   - Session not found
   - Verify IsComplete flag set

6. **ClearExerciseSessionAsync Tests**
   - Clear existing session
   - Clear non-existent session

7. **CalculatePerformanceRating Tests**
   - 100% → "Excellent"
   - 85% → "Good"
   - 75% → "Fair"
   - 65% → "Poor"
   - 50% → "Needs Improvement"

### Integration Tests

1. Full exercise flow (Setup → Problem × 3 → Results)
2. Session expiration after 30 minutes
3. TempData persistence across redirects
4. Concurrent user sessions (isolation test)

---

## Deployment Checklist

### Code Files
- [ ] `Services/IGradedExerciseService.cs` created
- [ ] `Services/GradedExerciseService.cs` created
- [ ] `Models/GradedExerciseSession.cs` created
- [ ] `Models/ExerciseScore.cs` created
- [ ] `Models/ExerciseProgress.cs` created
- [ ] `Models/SubmitAnswerResult.cs` modified (if needed)
- [ ] `Controllers/GradedExerciseController.cs` created

### Configuration
- [ ] Service registered in `Program.cs`
- [ ] Session timeout configured (30 minutes)
- [ ] Logging configured for all methods

### Testing
- [ ] All unit tests passing
- [ ] Code coverage >80%
- [ ] Integration tests passing
- [ ] Manual testing completed

### Documentation
- [ ] XML comments on all public methods
- [ ] README updated
- [ ] API documentation generated

---

## Future Enhancements (Out of Scope)

1. **Database Persistence:** Save exercise history for analytics
2. **Multi-Step Problems:** Complex problems requiring multiple inputs
3. **Formula Builder:** Interactive equation editor
4. **Hints System:** Progressive hints before final answer
5. **Adaptive Difficulty:** Adjust problem complexity based on performance

---

## References

- **Graded Quiz Implementation:** `Services/GradedQuizService.cs`
- **Exercise Problem Generation:** `Services/ExerciseProblemGeneratorService.cs`
- **Equation Parsing:** `Services/EquationParserService.cs`
- **Session Management Pattern:** `Controllers/GradedQuizController.cs`
- **TempData Best Practices:** [ASP.NET Core TempData Documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/app-state#tempdata)

---

## Sign-Off

**Architect:** ✅ Approved  
**Date:** 2025-01-27  
**Next Step:** Frontend Design Document
