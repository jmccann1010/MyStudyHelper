# Super Quiz User-Selectable Question Count - Feature Specification

## Feature Overview

**Feature ID:** #5018  
**Title:** Super Quiz: User-Selectable Question Count  
**Branch:** feature/super-quiz-select-number-of-questions

### Current State

The Super Quiz currently uses **all available terms and definitions** from the user's TermsAndDefinitions.md file. While this ensures comprehensive coverage, it may be overwhelming for users with large study material sets or those wanting quick practice sessions.

### Desired State

Users will be able to select the number of questions when starting a Super Quiz:
- **10 Questions:** Exactly 10 randomly selected questions (default)
- **Half:** Half of available terms (rounded down)
- **All:** All available terms (current behavior)

The selection is made on the Super Quiz start page before beginning the session, and the estimated time updates dynamically based on the selection.

---

## User Stories

### Story #5019: Display Question Count Options
**As a user**, I want to see question count options on the Super Quiz start page  
**So that** I can choose how many questions to practice with

**Acceptance Criteria:**
- Three radio button options displayed: "10 Questions", "Half (X questions)", "All (Y questions)"
- X and Y are calculated dynamically from available terms
- Default selection is "10 Questions"
- Selection state persists during form validation errors
- UI is responsive and accessible

---

### Story #5020: Fixed 10 Questions Option
**As a user**, I want to select "10 Questions" and receive exactly 10 random questions  
**So that** I can have a quick practice session

**Acceptance Criteria:**
- Selecting "10 Questions" generates exactly 10 random questions
- Works when total terms < 10 (shows error)
- Works when total terms >= 10
- Questions are randomly selected from all available terms
- Questions maintain bidirectional term/definition format

---

### Story #5021: Half Questions Option
**As a user**, I want to select "Half" and receive questions based on half of my available terms  
**So that** I can practice a moderate number of questions

**Acceptance Criteria:**
- Counts total terms in TermsAndDefinitions.md
- Calculates half (using integer division, rounds down)
- Generates that many random questions
- UI shows calculated count: "Half (15 questions)"
- Updates estimated time to match

---

### Story #5022: All Questions Option
**As a user**, I want to select "All" and receive questions for every available term  
**So that** I can master all my study materials

**Acceptance Criteria:**
- Counts all terms in TermsAndDefinitions.md
- Generates a question for each term/definition pair
- UI shows total count: "All (30 questions)"
- Maintains existing Super Quiz mastery behavior
- Updates estimated time to match

---

### Story #5023: Dynamic Estimated Time
**As a user**, I want the estimated time to update based on my question count selection  
**So that** I know how long the quiz will take

**Acceptance Criteria:**
- Estimated time updates when selection changes
- Uses existing calculation: 15 seconds per question
- Formats time appropriately (minutes or hours)
- Updates without page refresh (client-side JavaScript)

---

### Story #5024: Backend Question Limit Support
**As a developer**, I need the backend to support question count limits  
**So that** the selected question count is enforced during session creation

**Acceptance Criteria:**
- `ISuperQuizService.StartSuperQuizAsync` accepts optional parameter
- Three modes supported: Fixed10, Half, All
- Randomly selects specified number of questions when applicable
- Maintains existing validation rules (minimum 4 terms required for any option)
- Existing behavior preserved when parameter not provided (backward compatible)

---

## Business Value

1. **Improved User Experience:** Users can tailor quiz length to their available time
2. **Better Engagement:** Quick 10-question sessions lower the barrier to entry
3. **Flexible Learning:** Users can choose intensity based on confidence level
4. **Time Management:** Clear estimated time helps users plan study sessions

---

## Technical Constraints

1. Minimum 4 terms required to generate questions (existing constraint)
2. "10 Questions" option requires at least 10 terms
3. Maximum question limit of 50 remains enforced (existing constraint)
4. Existing mastery-based retry behavior maintained for all modes
5. Backward compatibility: existing Super Quiz sessions not affected

---

## Dependencies

- Existing `IMarkdownParserService` for term counting
- Existing `IQuestionGeneratorService` for question generation
- Existing `SuperQuizService` session management
- Client-side JavaScript for dynamic UI updates

---

## Success Criteria

- [ ] All 6 user stories implemented and tested
- [ ] UI shows question count options with dynamic counts
- [ ] Backend correctly limits questions based on selection
- [ ] Estimated time updates dynamically
- [ ] Existing Super Quiz functionality not broken
- [ ] Help documentation updated
- [ ] Unit tests added for new functionality

---

## Out of Scope

- Custom question count (free-form number input)
- Saving user's preferred question count selection
- Different question count options per study material file
- Time limits or timers during Super Quiz
- Different time estimates per question type

---

## Next Steps

1. **Architecture Phase:** Create backend and frontend design documents
2. **Implementation Phase:** Develop backend models, services, and controllers
3. **UI Phase:** Update views and add client-side JavaScript
4. **Testing Phase:** Unit tests and integration tests
5. **Documentation Phase:** Update help pages and user guides
