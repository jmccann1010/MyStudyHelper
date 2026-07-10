# Super Quiz Feature Specification

## Overview
The Super Quiz feature provides a comprehensive mastery-based learning mode that ensures students answer every term/definition pair correctly before completion. Unlike the regular quiz, Super Quiz continues re-asking missed questions until 100% accuracy is achieved.

## Business Goal
Enable students to achieve complete mastery of their study materials by eliminating the possibility of leaving gaps in knowledge. This addresses the limitation where regular quizzes allow students to move on after missing questions.

## Feature Description
Super Quiz is a continuous quiz session that:
1. Includes **all** terms and definitions from the user's uploaded study materials
2. Randomizes both the order of questions and the direction (term→definition or definition→term)
3. Tracks missed questions and re-asks them in subsequent rounds
4. Continues until every question has been answered correctly at least once
5. Provides summary statistics showing learning progress across rounds

## User Stories

### US-5012: Access Super Quiz
**As a student, I want to access Super Quiz mode from the home page**

**Acceptance Criteria:**
- Super Quiz card/button is visible on the home page alongside regular quiz options
- Clicking Super Quiz navigates to `/SuperQuiz/Start`
- Super Quiz is clearly labeled and differentiated from regular Quiz
- Super Quiz icon/styling indicates comprehensive/mastery mode

### US-5013: Randomization
**As a student, I want all terms and definitions randomized in Super Quiz**

**Acceptance Criteria:**
- All terms/definitions from uploaded materials are included in the question pool
- Question order is randomized each session
- Each term/definition appears exactly once per round
- Direction (term→definition or definition→term) is randomized per question independently
- Subsequent rounds re-randomize missed questions

### US-5014: Track Missed Questions
**As a student, I want the system to track which questions I miss**

**Acceptance Criteria:**
- System records each incorrect answer during the session
- Missed questions are stored separately from correctly answered questions
- Progress indicator shows: total questions, answered correctly, remaining
- User can see their current round number
- UI clearly differentiates between first-attempt questions and retry questions

### US-5015: Re-ask Missed Questions
**As a student, I want missed questions to be re-asked after completing all other questions**

**Acceptance Criteria:**
- After answering all questions in current round, missed questions form a new round
- User is notified when starting a new round of retry questions
- Missed questions are re-randomized (order and direction) for each new round
- Cycle continues until all questions are answered correctly
- User can see how many questions remain to master

### US-5016: Complete Super Quiz
**As a student, I want to complete the Super Quiz when all questions are answered correctly**

**Acceptance Criteria:**
- Quiz completes when no missed questions remain
- Completion screen shows session summary:
  - Total questions mastered
  - Number of rounds required
  - Time taken
  - Accuracy by round
- User can return to home page or start a new Super Quiz
- Session data is not persisted (in-memory only for MVP)

### US-5017: Answer Feedback
**As a student, I want feedback on each answer during Super Quiz**

**Acceptance Criteria:**
- Correct answers show green success feedback matching existing quiz UX
- Incorrect answers show red error feedback with the correct answer displayed
- Feedback is displayed before moving to next question
- UI matches existing quiz feedback patterns for consistency
- Feedback indicates whether this is a first attempt or retry

## Scope

### In Scope
- Complete term/definition coverage from user's uploaded materials
- Multi-round retry mechanism for missed questions
- Progress tracking and round indicators
- Session summary with statistics
- In-memory session management (no database persistence for MVP)
- Reuse existing QuizQuestion and QuestionGeneratorService infrastructure

### Out of Scope
- Persistent session storage (if user closes browser, session is lost)
- Historical analytics across multiple Super Quiz sessions
- Equation-based questions (follows existing EquationsEnabled setting)
- Time limits or scoring (focus is on mastery, not speed/grade)
- Adaptive difficulty or spaced repetition algorithms
- Multi-user competitive modes

## Technical Constraints
- Must use existing authentication (User.Identity.Name)
- Must respect EquationsEnabled setting (exclude equation flashcards if disabled)
- Session timeout after 60 minutes of inactivity (configurable)
- Maximum 500 questions per session (practical limit for all terms)
- Memory cache for session storage with LRU eviction

## Dependencies
- Existing IMarkdownParserService for loading study materials
- Existing IQuestionGeneratorService for question generation
- Existing QuizQuestion model and answer validation logic
- IMemoryCache for session state management

## Success Metrics
- User completes at least one Super Quiz session
- Average completion rate (sessions started vs. completed)
- Average number of rounds required to achieve mastery
- User feedback on learning effectiveness

## Design Principles
1. **Reuse existing patterns**: Follow GradedQuizService architecture for session management
2. **Consistency**: Match existing quiz UI/UX patterns for familiarity
3. **Simplicity**: Focus on core mastery loop without unnecessary features
4. **Performance**: Efficiently handle large question pools (200+ terms)
5. **Resilience**: Clear error messages and graceful degradation

## User Flow
```
Home Page → [Click Super Quiz] 
  ↓
Super Quiz Start Page (shows question count, estimated time)
  ↓
[Start Super Quiz]
  ↓
Round 1: All Questions (randomized order and direction)
  ↓
Answer each question → Immediate feedback
  ↓
Round Complete → Summary (X correct, Y missed)
  ↓
[If missed > 0] → Round 2: Missed Questions (re-randomized)
  ↓
[Repeat until missed = 0]
  ↓
Completion Summary (total questions, rounds, time, accuracy)
  ↓
[Return Home] or [Start New Super Quiz]
```

## Future Enhancements (Not in MVP)
- Persistent session storage with resume capability
- Historical tracking of Super Quiz performance over time
- Export results to CSV/PDF for study records
- Configurable "mastery threshold" (e.g., answer correctly 2x instead of 1x)
- Spaced repetition scheduling based on miss history
- Study mode toggle (show answer immediately vs. test mode)
