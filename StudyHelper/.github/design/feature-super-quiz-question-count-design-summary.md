# Super Quiz Question Count Selection - Design Summary

## Overview

This document provides a high-level overview of the design for implementing user-selectable question counts in the Super Quiz feature.

**Feature ID:** #5018  
**Branch:** `feature/super-quiz-select-number-of-questions`  
**Azure DevOps:** https://dev.azure.com/SchneiderDowns/Jeff/_workitems/edit/5018

---

## Executive Summary

### Current State
The Super Quiz currently uses **all available terms and definitions** from the user's study materials, which can be overwhelming for users with large content sets.

### Proposed Solution
Allow users to select from three question count options before starting a Super Quiz:
- **10 Questions** (default): Quick practice session with 10 random questions
- **Half**: Practice with half of available terms (rounded down)
- **All**: Comprehensive practice with all available terms (current behavior)

### Key Benefits
1. **Improved User Experience:** Tailored quiz length based on available time
2. **Lower Barrier to Entry:** Quick 10-question sessions encourage more frequent practice
3. **Flexible Learning:** Users choose intensity based on confidence and goals
4. **Better Time Management:** Clear estimated time display helps planning

---

## Design Documents

### 1. Feature Specification
**File:** `.github/design/feature-super-quiz-question-count-spec.md`

**Contents:**
- Detailed user stories with acceptance criteria
- Business value and success metrics
- Technical constraints and dependencies
- Out-of-scope items

### 2. Backend Design
**File:** `.github/design/feature-super-quiz-question-count-backend-design.md`

**Contents:**
- New enum: `SuperQuizQuestionCountOption`
- Enhanced view model: `SuperQuizStartViewModel`
- Updated service interface and implementation
- Controller changes (GET and POST)
- Validation rules and edge cases
- Unit testing strategy

**Key Technical Decisions:**
- Default parameter in `StartSuperQuizAsync` ensures backward compatibility
- Random selection uses `OrderBy(_ => Guid.NewGuid())` for simplicity
- Validation provides clear, actionable error messages
- Minimum 4 terms enforced for all options

### 3. Frontend Design
**File:** `.github/design/feature-super-quiz-question-count-frontend-design.md`

**Contents:**
- UI/UX mockup and design principles
- Updated Razor view with radio button group
- Client-side JavaScript for real-time updates
- CSS enhancements for visual feedback
- Responsive design considerations
- Accessibility compliance (WCAG 2.1 AA)
- Browser compatibility matrix

**Key Technical Decisions:**
- Radio buttons (not dropdown) for clear visual comparison
- Pure JavaScript (no jQuery dependency)
- Client-side validation provides immediate feedback
- Dynamic preview updates without page refresh

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         User Interface                          │
│  Views/SuperQuiz/Start.cshtml                                   │
│  - Radio buttons for question count selection                   │
│  - Preview cards (question count, estimated time)               │
│  - Client-side JavaScript (super-quiz-start.js)                 │
└────────────────────┬────────────────────────────────────────────┘
					 │ HTTP POST with SelectedOption
					 ↓
┌─────────────────────────────────────────────────────────────────┐
│                      Controller Layer                           │
│  SuperQuizController.Start(SuperQuizStartViewModel model)       │
│  - Validates user selection                                     │
│  - Checks term count requirements                               │
│  - Calls service with selected option                           │
└────────────────────┬────────────────────────────────────────────┘
					 │ StartSuperQuizAsync(username, option)
					 ↓
┌─────────────────────────────────────────────────────────────────┐
│                        Service Layer                            │
│  SuperQuizService.StartSuperQuizAsync(...)                      │
│  1. Parse markdown files (get all available terms)              │
│  2. Calculate target question count based on option             │
│  3. Validate target count is achievable                         │
│  4. Randomly select sections if limiting                        │
│  5. Generate questions for selected sections                    │
│  6. Create and cache session                                    │
└────────────────────┬────────────────────────────────────────────┘
					 │ ParseMarkdownFilesAsync(username)
					 ↓
┌─────────────────────────────────────────────────────────────────┐
│                      Data Access Layer                          │
│  MarkdownParserService                                          │
│  - Reads TermsAndDefinitions.md from user's folder              │
│  - Returns List<MarkdownSection>                                │
└─────────────────────────────────────────────────────────────────┘
```

---

## Data Flow

### Starting a Super Quiz

```
┌──────────────────┐
│  User visits     │
│  /SuperQuiz/Start│
└────────┬─────────┘
		 │ GET
		 ↓
┌──────────────────────────────────┐
│  Controller: Start() GET          │
│  - Parse markdown files           │
│  - Count available terms          │
│  - Build SuperQuizStartViewModel  │
└────────┬─────────────────────────┘
		 │ Render view
		 ↓
┌────────────────────────────────────┐
│  View: Start.cshtml                 │
│  - Display radio buttons            │
│  - Show preview (10 questions)      │
│  - Initialize JavaScript            │
└────────┬───────────────────────────┘
		 │ User selects "Half"
		 │ (JavaScript updates preview)
		 ↓
┌────────────────────────────────────┐
│  Preview cards update               │
│  - Questions: 15                    │
│  - Time: 4 minutes                  │
└────────┬───────────────────────────┘
		 │ User clicks "Start"
		 │ POST with SelectedOption=1
		 ↓
┌────────────────────────────────────┐
│  Controller: Start(model) POST      │
│  - Validate model                   │
│  - Check term requirements          │
│  - Call service with Half option    │
└────────┬───────────────────────────┘
		 │ StartSuperQuizAsync(username, Half)
		 ↓
┌────────────────────────────────────┐
│  Service: StartSuperQuizAsync       │
│  - Parse 30 available terms         │
│  - Calculate target: 30 / 2 = 15    │
│  - Randomly select 15 sections      │
│  - Generate 15 questions            │
│  - Create session                   │
│  - Return session ID                │
└────────┬───────────────────────────┘
		 │ Redirect to Question
		 ↓
┌────────────────────────────────────┐
│  /SuperQuiz/Question?sessionId=...  │
│  (Existing Super Quiz flow)         │
└─────────────────────────────────────┘
```

---

## Implementation Plan

### Phase 1: Backend Foundation
**Estimated Time:** 4-6 hours

1. Create `Models/SuperQuizQuestionCountOption.cs` enum
2. Update `ViewModels/SuperQuizStartViewModel.cs` with new properties
3. Update `Services/ISuperQuizService.cs` interface signature
4. Implement `Services/SuperQuizService.cs` changes:
   - Add `CalculateTargetQuestionCount` helper method
   - Update `StartSuperQuizAsync` implementation
   - Add random section selection logic
5. Write unit tests for service layer:
   - Test each option (Fixed10, Half, All)
   - Test edge cases (< 10 terms, rounding, validation)
   - Test random selection

**Deliverable:** Backend can start Super Quiz sessions with question count limits

---

### Phase 2: Controller Integration
**Estimated Time:** 2-3 hours

1. Update `Controllers/SuperQuizController.cs`:
   - Update GET `Start()` to pass total available terms
   - Update POST `Start(SuperQuizStartViewModel)` to accept and validate selection
   - Add client-side validation for Fixed10 option
2. Write controller tests:
   - Test GET returns correct view model
   - Test POST with each option
   - Test POST validation errors

**Deliverable:** Controller properly routes user selection to service layer

---

### Phase 3: Frontend Implementation
**Estimated Time:** 3-4 hours

1. Update `Views/SuperQuiz/Start.cshtml`:
   - Add radio button group
   - Add `data-count` attributes
   - Update preview cards with IDs
2. Create `wwwroot/js/super-quiz-start.js`:
   - Implement dynamic preview updates
   - Implement Fixed10 validation
   - Add form submission validation
3. Optional: Add CSS enhancements

**Deliverable:** UI displays options and updates preview in real-time

---

### Phase 4: Testing & Polish
**Estimated Time:** 2-3 hours

1. Manual testing:
   - Test all question count options
   - Test validation scenarios
   - Test responsive design (desktop, tablet, mobile)
   - Test accessibility (keyboard, screen reader)
2. Cross-browser testing:
   - Chrome, Firefox, Safari, Edge
   - Mobile browsers (iOS Safari, Chrome Mobile)
3. Fix any bugs or UX issues
4. Update help documentation

**Deliverable:** Feature is production-ready

---

### Phase 5: Documentation & Deployment
**Estimated Time:** 1-2 hours

1. Update `Views/Help/SuperQuiz.cshtml` (if exists) or create help content
2. Add feature announcement to help index
3. Update any developer documentation
4. Code review and merge to main
5. Deploy to production

**Deliverable:** Feature is documented and deployed

---

## Total Estimated Time

**12-18 hours** (1.5 - 2 days of focused development)

### Breakdown
- Backend: 4-6 hours
- Controller: 2-3 hours
- Frontend: 3-4 hours
- Testing: 2-3 hours
- Documentation: 1-2 hours

---

## Risk Assessment

### Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Random selection not truly random | Low | Low | Use `Guid.NewGuid()` for simplicity; test distribution |
| Performance impact with large term sets | Low | Low | Random selection is O(n log n); acceptable for < 100 terms |
| Backward compatibility broken | Low | High | Default parameter ensures existing code works; thorough testing |
| Client-side JavaScript fails | Low | Medium | Form still works without JS; server-side validation catches issues |
| Validation messages unclear | Medium | Low | Use clear, actionable error messages; test with real users |

### User Experience Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Users confused by new options | Low | Medium | Default to "10 Questions"; provide clear descriptions |
| Users select "All" accidentally with 100+ terms | Medium | Medium | Show question count and estimated time prominently |
| Users expect to remember last selection | Medium | Low | Out of scope for now; document as future enhancement |

---

## Success Metrics

### Functional Metrics
- [ ] All 6 user stories pass acceptance criteria
- [ ] Unit test coverage >= 80% for new code
- [ ] Zero regression bugs in existing Super Quiz functionality
- [ ] Passes accessibility audit (WCAG 2.1 AA)

### User Experience Metrics (post-deployment)
- Percentage of users selecting each option (10 / Half / All)
- Average quiz completion rate per option
- User feedback on feature usefulness

---

## Dependencies

### Internal Dependencies
- Existing `IMarkdownParserService` (term counting)
- Existing `IQuestionGeneratorService` (question generation)
- Existing `SuperQuizService` (session management)
- Existing Super Quiz views and controllers

### External Dependencies
- Bootstrap 5 (UI framework)
- Modern browser with JavaScript enabled
- .NET 10 runtime

---

## Backward Compatibility

### Ensuring Compatibility

1. **Service Interface:**
   - Default parameter `questionCountOption = SuperQuizQuestionCountOption.All`
   - Existing callers work without changes

2. **Session Model:**
   - No changes to `SuperQuizSession` structure
   - Existing cached sessions continue to work

3. **API Contracts:**
   - No breaking changes to HTTP endpoints
   - View model is backward compatible (new properties have defaults)

### Testing Strategy
- Run full test suite before and after changes
- Manual regression testing of existing Super Quiz flows
- Test with real user data (various term counts)

---

## Open Questions

### Resolved
✅ Should we use a dropdown or radio buttons?  
→ **Radio buttons** for clear visual comparison

✅ Should we use `OrderBy(_ => Guid.NewGuid())` or Fisher-Yates shuffle?  
→ **`OrderBy(_ => Guid.NewGuid())`** for simplicity; acceptable performance

✅ Should "10 Questions" or "All" be the default?  
→ **"10 Questions"** to encourage quick practice sessions

✅ Should we support custom question counts (e.g., 15, 25)?  
→ **No**, out of scope for initial release

### Pending
❓ Should we track user's preferred option for analytics?  
→ Yes, via logging; no UI changes required

❓ Should we add tooltips explaining each option?  
→ Out of scope for initial release; evaluate based on user feedback

---

## Next Steps

1. **Human Approval:** Review and approve this design document
2. **Implementation:** Begin Phase 1 (Backend Foundation)
3. **Code Review:** Review backend implementation before proceeding to frontend
4. **Testing:** Execute comprehensive testing plan
5. **Deployment:** Merge to main and deploy to production

---

## Appendix: Related Documents

### Design Documents
- `.github/design/feature-super-quiz-question-count-spec.md`
- `.github/design/feature-super-quiz-question-count-backend-design.md`
- `.github/design/feature-super-quiz-question-count-frontend-design.md`

### Azure DevOps Work Items
- Feature #5018: Super Quiz: User-Selectable Question Count
- User Story #5019: Display question count options on start page
- User Story #5020: Fixed 10 questions option
- User Story #5021: Half questions option
- User Story #5022: All questions option
- User Story #5023: Dynamic estimated time updates
- User Story #5024: Backend question limit support

### Existing Documentation
- `.github/design/feature-super-quiz-spec.md` (original Super Quiz design)
- `.github/design/feature-super-quiz-backend-design.md` (original backend)
- `.github/design/feature-super-quiz-frontend-design.md` (original frontend)

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-06-02 | GitHub Copilot | Initial design document created |

---

**Design Status:** ✅ Ready for Implementation  
**Approval Required:** Solutions Architect, Lead Developer  
**Estimated Delivery:** 1.5 - 2 days
