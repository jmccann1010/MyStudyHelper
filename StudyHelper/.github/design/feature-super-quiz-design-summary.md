# Super Quiz Feature - Design Summary

## Overview
Super Quiz is a mastery-based quiz mode that ensures students answer every question correctly through a multi-round retry mechanism. This document provides the high-level architectural design for the feature.

## Architecture Decisions

### 1. Session Management Pattern
**Decision:** In-memory session storage using `IMemoryCache` with session-based state management.

**Rationale:**
- Follows existing `GradedQuizService` pattern for consistency
- Simpler than database persistence for MVP
- Sufficient for single-session use case
- Cache eviction handles abandoned sessions automatically

**Trade-offs:**
- Sessions lost on browser close or server restart
- Cannot resume sessions across devices
- Future enhancement: migrate to database persistence if needed

### 2. Service Layer Design
**Decision:** Create dedicated `ISuperQuizService` and `SuperQuizService` following existing service patterns.

**Rationale:**
- Separation of concerns from regular quiz logic
- Enables independent testing and maintenance
- Follows Single Responsibility Principle
- Allows different session lifecycle than graded quiz

**Components:**
- `ISuperQuizService`: Service interface
- `SuperQuizService`: Implementation with memory cache
- `SuperQuizController`: MVC controller for HTTP endpoints
- `SuperQuizSession`: Session state model

### 3. Data Model
**Decision:** Extend `GradedQuizSession` pattern with retry tracking.

**New Model: `SuperQuizSession`**
```
- SessionId (string, GUID)
- Username (string)
- AllQuestions (List<QuizQuestion>) - initial full set
- CurrentRound (int) - tracks retry rounds
- CorrectlyAnswered (HashSet<int>) - indices of mastered questions
- CurrentRoundQueue (Queue<QuizQuestion>) - questions for current round
- MissedThisRound (List<QuizQuestion>) - accumulates misses
- RoundHistory (List<RoundSummary>) - statistics per round
- CreatedAt, LastActivityAt (DateTime)
- IsComplete (bool)
```

**Rationale:**
- Queue structure efficiently handles question progression
- HashSet provides O(1) lookup for mastery tracking
- RoundHistory enables completion summary statistics
- Separates "current attempt" from "mastered" state

### 4. Question Generation Strategy
**Decision:** Generate all questions upfront at session start, then randomize per round.

**Rationale:**
- Predictable session scope (user knows total count)
- Avoids mid-session content changes
- Enables progress percentage calculation
- Simpler error handling (fail fast at start)

**Implementation:**
1. Parse all markdown sections at session start
2. Generate one question per section (all terms)
3. Randomize order and direction for initial round
4. Re-randomize missed questions for subsequent rounds

### 5. Round Transition Logic
**Decision:** Batch missed questions into discrete rounds with explicit round boundaries.

**Rationale:**
- Clear mental model for users ("Round 2 of retries")
- Enables round-specific statistics
- Natural pause points for user feedback
- Aligns with gamification patterns

**Flow:**
1. Complete current round queue
2. Check if MissedThisRound is empty
3. If not empty: increment CurrentRound, re-randomize missed questions into new queue
4. If empty: mark IsComplete = true, show summary

### 6. Controller Design
**Decision:** RESTful controller with session-based navigation pattern.

**Endpoints:**
- `GET /SuperQuiz/Start` - Session creation form (shows question count)
- `POST /SuperQuiz/Start` - Create session, redirect to question
- `GET /SuperQuiz/Question?sessionId={id}` - Display current question
- `POST /SuperQuiz/SubmitAnswer` - Validate answer, advance state
- `GET /SuperQuiz/RoundSummary?sessionId={id}` - Between-round feedback
- `GET /SuperQuiz/Complete?sessionId={id}` - Final summary

**Rationale:**
- Matches existing QuizController and GradedQuizController patterns
- Session ID in query string for bookmarkability
- POST/Redirect/GET pattern prevents double submission
- Explicit round summary endpoint for better UX

## Data Flow

### Session Start Flow
```
User → Start Page
  ↓
Parse markdown (IMarkdownParserService)
  ↓
Generate all questions (IQuestionGeneratorService)
  ↓
Create SuperQuizSession
  ↓
Randomize questions → CurrentRoundQueue
  ↓
Store in IMemoryCache
  ↓
Redirect to Question
```

### Answer Submission Flow
```
User submits answer
  ↓
Retrieve session from cache
  ↓
Validate answer
  ↓
If correct: Add to CorrectlyAnswered set
If incorrect: Add to MissedThisRound list
  ↓
Remove from CurrentRoundQueue
  ↓
Update LastActivityAt
  ↓
If queue empty:
  - Check MissedThisRound
  - If has misses → RoundSummary
  - If no misses → Complete
Else:
  - Redirect to next Question
```

### Round Transition Flow
```
CurrentRoundQueue is empty
  ↓
Save RoundSummary to RoundHistory
  ↓
Check MissedThisRound
  ↓
If empty:
  - IsComplete = true
  - Redirect to Complete
If not empty:
  - Increment CurrentRound
  - Randomize MissedThisRound
  - Move to CurrentRoundQueue
  - Clear MissedThisRound
  - Redirect to RoundSummary (brief interstitial)
  - User clicks Continue → next Question
```

## Component Interaction

```
SuperQuizController
	↓ (depends on)
ISuperQuizService
	↓ (depends on)
IMarkdownParserService, IQuestionGeneratorService, IMemoryCache
	↓ (produces/consumes)
SuperQuizSession (stored in cache)
	↓ (contains)
List<QuizQuestion>, HashSet<int>, Queue<QuizQuestion>
```

## Error Handling

### Session Not Found
- **Cause:** Cache eviction or invalid session ID
- **Response:** Redirect to Start with error message
- **Message:** "Your session expired. Please start a new Super Quiz."

### No Study Materials
- **Cause:** User has no uploaded markdown files
- **Response:** Show error page with link to Study Materials
- **Message:** "No study materials found. Please upload materials first."

### Insufficient Content
- **Cause:** Fewer than 4 sections (cannot generate distractors)
- **Response:** Show error on Start page
- **Message:** "At least 4 terms required for Super Quiz. Current count: {count}"

### Question Generation Failure
- **Cause:** Invalid markdown format or parsing error
- **Response:** Fail fast at session start
- **Message:** "Unable to generate questions. Please check your study materials."

### Session Timeout
- **Cause:** No activity for 60 minutes
- **Response:** Cache eviction, redirect to Start
- **Message:** "Session timed out due to inactivity."

## Performance Considerations

### Memory Usage
- Session size: ~10KB + (300 bytes × question count)
- Max 500 questions = ~160KB per session
- Cache limit: 100 concurrent sessions = ~16MB
- Acceptable for MVP

### Question Generation
- All questions generated at session start
- Trade-off: longer initial load for predictable session
- For 200 questions: ~2-3 seconds (acceptable)
- If >500 questions: show warning, limit to first 500

### Cache Strategy
- Sliding expiration: 60 minutes
- LRU eviction when memory limit reached
- Session ID as cache key: `superquiz-session-{guid}`

## Security Considerations

### Authentication
- All endpoints require `[Authorize]` attribute
- Session.Username must match User.Identity.Name
- Prevent cross-user session access

### Input Validation
- Selected answer index: 0-3 range
- Session ID: valid GUID format
- Question count: 1-500 range (enforced at start)

### Anti-Forgery
- All POST actions use `[ValidateAntiForgeryToken]`
- Form submissions include CSRF token

## Testing Strategy

### Unit Tests
- `SuperQuizService` logic (round transitions, mastery tracking)
- Question randomization
- Correct/incorrect answer handling
- Round summary calculations
- Session state integrity

### Integration Tests
- Full session lifecycle (start → questions → completion)
- Cache persistence and retrieval
- Multi-round retry flow
- Session timeout behavior

### Manual Testing
- Complete session with no mistakes (1 round)
- Complete session with mistakes (multi-round)
- Session expiration/timeout
- Large question pool (200+ questions)
- UI consistency with existing quiz patterns

## Future Enhancements (Post-MVP)

### Persistent Storage
- Migrate sessions to database
- Enable resume capability
- Store historical performance

### Advanced Features
- Configurable mastery threshold (answer correctly N times)
- Spaced repetition scheduling
- Progress charts and analytics
- Export results to PDF/CSV

### Performance Optimization
- Lazy question generation (generate on-demand per round)
- Redis for distributed caching
- Background job for session cleanup

## Dependencies

### External Services
- `IMarkdownParserService` - existing
- `IQuestionGeneratorService` - existing
- `IMemoryCache` - .NET built-in
- `ILogger<SuperQuizService>` - logging

### Models (Existing)
- `QuizQuestion`
- `QuestionDirection`
- `MarkdownSection`

### Models (New)
- `SuperQuizSession`
- `RoundSummary`
- `SuperQuizProgress` (view model)

### ViewModels (New)
- `SuperQuizStartViewModel`
- `SuperQuizQuestionViewModel` (extend existing)
- `SuperQuizRoundSummaryViewModel`
- `SuperQuizCompleteViewModel`

## Compatibility

### Backward Compatibility
- No changes to existing quiz features
- No database migrations required
- No breaking changes to existing services

### EquationsEnabled Setting
- Respect existing user preference
- Exclude equation-based content if disabled
- Only include term/definition pairs

## Deployment Notes
- No database changes required
- No configuration changes required
- Memory cache configuration remains default
- Can deploy independently of other features
