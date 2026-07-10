# Feature: Graded Exercises

## Feature Overview
Create a new "Graded Exercises" feature that functions like the "Graded Quiz" but uses the Equations file to generate math/calculation exercises with scoring and results tracking.

## Epic
**Title:** Graded Exercises - Scored Equation Practice

**Description:**
Add a graded exercises feature that allows users to practice solving equation problems with score tracking, progress monitoring, and detailed results at the end. This feature mirrors the successful Graded Quiz implementation but focuses on equation-based calculations instead of multiple-choice questions.

**Acceptance Criteria:**
- Users can start a graded exercise session by selecting the number of problems (1-50)
- System generates exercise problems from the user's equation files
- Users solve each problem and submit their answers
- System validates answers with appropriate tolerance for decimal calculations
- Users see their score progress throughout the session
- Final results page shows complete breakdown of performance
- New panel on home page provides access to Graded Exercises

---

## User Stories

### Story 1: Setup Page for Graded Exercises
**Title:** As a user, I want to select how many exercise problems to solve so I can control my practice session length

**Description:**
Create a setup page where users can choose the number of exercise problems they want to complete in a graded session.

**Acceptance Criteria:**
- [ ] Setup page displays with clear instructions
- [ ] User can select problem count between 1 and 50
- [ ] Validation ensures valid input (required, numeric, within range)
- [ ] "Start Graded Exercises" button initiates the session
- [ ] Error messages display for invalid inputs
- [ ] Page follows existing design patterns from GradedQuiz

**Technical Notes:**
- Controller: GradedExerciseController.Setup (GET)
- View: Views/GradedExercise/Setup.cshtml
- Similar to GradedQuiz/Setup.cshtml

**Story Points:** 3

---

### Story 2: Exercise Problem Display
**Title:** As a user, I want to see one exercise problem at a time with my current score so I can track my progress

**Description:**
Display exercise problems individually with input field for answers, showing progress and current score.

**Acceptance Criteria:**
- [ ] Display current problem number and total (e.g., "Problem 3 of 10")
- [ ] Show problem text with equation and given values
- [ ] Provide input field for numerical answer
- [ ] Display current correct/incorrect count
- [ ] Show progress bar indicating completion percentage
- [ ] Submit button advances to next problem
- [ ] Validation ensures answer is submitted before advancing
- [ ] Session state maintained across requests using TempData

**Technical Notes:**
- Controller: GradedExerciseController.Problem (GET)
- Controller: GradedExerciseController.SubmitAnswer (POST)
- View: Views/GradedExercise/Problem.cshtml
- ViewModel: GradedExerciseProblemViewModel
- Use TempData.Peek() and TempData.Keep() for session management

**Story Points:** 8

---

### Story 3: Graded Exercise Service Implementation
**Title:** As a developer, I need a service to manage graded exercise sessions so the system can track progress and scoring

**Description:**
Implement a GradedExerciseService that manages session lifecycle, problem generation, answer validation, and scoring.

**Acceptance Criteria:**
- [ ] StartExerciseAsync creates new session with specified problem count
- [ ] Generates problems from user's equation files using IExerciseProblemGeneratorService
- [ ] SubmitAnswerAsync validates user answers with decimal tolerance
- [ ] GetExerciseSessionAsync retrieves current session state
- [ ] GetExerciseProgressAsync returns current score and progress
- [ ] FinishExerciseAsync finalizes session and calculates final score
- [ ] ClearExerciseSessionAsync removes session for retake
- [ ] Sessions stored in IMemoryCache with 30-minute timeout
- [ ] All methods include proper error handling and logging

**Technical Notes:**
- Interface: IGradedExerciseService
- Implementation: GradedExerciseService
- Model: GradedExerciseSession
- Depends on: IExerciseProblemGeneratorService, IEquationParserService, IMemoryCache
- Register in Program.cs as scoped service

**Story Points:** 13

---

### Story 4: Results Page with Score Breakdown
**Title:** As a user, I want to see my final score and review all problems so I can learn from my mistakes

**Description:**
Display comprehensive results page showing final score, performance rating, and detailed review of all problems with correct answers.

**Acceptance Criteria:**
- [ ] Display final score as percentage and fraction (e.g., "8/10 - 80%")
- [ ] Show performance rating (Excellent, Good, Fair, Poor, Needs Improvement)
- [ ] List all problems with user's answer and correct answer
- [ ] Highlight correct answers in green, incorrect in red
- [ ] Show solution steps for each problem
- [ ] Provide "Retake Exercises" button to start new session
- [ ] Provide "Back to Home" button
- [ ] Performance rating matches GradedQuiz logic

**Technical Notes:**
- Controller: GradedExerciseController.Results (GET)
- View: Views/GradedExercise/Results.cshtml
- ViewModel: GradedExerciseResultViewModel
- Similar design to GradedQuiz/Results.cshtml

**Story Points:** 5

---

### Story 5: Home Page Panel Integration
**Title:** As a user, I want a Graded Exercises panel on the home page so I can easily start a graded exercise session

**Description:**
Add a new panel to the home page that provides access to the Graded Exercises feature with consistent styling.

**Acceptance Criteria:**
- [ ] New panel displays on home page in card grid layout
- [ ] Panel includes appropriate icon (calculator with checkmark suggested)
- [ ] Title: "Graded Exercises"
- [ ] Description explains the feature clearly
- [ ] "Start Graded Exercises" button links to setup page
- [ ] Color scheme: border-secondary or similar to differentiate from other panels
- [ ] Hover effects consistent with other panels
- [ ] Responsive layout maintained

**Technical Notes:**
- Update: Views/Home/Index.cshtml
- Add new panel after Equation Flashcards panel
- Follow existing card/panel structure

**Story Points:** 3

---

### Story 6: Models and ViewModels
**Title:** As a developer, I need data models for graded exercise sessions so the system can track state and results

**Description:**
Create necessary models and view models to support graded exercise functionality.

**Acceptance Criteria:**
- [ ] GradedExerciseSession model includes:
  - SessionId, Username, TotalProblems
  - CurrentProblemIndex, Problems list
  - UserAnswers dictionary, IsComplete flag
  - CreatedAt, LastActivityAt timestamps
- [ ] GradedExerciseProblemViewModel includes:
  - ProblemNumber, TotalProblems
  - ProblemText, GivenValues
  - CorrectCount, IncorrectCount
  - ProgressPercentage calculation
- [ ] GradedExerciseResultViewModel includes:
  - CorrectCount, IncorrectCount, TotalProblems
  - Percentage, PerformanceRating
  - Problems list, UserAnswers dictionary
- [ ] ExerciseScore model includes score calculation logic

**Technical Notes:**
- Models: GradedExerciseSession, ExerciseScore
- ViewModels: GradedExerciseProblemViewModel, GradedExerciseResultViewModel
- Store in Models/ and ViewModels/ folders respectively

**Story Points:** 5

---

### Story 7: Answer Validation with Decimal Tolerance
**Title:** As a developer, I need to validate decimal answers with appropriate tolerance so rounding differences don't penalize users

**Description:**
Implement answer validation that accounts for decimal rounding and calculation precision.

**Acceptance Criteria:**
- [ ] Validation accepts answers within ±0.01 of correct answer
- [ ] Handles currency formatting ($ symbols, commas)
- [ ] Handles percentage formatting (% symbols)
- [ ] Validates ratio results with appropriate precision
- [ ] Provides clear error messages for invalid input
- [ ] Logs validation results for debugging
- [ ] Matches existing ExerciseResult validation logic

**Technical Notes:**
- Leverage existing IExerciseProblemGeneratorService.ValidateAnswer
- Ensure consistency with Exercise controller validation
- Add unit tests for edge cases

**Story Points:** 5

---

### Story 8: Session Management and Security
**Title:** As a developer, I need secure session management so user progress is protected and isolated

**Description:**
Implement secure session management using TempData and memory cache with proper authentication.

**Acceptance Criteria:**
- [ ] All controller actions require [Authorize] attribute
- [ ] Sessions scoped by username to prevent cross-user access
- [ ] TempData uses server-side session storage (already configured)
- [ ] Cache entries expire after 30 minutes of inactivity
- [ ] Expired sessions redirect to setup with friendly message
- [ ] CSRF protection on all POST actions with [ValidateAntiForgeryToken]
- [ ] Logging includes username and session ID for audit trail
- [ ] Session IDs use GUIDs for uniqueness

**Technical Notes:**
- Follow GradedQuizController security patterns
- Use TempData.Peek() to avoid consuming values prematurely
- Use TempData.Keep() to preserve session ID across redirects

**Story Points:** 5

---

### Story 9: Unit Tests for Graded Exercise Service
**Title:** As a developer, I need comprehensive unit tests so the graded exercise service is reliable

**Description:**
Create unit tests covering all GradedExerciseService methods and edge cases.

**Acceptance Criteria:**
- [ ] Test StartExerciseAsync with valid and invalid inputs
- [ ] Test SubmitAnswerAsync with correct/incorrect answers
- [ ] Test session expiration and not found scenarios
- [ ] Test score calculation accuracy
- [ ] Test session lifecycle (start, progress, finish, clear)
- [ ] Test concurrent session handling
- [ ] Achieve >80% code coverage for service
- [ ] All tests pass consistently

**Technical Notes:**
- Test project: StudyHelper.Tests
- Follow patterns from GradedQuizServiceTests.cs
- Use xUnit, Moq, and FluentAssertions
- Mock IExerciseProblemGeneratorService and IMemoryCache

**Story Points:** 8

---

## Technical Implementation Summary

### Files to Create
1. **Controllers/GradedExerciseController.cs** - Main controller for graded exercises
2. **Services/IGradedExerciseService.cs** - Service interface
3. **Services/GradedExerciseService.cs** - Service implementation
4. **Models/GradedExerciseSession.cs** - Session model
5. **Models/ExerciseScore.cs** - Score calculation model
6. **ViewModels/GradedExerciseProblemViewModel.cs** - Problem display view model
7. **ViewModels/GradedExerciseResultViewModel.cs** - Results display view model
8. **Views/GradedExercise/Setup.cshtml** - Setup page view
9. **Views/GradedExercise/Problem.cshtml** - Problem display view
10. **Views/GradedExercise/Results.cshtml** - Results page view
11. **StudyHelper.Tests/Services/GradedExerciseServiceTests.cs** - Unit tests

### Files to Modify
1. **Views/Home/Index.cshtml** - Add Graded Exercises panel
2. **Program.cs** - Register IGradedExerciseService

### Dependencies
- Uses existing IExerciseProblemGeneratorService
- Uses existing IEquationParserService
- Uses existing IUserStudyMaterialService
- Uses existing IMemoryCache

---

## Estimated Total Story Points: 55

## Estimated Duration
- Sprint 1: Stories 1, 2, 6 (16 points) - Setup, Display, Models
- Sprint 2: Stories 3, 7 (18 points) - Service Implementation, Validation
- Sprint 3: Stories 4, 5, 8, 9 (21 points) - Results, Integration, Security, Testing

## Definition of Done
- [ ] All code written and committed
- [ ] All unit tests passing
- [ ] Manual testing completed
- [ ] Code review completed
- [ ] No compilation errors or warnings
- [ ] Documentation updated
- [ ] Feature accessible from home page
- [ ] Works with user study materials
- [ ] Session management secure and reliable
- [ ] Merged to main branch

---

## Azure DevOps Setup Instructions

### Create Epic
1. Navigate to https://dev.azure.com/SchneiderDowns/Jeff
2. Go to Boards → Work Items
3. Click "New Work Item" → "Epic"
4. Title: "Graded Exercises - Scored Equation Practice"
5. Description: Copy from "Epic" section above
6. Area Path: StudyHelper
7. Iteration: Select appropriate sprint

### Create User Stories
For each story above (Stories 1-9):
1. Click "New Work Item" → "User Story"
2. Link to the Epic created above
3. Copy Title, Description, and Acceptance Criteria
4. Add Story Points to "Effort" field
5. Add Technical Notes to Description
6. Assign to appropriate team member
7. Set Priority based on dependency order

### Story Dependencies
- Story 6 (Models) should be completed first
- Story 3 (Service) depends on Story 6
- Stories 1, 2, 4 (Controllers/Views) depend on Stories 3 and 6
- Story 5 (Home Panel) can be done independently
- Story 7 (Validation) should align with Story 3
- Story 8 (Security) should be implemented alongside Story 2
- Story 9 (Tests) should be done last

### Suggested Sprint Planning
**Sprint 1 - Foundation (16 points):**
- Story 6: Models and ViewModels
- Story 1: Setup Page
- Story 2: Exercise Problem Display (start)

**Sprint 2 - Core Logic (18 points):**
- Story 2: Exercise Problem Display (complete)
- Story 3: Service Implementation
- Story 7: Answer Validation

**Sprint 3 - Completion (21 points):**
- Story 4: Results Page
- Story 5: Home Page Panel
- Story 8: Security
- Story 9: Unit Tests

---

## References
- Existing Graded Quiz Implementation: Controllers/GradedQuizController.cs
- Existing Exercise Implementation: Controllers/ExerciseController.cs
- Service Patterns: Services/GradedQuizService.cs
- Test Patterns: StudyHelper.Tests/Services/GradedQuizServiceTests.cs
