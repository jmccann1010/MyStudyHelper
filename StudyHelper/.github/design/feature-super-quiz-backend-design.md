# Super Quiz Feature - Backend Design

## Overview
This document specifies the backend implementation details for the Super Quiz feature, including service interfaces, data models, business logic, and integration points.

## New Models

### SuperQuizSession
**Purpose:** Represents an active Super Quiz session with multi-round retry tracking.

**Location:** `Models/SuperQuizSession.cs`

```csharp
namespace StudyHelper.Models;

/// <summary>
/// Represents an active Super Quiz session with mastery-based retry tracking.
/// Supports multi-round retries until all questions are answered correctly.
/// </summary>
public class SuperQuizSession
{
	/// <summary>
	/// Unique session identifier.
	/// </summary>
	public string SessionId { get; set; } = Guid.NewGuid().ToString();

	/// <summary>
	/// Authenticated username for session scoping.
	/// </summary>
	public string Username { get; set; } = string.Empty;

	/// <summary>
	/// All questions generated at session start (immutable after creation).
	/// </summary>
	public List<QuizQuestion> AllQuestions { get; set; } = new();

	/// <summary>
	/// Current round number (starts at 1).
	/// Round 1 = all questions, Round 2+ = missed questions from previous round.
	/// </summary>
	public int CurrentRound { get; set; } = 1;

	/// <summary>
	/// Set of question indices that have been answered correctly at least once.
	/// Uses indices into AllQuestions list.
	/// </summary>
	public HashSet<int> CorrectlyAnswered { get; set; } = new();

	/// <summary>
	/// Queue of questions for the current round.
	/// Dequeued as user progresses through questions.
	/// </summary>
	public Queue<QuizQuestion> CurrentRoundQueue { get; set; } = new();

	/// <summary>
	/// List of questions missed in the current round.
	/// Accumulated during round, used to populate next round queue.
	/// </summary>
	public List<QuizQuestion> MissedThisRound { get; set; } = new();

	/// <summary>
	/// Historical summary of each completed round.
	/// </summary>
	public List<RoundSummary> RoundHistory { get; set; } = new();

	/// <summary>
	/// Session creation timestamp.
	/// </summary>
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Last activity timestamp for timeout detection.
	/// </summary>
	public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Whether all questions have been answered correctly.
	/// </summary>
	public bool IsComplete { get; set; }

	/// <summary>
	/// Total number of questions in this session.
	/// </summary>
	public int TotalQuestions => AllQuestions.Count;

	/// <summary>
	/// Number of questions remaining to master.
	/// </summary>
	public int RemainingToMaster => TotalQuestions - CorrectlyAnswered.Count;

	/// <summary>
	/// Number of questions left in current round.
	/// </summary>
	public int QuestionsLeftThisRound => CurrentRoundQueue.Count;
}
```

### RoundSummary
**Purpose:** Captures statistics for a completed round.

**Location:** `Models/RoundSummary.cs`

```csharp
namespace StudyHelper.Models;

/// <summary>
/// Summary statistics for a completed round in Super Quiz.
/// </summary>
public class RoundSummary
{
	/// <summary>
	/// Round number (1-based).
	/// </summary>
	public int RoundNumber { get; set; }

	/// <summary>
	/// Total questions asked in this round.
	/// </summary>
	public int TotalQuestions { get; set; }

	/// <summary>
	/// Number answered correctly in this round.
	/// </summary>
	public int CorrectAnswers { get; set; }

	/// <summary>
	/// Number answered incorrectly in this round.
	/// </summary>
	public int IncorrectAnswers { get; set; }

	/// <summary>
	/// Accuracy percentage for this round.
	/// </summary>
	public double AccuracyPercent => TotalQuestions > 0 
		? (double)CorrectAnswers / TotalQuestions * 100 
		: 0;

	/// <summary>
	/// Round completion timestamp.
	/// </summary>
	public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
```

### SuperQuizProgress
**Purpose:** View model for displaying current progress to the user.

**Location:** `ViewModels/SuperQuizProgress.cs`

```csharp
namespace StudyHelper.ViewModels;

/// <summary>
/// Progress information for displaying Super Quiz state.
/// </summary>
public class SuperQuizProgress
{
	public int TotalQuestions { get; set; }
	public int Mastered { get; set; }
	public int Remaining { get; set; }
	public int CurrentRound { get; set; }
	public int QuestionsLeftThisRound { get; set; }
	public double OverallProgress => TotalQuestions > 0 
		? (double)Mastered / TotalQuestions * 100 
		: 0;
}
```

## Service Interface

### ISuperQuizService
**Purpose:** Service contract for Super Quiz session management and state transitions.

**Location:** `Services/ISuperQuizService.cs`

```csharp
using StudyHelper.Models;
using StudyHelper.ViewModels;

namespace StudyHelper.Services;

/// <summary>
/// Service for managing Super Quiz sessions with mastery-based retry logic.
/// </summary>
public interface ISuperQuizService
{
	/// <summary>
	/// Starts a new Super Quiz session with all available questions.
	/// </summary>
	/// <param name="username">Authenticated username for session scoping.</param>
	/// <returns>Session ID for future references.</returns>
	/// <exception cref="ArgumentException">Username is null or empty.</exception>
	/// <exception cref="InvalidOperationException">No study materials found or insufficient content.</exception>
	Task<string> StartSuperQuizAsync(string username);

	/// <summary>
	/// Gets the current question for the user to answer.
	/// </summary>
	/// <param name="sessionId">Session ID.</param>
	/// <returns>Current question or null if session not found.</returns>
	Task<QuizQuestion?> GetCurrentQuestionAsync(string sessionId);

	/// <summary>
	/// Submits an answer and advances session state.
	/// </summary>
	/// <param name="sessionId">Session ID.</param>
	/// <param name="selectedAnswerIndex">0-based index of selected answer (0-3).</param>
	/// <returns>Result containing validation and next action.</returns>
	/// <exception cref="InvalidOperationException">Session not found or invalid answer index.</exception>
	Task<SuperQuizAnswerResult> SubmitAnswerAsync(string sessionId, int selectedAnswerIndex);

	/// <summary>
	/// Gets current progress information.
	/// </summary>
	/// <param name="sessionId">Session ID.</param>
	/// <returns>Progress state or null if session not found.</returns>
	Task<SuperQuizProgress?> GetProgressAsync(string sessionId);

	/// <summary>
	/// Gets summary for the just-completed round.
	/// Called when transitioning between rounds.
	/// </summary>
	/// <param name="sessionId">Session ID.</param>
	/// <returns>Round summary or null if session not found.</returns>
	Task<RoundSummary?> GetLastRoundSummaryAsync(string sessionId);

	/// <summary>
	/// Starts the next round of missed questions.
	/// </summary>
	/// <param name="sessionId">Session ID.</param>
	/// <exception cref="InvalidOperationException">Session not found or no missed questions.</exception>
	Task StartNextRoundAsync(string sessionId);

	/// <summary>
	/// Gets completion summary with all round statistics.
	/// </summary>
	/// <param name="sessionId">Session ID.</param>
	/// <returns>Completion summary or null if session not found or not complete.</returns>
	Task<SuperQuizCompletionSummary?> GetCompletionSummaryAsync(string sessionId);

	/// <summary>
	/// Validates that the session belongs to the specified user.
	/// </summary>
	/// <param name="sessionId">Session ID.</param>
	/// <param name="username">Username to validate.</param>
	/// <returns>True if session exists and belongs to user.</returns>
	Task<bool> ValidateSessionOwnershipAsync(string sessionId, string username);
}
```

### SuperQuizAnswerResult
**Purpose:** Return type for answer submission.

**Location:** `Models/SuperQuizAnswerResult.cs`

```csharp
namespace StudyHelper.Models;

/// <summary>
/// Result of submitting an answer in Super Quiz.
/// </summary>
public class SuperQuizAnswerResult
{
	public bool IsCorrect { get; set; }
	public string CorrectAnswerText { get; set; } = string.Empty;
	public string UserAnswerText { get; set; } = string.Empty;
	public string Explanation { get; set; } = string.Empty;

	/// <summary>
	/// Next action after this answer.
	/// </summary>
	public SuperQuizNextAction NextAction { get; set; }

	/// <summary>
	/// Session progress after this answer.
	/// </summary>
	public SuperQuizProgress Progress { get; set; } = new();
}

/// <summary>
/// Indicates what should happen next in the quiz flow.
/// </summary>
public enum SuperQuizNextAction
{
	/// <summary>Continue to next question in current round.</summary>
	NextQuestion,

	/// <summary>Current round complete, show round summary.</summary>
	RoundComplete,

	/// <summary>All questions mastered, show completion summary.</summary>
	QuizComplete
}
```

### SuperQuizCompletionSummary
**Purpose:** Final summary shown at quiz completion.

**Location:** `ViewModels/SuperQuizCompletionSummary.cs`

```csharp
namespace StudyHelper.ViewModels;

/// <summary>
/// Summary shown when Super Quiz is completed.
/// </summary>
public class SuperQuizCompletionSummary
{
	public int TotalQuestions { get; set; }
	public int TotalRounds { get; set; }
	public TimeSpan TotalTime { get; set; }
	public List<RoundSummary> RoundHistory { get; set; } = new();
	public double OverallAccuracy { get; set; }
}
```

## Service Implementation

### SuperQuizService
**Location:** `Services/SuperQuizService.cs`

**Key Implementation Details:**

#### Constructor Dependencies
```csharp
public SuperQuizService(
	IMarkdownParserService markdownParserService,
	IQuestionGeneratorService questionGeneratorService,
	IMemoryCache memoryCache,
	ILogger<SuperQuizService> logger)
```

#### Session Cache Key Convention
```csharp
private string GetCacheKey(string sessionId) => $"superquiz-session-{sessionId}";
```

#### Cache Options
```csharp
private MemoryCacheEntryOptions GetCacheOptions() => new MemoryCacheEntryOptions
{
	SlidingExpiration = TimeSpan.FromMinutes(60),
	Priority = CacheItemPriority.Normal
};
```

#### StartSuperQuizAsync Logic
1. Validate username (throw ArgumentException if null/empty)
2. Parse markdown files using IMarkdownParserService
3. If no sections found, throw InvalidOperationException
4. If fewer than 4 sections, throw InvalidOperationException (need 4 for distractors)
5. Generate one question per section using IQuestionGeneratorService
6. Create SuperQuizSession with new GUID
7. Set AllQuestions to generated list
8. Randomize questions and populate CurrentRoundQueue
9. Store in cache with 60-minute sliding expiration
10. Return SessionId

**Randomization Strategy:**
```csharp
private void RandomizeQuestionsIntoQueue(SuperQuizSession session, List<QuizQuestion> questions)
{
	var random = new Random();
	var shuffled = questions.OrderBy(q => random.Next()).ToList();

	// Randomize direction for each question
	foreach (var question in shuffled)
	{
		question.Direction = random.Next(2) == 0 
			? QuestionDirection.TermToDefinition 
			: QuestionDirection.DefinitionToTerm;
		session.CurrentRoundQueue.Enqueue(question);
	}
}
```

#### GetCurrentQuestionAsync Logic
1. Retrieve session from cache
2. If not found, return null
3. Update LastActivityAt
4. Peek at CurrentRoundQueue (do not dequeue yet)
5. Return question

#### SubmitAnswerAsync Logic
1. Validate selectedAnswerIndex (0-3)
2. Retrieve session from cache (throw if not found)
3. Dequeue current question from CurrentRoundQueue
4. Validate answer: selectedAnswerIndex == question.CorrectAnswerIndex
5. Update LastActivityAt
6. If correct:
   - Add question index to CorrectlyAnswered set
7. If incorrect:
   - Add question to MissedThisRound list
8. Determine NextAction:
   - If CurrentRoundQueue.Count > 0: NextQuestion
   - If CurrentRoundQueue.Count == 0 && MissedThisRound.Count > 0: RoundComplete
   - If CurrentRoundQueue.Count == 0 && MissedThisRound.Count == 0: QuizComplete
9. If RoundComplete or QuizComplete:
   - Save RoundSummary to RoundHistory
10. If QuizComplete:
	- Set IsComplete = true
11. Save session back to cache
12. Build and return SuperQuizAnswerResult

#### StartNextRoundAsync Logic
1. Retrieve session from cache (throw if not found)
2. If MissedThisRound is empty, throw InvalidOperationException
3. Increment CurrentRound
4. Randomize MissedThisRound questions into CurrentRoundQueue
5. Clear MissedThisRound list
6. Update LastActivityAt
7. Save session back to cache

#### GetCompletionSummaryAsync Logic
1. Retrieve session from cache
2. If not found or not complete, return null
3. Calculate OverallAccuracy from RoundHistory
4. Calculate TotalTime from CreatedAt and LastActivityAt
5. Build SuperQuizCompletionSummary
6. Return summary

## Business Logic Rules

### Question Selection
- All questions generated at session start
- One question per MarkdownSection (covers all terms)
- Questions reused across rounds if missed
- Direction randomized per round (same term may appear as term→def in Round 1, def→term in Round 2)

### Mastery Tracking
- Question is "mastered" after first correct answer
- Mastered questions never appear again in this session
- Incorrect answers add question to next round
- A question can be missed multiple times across rounds

### Round Transitions
- Round ends when CurrentRoundQueue is empty
- If MissedThisRound is not empty, prepare next round
- If MissedThisRound is empty, mark complete
- RoundSummary saved before transition

### Session Timeout
- 60-minute sliding expiration
- Any interaction resets the timer
- Expired sessions automatically removed by cache

### Maximum Question Limit
- Enforce 500 question maximum at session start
- If more than 500 sections, take first 500
- Log warning if limit reached

### Error Handling
- Session not found: return null or throw based on context
- Invalid answer index: throw InvalidOperationException
- No study materials: throw InvalidOperationException with clear message
- Insufficient content (<4 sections): throw InvalidOperationException
- Cache failures: log error, throw exception

## Integration Points

### IMarkdownParserService
```csharp
var sections = await _markdownParserService.ParseMarkdownFilesAsync(username);
```
- Reuse existing service
- Respects EquationsEnabled setting
- Returns List<MarkdownSection>

### IQuestionGeneratorService
```csharp
var question = _questionGeneratorService.GenerateQuestion(sections, direction);
```
- Reuse existing service
- Generates one question per call
- Loop through sections to generate all questions

### IMemoryCache
```csharp
_memoryCache.Set(cacheKey, session, GetCacheOptions());
var session = _memoryCache.Get<SuperQuizSession>(cacheKey);
```
- Standard .NET IMemoryCache
- Sliding expiration for timeout
- LRU eviction when memory pressure

## Service Registration

**Location:** `Program.cs`

```csharp
builder.Services.AddScoped<ISuperQuizService, SuperQuizService>();
```

## Validation Rules

### Session Start
- Username: required, non-empty
- Study materials: at least 4 sections required
- Maximum: 500 questions

### Answer Submission
- Session ID: valid GUID format, must exist in cache
- Selected answer index: 0-3 range
- Session ownership: Username must match session.Username

### Round Transition
- Can only start next round if MissedThisRound is not empty
- Must be called after round completes

## Testing Requirements

### Unit Tests - Service Logic
```
SuperQuizServiceTests.cs:
- StartSuperQuizAsync_ValidUser_CreatesSession
- StartSuperQuizAsync_NoStudyMaterials_ThrowsException
- StartSuperQuizAsync_InsufficientContent_ThrowsException
- SubmitAnswerAsync_CorrectAnswer_MarksAsMastered
- SubmitAnswerAsync_IncorrectAnswer_AddsToMissedList
- SubmitAnswerAsync_LastQuestionCorrect_MarksComplete
- SubmitAnswerAsync_LastQuestionIncorrect_TriggersRoundComplete
- StartNextRoundAsync_ReRandomizesQuestions
- GetCompletionSummaryAsync_CalculatesAccuracyCorrectly
- ValidateSessionOwnershipAsync_WrongUser_ReturnsFalse
```

### Integration Tests - Full Flow
```
SuperQuizIntegrationTests.cs:
- CompleteSessionWithNoMistakes_OneRound
- CompleteSessionWithMistakes_MultipleRounds
- SessionTimeout_ReturnsNull
- LargeQuestionPool_HandlesCorrectly
```

### Edge Cases
- All questions correct on first attempt (1 round)
- All questions incorrect on first attempt (2+ rounds)
- Mix of correct/incorrect
- Session expiration during quiz
- Invalid session ID
- Cross-user session access attempt

## Performance Considerations

### Session Size
- 200 questions ≈ 60KB per session
- Acceptable for in-memory cache
- Monitor if sessions grow larger

### Question Generation
- Generate all questions at start: 2-3 seconds for 200 questions
- Acceptable latency for session start
- If performance issue: implement progress indicator

### Cache Efficiency
- Use sliding expiration to keep active sessions
- Automatic eviction of abandoned sessions
- Consider cache size monitoring

## Security Considerations

### Session Ownership
- Always validate Username matches User.Identity.Name
- Prevent cross-user session access
- Return 403 Forbidden if ownership check fails

### Input Validation
- Validate all user inputs at service boundary
- Use guard clauses for null/empty checks
- Range validation for answer indices

### Data Isolation
- Sessions scoped by username
- Cache keys include session ID (GUID)
- No shared state between users

## Logging Strategy

### Information Level
- Session started (username, question count)
- Round completed (round number, accuracy)
- Quiz completed (total time, rounds)

### Warning Level
- Session not found (possible timeout)
- Insufficient study materials
- Cache eviction

### Error Level
- Question generation failure
- Deserialization errors
- Unexpected exceptions

**Log Message Format:**
```csharp
_logger.LogInformation("Super Quiz session started for user {Username} with {QuestionCount} questions", 
	username, questionCount);
```

## Future Enhancements

### Database Persistence (Post-MVP)
- Replace IMemoryCache with database storage
- Add `SuperQuizSessions` and `SuperQuizRoundHistory` tables
- Enable session resume capability
- Store historical performance data

### Advanced Features (Post-MVP)
- Configurable mastery threshold (e.g., answer correctly 2x)
- Spaced repetition algorithm integration
- Export session results to PDF/CSV
- Analytics across multiple sessions
