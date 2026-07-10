# Feature Specification: Bidirectional Quiz Questions

**Feature Name:** Bidirectional Quiz Questions  
**Project:** StudyHelper  
**Azure DevOps Project:** https://dev.azure.com/SchneiderDowns/Jeff  
**Branch:** `feature/quiz-bidirectional-questions`  
**Date Created:** 2025-01-27  
**Status:** Specification Ready for Review  

---

## Executive Summary

This feature enhances the Quiz and Graded Quiz functionality by supporting bidirectional questions. Currently, quizzes only display a term and ask users to select the correct definition. This feature will also allow quizzes to display a definition and ask users to select the correct term, providing more comprehensive learning and testing opportunities.

---

## Business Value

### Problem Statement
Current quiz functionality is unidirectional—students see terms and must recall definitions. This approach:
- Limits learning depth and bidirectional recall ability
- Doesn't test whether students can identify terms from definitions
- Provides less variety in study sessions
- Doesn't fully prepare students for real-world scenarios where they may encounter definitions without immediately knowing the associated term

### Benefits
1. **Enhanced Learning:** Bidirectional practice improves memory retention and understanding
2. **Comprehensive Testing:** Tests both term→definition and definition→term recall
3. **Increased Variety:** Makes study sessions more engaging with varied question formats
4. **Real-World Preparation:** Better prepares students for various testing and application scenarios
5. **Adaptive Learning:** Allows the system to automatically vary question direction

---

## Scope

### In Scope
- Quiz feature: Support bidirectional questions (term→definition and definition→term)
- Graded Quiz feature: Support bidirectional questions with scoring
- Question generation: Randomize question direction for variety
- Answer options: Dynamically generate incorrect options based on question direction
- User experience: Clear indication of question type (asking for term vs definition)
- Session persistence: Track question direction in quiz sessions

### Out of Scope
- Flashcard features (separate feature set)
- Exercise features (equation-based, not term-based)
- Custom user preference for question direction ratio
- Analytics/reporting on performance by question direction
- Adaptive difficulty based on bidirectional performance

---

## User Stories

### Epic: Bidirectional Quiz Questions
**As a** student  
**I want** quizzes to test both term-to-definition and definition-to-term recall  
**So that** I can develop comprehensive understanding and bidirectional memory of study material

---

### Story 1: View Definition-to-Term Questions in Practice Quiz
**Priority:** High  
**Effort:** 5 points  

**As a** student using the practice Quiz feature  
**I want** to see questions that display a definition and ask me to select the correct term  
**So that** I can practice identifying terms from their definitions

**Acceptance Criteria:**
1. When I start a quiz, some questions display a definition as the prompt
2. The definition is clearly labeled or formatted to indicate it's asking for the term
3. Four answer options are presented as possible terms
4. Only one answer option is the correct term
5. Three incorrect terms are used as distractors
6. The incorrect terms are contextually relevant (from the same study material)
7. After I select an answer, I receive immediate feedback indicating correct/incorrect
8. The explanation includes the term, definition, and why my answer was correct or incorrect

**Notes:**
- Question direction should be randomized (roughly 50/50 split between term→definition and definition→term)
- Maintain existing Quiz behavior for term→definition questions

---

### Story 2: View Definition-to-Term Questions in Graded Quiz
**Priority:** High  
**Effort:** 5 points  

**As a** student taking a Graded Quiz assessment  
**I want** to encounter questions that ask me to identify the correct term from a definition  
**So that** my assessment comprehensively tests bidirectional knowledge

**Acceptance Criteria:**
1. When I take a graded quiz, some questions display definitions as prompts
2. The question clearly indicates it's asking for the term (e.g., "Which term matches this definition?")
3. Four answer options are presented as possible terms
4. My answer is recorded and scored like other questions
5. During the quiz, I don't see feedback (consistent with graded quiz behavior)
6. On the results page, bidirectional questions are displayed with correct answers and explanations
7. My score includes performance on both question directions
8. The results page clearly shows which questions were term→definition and which were definition→term

**Notes:**
- Bidirectional questions should be mixed throughout the quiz, not grouped
- Score calculation treats all questions equally regardless of direction

---

### Story 3: Question Generation Supports Bidirectional Format
**Priority:** High  
**Effort:** 8 points  

**As a** developer  
**I want** the question generation service to create both term→definition and definition→term questions from the same study material  
**So that** the system can present varied question types to users

**Technical Requirements:**
1. Modify question generation logic to support a "direction" parameter or property
2. When generating a question:
   - Randomly determine direction (50% term→definition, 50% definition→term)
   - Set the prompt based on direction (term or definition)
   - Set the correct answer based on direction (definition or term)
   - Generate three incorrect options from the opposite field (e.g., if asking for term, distractors are other terms)
3. Store question direction in session/cache for consistent display
4. Ensure explanations are contextually appropriate for the question direction
5. Maintain backward compatibility with existing question format

**Acceptance Criteria:**
1. `IQuestionGeneratorService` supports generating bidirectional questions
2. Generated questions include a `QuestionDirection` property (enum: `TermToDefinition`, `DefinitionToTerm`)
3. Question prompt and answer options are correctly populated based on direction
4. Incorrect answer options are contextually relevant to the question direction
5. Unit tests verify correct generation for both directions
6. No breaking changes to existing quiz functionality

---

### Story 4: Quiz UI Indicates Question Direction
**Priority:** Medium  
**Effort:** 3 points  

**As a** student  
**I want** a clear visual indication of what type of answer is expected (term or definition)  
**So that** I'm not confused about what the question is asking

**Acceptance Criteria:**
1. When a question asks for a definition (term→definition), display a label like "Select the correct definition:"
2. When a question asks for a term (definition→term), display a label like "Select the correct term:" or "Which term matches this definition?"
3. The prompt text is styled consistently with the existing UI
4. The label appears above or near the answer options
5. Visual design is clear and accessible (readable contrast, appropriate font size)

**Notes:**
- Consider using icons or color coding for additional clarity (optional enhancement)
- Ensure accessibility for screen readers

---

### Story 5: Graded Quiz Results Display Question Direction
**Priority:** Medium  
**Effort:** 3 points  

**As a** student reviewing my graded quiz results  
**I want** to see which questions were term→definition and which were definition→term  
**So that** I can understand my performance across both question types

**Acceptance Criteria:**
1. On the results page, each question displays an indicator of its direction
2. For term→definition questions, show "Question Type: Term → Definition" or similar
3. For definition→term questions, show "Question Type: Definition → Term" or similar
4. The indicator is visually distinct but doesn't clutter the results display
5. Both question types are displayed consistently in the results review section

**Notes:**
- Consider grouping or filtering options by question direction (future enhancement)
- Ensure indicator is clear in both correct and incorrect answer displays

---

### Story 6: Distractor Generation for Bidirectional Questions
**Priority:** High  
**Effort:** 5 points  

**As a** developer  
**I want** the system to generate contextually appropriate incorrect answers (distractors) for both question directions  
**So that** questions are challenging and educational

**Technical Requirements:**
1. For term→definition questions:
   - Correct answer: the term's actual definition
   - Distractors: definitions of three other terms from the study material
2. For definition→term questions:
   - Correct answer: the actual term
   - Distractors: three other terms from the study material
3. Distractors should be randomly selected from available study material
4. Avoid repeating the same distractors across consecutive questions in a session
5. Ensure distractors are sufficiently different from the correct answer

**Acceptance Criteria:**
1. Generated questions always have exactly four answer options (1 correct, 3 incorrect)
2. Distractors are contextually relevant and drawn from the same study material source
3. No duplicate distractors within a single question
4. Distractors are shuffled so the correct answer position varies
5. If insufficient terms/definitions are available, handle gracefully (e.g., minimum 4 terms required)

---

### Story 7: Session Tracking for Bidirectional Questions
**Priority:** Medium  
**Effort:** 3 points  

**As a** developer  
**I want** quiz sessions to track question direction for each question  
**So that** the system can correctly display prompts, answers, and explanations

**Technical Requirements:**
1. Extend quiz session models to include question direction for each question
2. Store direction in memory cache or session state alongside other question data
3. Retrieve direction when rendering question views
4. Include direction in result summaries and review pages
5. Ensure session data serialization/deserialization handles new property

**Acceptance Criteria:**
1. Quiz session includes `QuestionDirection` for each question in the session
2. Direction persists across page navigation within a quiz session
3. Results page correctly retrieves and displays question direction
4. No data loss or corruption when adding direction property
5. Backward compatibility: existing sessions without direction default gracefully

---

## Technical Design Overview

### Components Affected

1. **Models**
   - `Question` model: Add `QuestionDirection` enum property
   - `QuizSession` model: Ensure question direction is stored
   - `GradedQuizSession` model: Ensure question direction is stored

2. **Services**
   - `IQuestionGeneratorService` / `QuestionGeneratorService`: Modify generation logic to support bidirectional questions
   - `IMarkdownParserService` / `MarkdownParserService`: Ensure term/definition pairs are accessible for distractor generation

3. **View Models**
   - `QuizQuestionViewModel`: Include question direction for UI display
   - `GradedQuizQuestionViewModel`: Include question direction
   - `GradedQuizResultViewModel`: Include direction in result review

4. **Controllers**
   - `QuizController`: Handle bidirectional questions in question display and answer submission
   - `GradedQuizController`: Handle bidirectional questions in graded quiz flow

5. **Views**
   - `Views/Quiz/Question.cshtml`: Display question direction label and format prompt appropriately
   - `Views/GradedQuiz/Question.cshtml`: Display question direction label
   - `Views/GradedQuiz/Results.cshtml`: Display question direction in results review

### Data Flow

1. **Question Generation**
   ```
   User starts quiz → Service generates questions with random direction →
   Session stores questions with direction → Controller passes to view
   ```

2. **Question Display**
   ```
   Controller retrieves question from session → Includes direction →
   View renders prompt (term or definition) and answer options (definitions or terms) based on direction
   ```

3. **Answer Validation**
   ```
   User submits answer → Controller retrieves question direction from session →
   Validates answer against correct term or definition based on direction →
   Returns feedback with appropriate explanation
   ```

4. **Results Review**
   ```
   Graded quiz completes → Results page retrieves all questions with directions →
   Displays each question with direction indicator, correct answer, user's answer, and explanation
   ```

---

## Design Considerations

### Question Direction Distribution
- **Approach:** Randomize direction for each question independently (50/50 split)
- **Alternative:** Allow users to configure direction preference (out of scope for this feature)

### Distractor Quality
- **Challenge:** Ensuring distractors are plausible and educational
- **Solution:** Select distractors from the same study material to maintain contextual relevance
- **Future Enhancement:** Use semantic similarity or difficulty scoring for better distractor selection

### Backward Compatibility
- **Requirement:** Existing quiz sessions and data must continue to work
- **Solution:** Default question direction to `TermToDefinition` if not specified
- **Testing:** Verify existing quizzes work unchanged before and after deployment

### Accessibility
- **Screen Readers:** Ensure question direction labels are announced clearly
- **Visual Indicators:** Use text labels primarily; icons/color as supplementary
- **Keyboard Navigation:** No changes needed; existing navigation continues to work

---

## Testing Strategy

### Unit Tests
1. Question generation with `TermToDefinition` direction
2. Question generation with `DefinitionToTerm` direction
3. Distractor generation for both directions
4. Answer validation for both directions
5. Session serialization/deserialization with direction property

### Integration Tests
1. Start quiz and verify bidirectional questions are generated
2. Answer a term→definition question correctly
3. Answer a definition→term question correctly
4. Answer a term→definition question incorrectly
5. Answer a definition→term question incorrectly
6. Complete graded quiz with mixed question directions
7. View results and verify direction indicators are present

### Manual Testing
1. Start practice quiz and observe mix of question directions
2. Answer several questions and verify feedback is contextually appropriate
3. Start graded quiz and observe mix of question directions
4. Complete graded quiz and review results for direction indicators
5. Verify accessibility (screen reader, keyboard navigation)
6. Test with minimal study material (4 terms) to verify graceful handling

---

## Deployment Plan

### Phase 1: Backend Implementation
- Implement question direction enum and model changes
- Update question generation service for bidirectional support
- Update distractor generation logic
- Add unit tests

### Phase 2: Frontend Implementation
- Update quiz and graded quiz views for direction display
- Add direction labels and formatting
- Update result views to show direction indicators
- Verify UI/UX consistency

### Phase 3: Testing & Refinement
- Execute full test suite
- Conduct manual testing and accessibility review
- Gather feedback from stakeholders
- Address bugs and polish UX

### Phase 4: Deployment
- Deploy to production
- Monitor for issues
- Collect user feedback

---

## Success Metrics

1. **Functionality:** 100% of quizzes generate bidirectional questions
2. **Distribution:** Approximately 50/50 split between question directions in quiz sessions
3. **User Engagement:** No decrease in quiz completion rates
4. **Performance:** No noticeable performance degradation in question generation or display
5. **Quality:** Zero critical bugs reported in first 30 days post-deployment

---

## Risks & Mitigation

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Insufficient study material (< 4 terms) for distractor generation | High | Low | Validate term count before quiz start; display error if insufficient |
| User confusion about question direction | Medium | Medium | Clear UI labels and instructions; update help documentation |
| Performance impact from additional generation logic | Low | Low | Optimize distractor selection; cache study material in session |
| Backward compatibility issues with existing sessions | High | Low | Thorough testing; default direction for legacy data |

---

## Open Questions

1. **Question:** Should there be a user setting to control the ratio of term→definition vs definition→term questions?
   - **Answer:** Out of scope for initial release; consider for future enhancement

2. **Question:** Should analytics track performance differences between question directions?
   - **Answer:** Out of scope for initial release; consider for future enhancement

3. **Question:** Should help documentation be updated to explain bidirectional questions?
   - **Answer:** Yes, update Quiz and Graded Quiz help pages after implementation

---

## Dependencies

- No external dependencies
- Relies on existing `IQuestionGeneratorService` and `IMarkdownParserService` interfaces
- Compatible with current study material format (markdown term/definition pairs)

---

## Timeline Estimate

- **Backend Development:** 3-5 days
- **Frontend Development:** 2-3 days
- **Testing & QA:** 2-3 days
- **Documentation Update:** 1 day
- **Total Estimate:** 8-12 days (1.5-2 weeks)

---

## Approvals

- [ ] Product Owner Review
- [ ] Technical Lead Review
- [ ] QA Lead Review
- [ ] Ready for Development

---

## References

- Azure DevOps Project: https://dev.azure.com/SchneiderDowns/Jeff
- GitHub Branch: `feature/quiz-bidirectional-questions`
- Related Documentation:
  - Quiz Feature Help: `Views/Help/Quiz.cshtml`
  - Graded Quiz Feature Help: `Views/Help/GradedQuiz.cshtml`
  - Question Generator Service: `Services/QuestionGeneratorService.cs`
