# Feature: Graded Quiz - Backlog & User Stories

**Feature Title:** Graded Quiz  
**Branch:** `feature/graded-quiz`  
**Status:** Ready for Human Approval  
**Target Sprint:** TBD (pending human approval)  

---

## Feature Overview

Implement a **Graded Quiz** feature that allows users to take a scored quiz by:
1. Selecting the number of questions to answer
2. Answering multiple-choice questions
3. Viewing real-time score tracking
4. Receiving a final score report with performance metrics

This feature will be accessible from a new **"Graded Quiz"** panel on the home page, following the existing design patterns.

---

## User Stories

### US-1: Create Graded Quiz Setup Page
**Description:** As a user, I want to select how many questions I want to answer before starting a graded quiz.

**Acceptance Criteria:**
- [ ] New page `/GradedQuiz/Setup` exists and is accessible to authenticated users
- [ ] Page displays a title "Graded Quiz Setup"
- [ ] User can select question count via dropdown or input field (valid range: 1-50 questions)
- [ ] A "Start Quiz" button is displayed and enabled only when question count is valid
- [ ] Page stores selected question count in session/TempData for next page
- [ ] Page redirects to `/GradedQuiz/Question` on successful selection
- [ ] Cancel button returns user to home page

**Definition of Done:**
- Page renders without errors
- Input validation works (rejects invalid counts)
- Session data persists to next request
- Unit tests verify routing and state management

---

### US-2: Implement Graded Quiz Question Display Page
**Description:** As a user, I want to see quiz questions one at a time with multiple-choice answers during a graded quiz.

**Acceptance Criteria:**
- [ ] New page `/GradedQuiz/Question` displays a single question
- [ ] Each question shows:
  - Question number (e.g., "Question 1 of 10")
  - Question text
  - Four answer options labeled A, B, C, D
  - Current score display (e.g., "3 correct, 2 incorrect")
  - Progress bar showing quiz completion
- [ ] User can select an answer by clicking a radio button or button
- [ ] "Next" button advances to the next question
- [ ] "Previous" button allows users to go back to the previous question
- [ ] On the final question, "Next" button changes to "Finish Quiz"
- [ ] Page retrieves questions from existing quiz infrastructure (IMarkdownParserService, IQuestionGeneratorService)
- [ ] Unanswered questions cannot be skipped (validation on Next)
- [ ] User responses are stored in session for the quiz duration

**Definition of Done:**
- Page displays questions correctly without errors
- Navigation between questions works (forward and backward)
- Answer selection and validation work
- Unit tests verify question generation and state management
- Integration tests verify session persistence

---

### US-3: Add Score Tracking Logic
**Description:** As the quiz engine, I need to track user answers and calculate real-time scores.

**Acceptance Criteria:**
- [ ] Create or extend service class `IGradedQuizService` with methods:
  - `StartQuizAsync(int questionCount, string username)` → returns quiz session ID
  - `SubmitAnswerAsync(string quizSessionId, int questionNumber, int selectedAnswerIndex)` → returns validation result
  - `GetCurrentScoreAsync(string quizSessionId)` → returns (correct: int, incorrect: int)
  - `GetQuizProgressAsync(string quizSessionId)` → returns current question number and total
- [ ] Score tracking correctly increments on correct answers
- [ ] Score tracking correctly increments on incorrect answers
- [ ] Duplicate submissions for the same question do not change the score
- [ ] Quiz session expires after 30 minutes of inactivity
- [ ] Session data is stored in in-memory cache or temporary storage (not persisted to database initially)

**Definition of Done:**
- Service is dependency-injected in controllers
- Unit tests achieve >80% coverage of score logic
- Integration tests verify session lifecycle
- Edge cases tested (duplicate submission, timeout, etc.)

---

### US-4: Create Graded Quiz Results Page
**Description:** As a user, I want to see my final score and performance summary when I finish a graded quiz.

**Acceptance Criteria:**
- [ ] New page `/GradedQuiz/Results` displays:
  - Total questions answered
  - Number of correct answers
  - Number of incorrect answers
  - Percentage score
  - Performance rating (e.g., Excellent: 90+%, Good: 80-89%, etc.)
  - List of each question with user's answer vs. correct answer
  - Explanation for each question
- [ ] "Retake Quiz" button returns user to Setup page
- [ ] "Return to Home" button navigates to `/`
- [ ] Results page is only accessible after quiz completion (redirects to Setup if accessed directly)

**Definition of Done:**
- Page displays all required metrics correctly
- Calculations are accurate (percentage, rating)
- Navigation buttons work as expected
- Unit tests verify result calculations
- Integration tests verify access control

---

### US-5: Add Graded Quiz Panel to Home Page
**Description:** As a user browsing the home page, I want to see a "Graded Quiz" panel alongside other study tools.

**Acceptance Criteria:**
- [ ] Home page (`/Views/Home/Index.cshtml`) includes a new panel for "Graded Quiz"
- [ ] Panel follows the existing design pattern (same styling, layout, and card format as Flashcard, Equation, Quiz panels)
- [ ] Panel displays:
  - Icon (consistent with other panels)
  - Title "Graded Quiz"
  - Description: "Take a scored quiz and track your performance"
  - Call-to-action button: "Start Graded Quiz"
- [ ] Button links to `/GradedQuiz/Setup`
- [ ] Panel is visible only to authenticated users
- [ ] Panel is responsive on mobile and desktop

**Definition of Done:**
- Panel renders without errors
- Styling matches existing panels
- Responsive design works on all breakpoints
- No new CSS files needed (uses existing bootstrap + site.css)
- Unit tests verify visibility logic

---

### US-6: Create Graded Quiz Controller
**Description:** As the application, I need a controller to route requests for the graded quiz feature.

**Acceptance Criteria:**
- [ ] Create `GradedQuizController` in `Controllers/` directory
- [ ] Controller has these actions:
  - `Setup()` (GET) → displays setup page
  - `StartQuiz()` (POST) → validates question count, initiates quiz session, redirects to Question
  - `Question()` (GET) → displays current question
  - `SubmitAnswer()` (POST) → validates answer, advances to next question or Results
  - `Results()` (GET) → displays final score and performance report
  - `RetakeQuiz()` (GET) → clears session and redirects to Setup
- [ ] Controller requires `[Authorize]` attribute
- [ ] All actions validate user session and quiz state
- [ ] Proper error handling with redirects to error page on invalid state

**Definition of Done:**
- All actions route correctly
- Session management works
- Error handling tested
- Unit tests verify controller logic (>80% coverage)
- Integration tests verify full flow

---

### US-7: Write Unit Tests for Graded Quiz Feature
**Description:** As a QA engineer, I need comprehensive unit tests for the graded quiz feature to ensure quality and maintainability.

**Acceptance Criteria:**
- [ ] Create test class `GradedQuizControllerTests` in `StudyHelper.Tests/Controllers/`
- [ ] Create test class `GradedQuizServiceTests` in `StudyHelper.Tests/Services/`
- [ ] Tests cover:
  - Setup page validation (valid/invalid question counts)
  - Score calculation (correct/incorrect answers)
  - Score persistence across questions
  - Quiz session lifecycle (start, progress, end)
  - Results calculation and display logic
  - Navigation between questions (forward/backward)
  - Unanswered question validation
  - Error handling and edge cases
- [ ] All tests use xUnit framework
- [ ] All tests achieve ≥80% code coverage for tested classes
- [ ] No placeholder `Assert.True(true)` assertions
- [ ] Tests run successfully and all pass

**Definition of Done:**
- All tests pass
- Coverage report shows ≥80% on graded quiz code
- Test file naming follows convention: `ClassName_Tests.cs`
- Code review complete

---

## Dependency Map

```
Home Page (View)
  └── Graded Quiz Panel Link
	  └── GradedQuizController
		  ├── Setup (GET/POST)
		  ├── Question (GET/POST)
		  ├── Results (GET)
		  └── RetakeQuiz (GET)
			  └── IGradedQuizService
				  ├── StartQuizAsync()
				  ├── SubmitAnswerAsync()
				  ├── GetCurrentScoreAsync()
				  └── GetQuizProgressAsync()
					  └── IQuestionGeneratorService (existing)
						  └── IMarkdownParserService (existing)
```

---

## Technical Considerations

- **Reuse Existing Services:** Leverage `IQuestionGeneratorService` and `IMarkdownParserService` to avoid duplicating question generation logic.
- **Session Management:** Use ASP.NET Core session or TempData for quiz state (no database persistence initially).
- **User Identification:** Use `User.Identity?.Name` to scope quiz sessions to authenticated users.
- **Error Handling:** Graceful error pages if session expires or invalid state detected.
- **Testing:** Mock dependencies; do not hit real files or database during unit tests.

---

## Acceptance Criteria Summary

✅ Feature is complete when:
- All 7 user stories are implemented and passing
- All unit tests pass with ≥80% coverage
- Home page displays Graded Quiz panel
- User can select question count, answer questions, and view results
- Score tracking is accurate
- Session management is secure and robust

---

## Files to Create/Modify

### New Files
- `Controllers/GradedQuizController.cs`
- `Services/IGradedQuizService.cs`
- `Services/GradedQuizService.cs`
- `Views/GradedQuiz/Setup.cshtml`
- `Views/GradedQuiz/Question.cshtml`
- `Views/GradedQuiz/Results.cshtml`
- `StudyHelper.Tests/Controllers/GradedQuizControllerTests.cs`
- `StudyHelper.Tests/Services/GradedQuizServiceTests.cs`

### Modified Files
- `Views/Home/Index.cshtml` (add Graded Quiz panel)
- `Program.cs` (register IGradedQuizService dependency)

---

## Estimated Complexity
- **Story Points:** 21 (US-1: 3, US-2: 5, US-3: 5, US-4: 3, US-5: 2, US-6: 2, US-7: 1)
- **Implementation Effort:** ~3-4 sprints (engineering + review + QA)
- **Risk:** Low (leverages existing infrastructure; similar to existing Quiz feature)

---

## Next Steps

1. **Human Review & Approval:** This backlog requires human review and approval before proceeding.
2. **Azure DevOps Sync:** Upon approval, create work items in https://dev.azure.com/SchneiderDowns/Jeff
3. **Solutions Architect:** Design documents required before development begins
4. **Engineering:** Implementation on `feature/graded-quiz` branch
5. **Code Review & Security:** Review implementation before QA
6. **QA:** Test implementation and verify coverage
7. **Technical Writing:** Document feature for end users
8. **Human Final Review:** Approve PR and merge to main

