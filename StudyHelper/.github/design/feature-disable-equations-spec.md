# Feature Specification: Disable Equations Setting

## Epic
**Title:** User-Configurable Equations Visibility Control  
**ID:** FEAT-DisableEquations  
**Status:** Proposed  
**Priority:** Medium  
**Target Release:** TBD

## Business Value

### Problem Statement
Currently, all users see all study features on the home page (Quiz, Graded Quiz, Exercise, Graded Exercises, Term Flashcards, and Equation Flashcards) regardless of whether they use equation-based features. Users who focus exclusively on term/definition study materials have no way to hide equation-related features that are not relevant to their study needs.

### Proposed Solution
Add a user preference setting in the Study Materials settings page that allows users to enable or disable equation-based features. When equations are disabled, the following panels will be hidden from the home page:
- Exercise
- Graded Exercises
- Equation Flashcards

### Business Benefits
- **Improved User Experience:** Users can customize their home page to show only relevant study methods
- **Reduced Clutter:** Simplifies the interface for users who don't use equation-based study materials
- **User Control:** Empowers users to personalize their study environment
- **Scalability:** Establishes a pattern for future feature toggles and user preferences

## Scope

### In Scope
1. Add "Enable Equations" toggle setting in Study Materials settings
2. Store user preference in user-specific metadata
3. Conditionally render equation-based panels on home page based on setting:
   - Exercise panel (lines 67-93 in Views/Home/Index.cshtml)
   - Graded Exercises panel (lines 95-120 in Views/Home/Index.cshtml)
   - Equation Flashcards panel (lines 149-173 in Views/Home/Index.cshtml)
4. Default new users to "Equations Enabled" for backward compatibility
5. Add informational help text explaining what features are affected
6. Persist user preference across sessions

### Out of Scope
- Disabling access to equation controllers/routes (they remain accessible via direct URL)
- Disabling equation file uploads (users can still upload equation files)
- Removing equation-related code or services from the backend
- Creating additional feature toggles for other study types
- Migration of existing user preferences (all existing users default to enabled)

### Assumptions
- Users manage their own preferences; no admin interface needed
- The setting applies to home page visibility only
- Equation-based features remain functional if accessed directly
- Default state is "enabled" to maintain current user experience

## User Stories

### Story 1: Access Equations Setting
**As a** StudyHelper user  
**I want to** access a setting to enable or disable equation-based features  
**So that** I can customize which study methods appear on my home page

**Acceptance Criteria:**
- Given I am logged in
- When I navigate to Study Materials settings
- Then I see an "Enable Equations" toggle/checkbox control
- And I see explanatory text describing which features are affected
- And the default state is "enabled" (checked)

**Technical Notes:**
- Add setting to `Views/StudyMaterials/Manage.cshtml`
- Setting should be visually grouped with other user preferences
- Use Bootstrap form-check or form-switch component

---

### Story 2: Enable Equation Features
**As a** user who uses equation-based study materials  
**I want to** enable equation features in settings  
**So that** Exercise, Graded Exercises, and Equation Flashcards appear on my home page

**Acceptance Criteria:**
- Given I am on the Study Materials settings page
- When I check the "Enable Equations" checkbox
- And I save my preferences
- Then I see a success message
- And when I return to the home page
- Then I see all six study method panels including equation-based ones

**Technical Notes:**
- Save preference to user metadata JSON file
- Add boolean property `EquationsEnabled` to metadata model
- Default value: `true`

---

### Story 3: Disable Equation Features
**As a** user who only uses term/definition study materials  
**I want to** disable equation features in settings  
**So that** Exercise, Graded Exercises, and Equation Flashcards are hidden from my home page

**Acceptance Criteria:**
- Given I am on the Study Materials settings page
- When I uncheck the "Enable Equations" checkbox
- And I save my preferences
- Then I see a success message
- And when I return to the home page
- Then I see only three study method panels: Quiz, Graded Quiz, and Term Flashcards
- And I do NOT see: Exercise, Graded Exercises, or Equation Flashcards panels

**Technical Notes:**
- Home page controller must pass user preference to view
- View uses conditional rendering based on preference
- Grid layout should reflow properly with 3 vs 6 panels

---

### Story 4: Persist Preference Across Sessions
**As a** user who has disabled equations  
**I want to** my preference to be remembered across login sessions  
**So that** I don't need to reconfigure the setting every time I use the app

**Acceptance Criteria:**
- Given I have disabled equations in settings
- When I log out and log back in
- Then my home page still shows only the three term-based study methods
- And the setting remains unchecked in Study Materials settings

**Technical Notes:**
- Preference stored in `App_Data/StudyMaterials/{username}/metadata.json`
- Load preference on each home page request
- No caching issues with stale preferences

---

### Story 5: Default Behavior for New Users
**As a** new user registering for the first time  
**I want to** see all available study methods  
**So that** I can explore all features the app offers

**Acceptance Criteria:**
- Given I am a new user who just registered
- When I first visit the home page
- Then I see all six study method panels
- And when I check Study Materials settings
- Then the "Enable Equations" checkbox is checked by default

**Technical Notes:**
- When metadata.json is first created, `EquationsEnabled = true`
- Existing users without the property default to `true`

---

### Story 6: Validation and Error Handling
**As a** user modifying equation settings  
**I want to** receive clear feedback if something goes wrong  
**So that** I understand the state of my preferences

**Acceptance Criteria:**
- Given I am on the Study Materials settings page
- When I attempt to save my preference but an error occurs
- Then I see an error message explaining the issue
- And my previous preference remains unchanged
- And when the save succeeds
- Then I see a clear success message

**Technical Notes:**
- Handle file I/O errors gracefully
- Log errors for debugging
- Show user-friendly error messages

---

### Story 7: Help Documentation
**As a** user learning about the equations setting  
**I want to** understand what the setting does  
**So that** I can make an informed decision

**Acceptance Criteria:**
- Given I am on the Study Materials settings page
- When I look at the "Enable Equations" setting
- Then I see help text or a tooltip explaining:
  - Which features are affected (Exercise, Graded Exercises, Equation Flashcards)
  - That disabling only hides home page panels
  - That equation files can still be uploaded
  - That features remain accessible via direct URL

**Technical Notes:**
- Use Bootstrap form-text helper or tooltip
- Keep text concise but informative
- Consider link to full help documentation

---

## Non-Functional Requirements

### Performance
- Loading user preference should add negligible time to home page load (<10ms)
- Metadata file operations should be asynchronous
- No caching issues with stale preferences

### Security
- User preferences isolated per username (existing pattern)
- No ability to view or modify other users' preferences
- Validation that authenticated user matches metadata username

### Usability
- Toggle setting should be obvious and easy to understand
- Changes should take effect immediately on next page load
- No page reload required to save the setting (use form POST)

### Reliability
- Default to enabled state if preference cannot be loaded
- Graceful degradation if metadata file is corrupt
- Backward compatible with existing user metadata

## Technical Architecture

### Data Model Changes
**File:** `Models/UserStudyMaterialMetadata.cs` (may need to create)

```csharp
public class UserStudyMaterialMetadata
{
	public string Username { get; set; } = string.Empty;
	public List<UserStudyMaterial> Materials { get; set; } = new();
	public bool EquationsEnabled { get; set; } = true; // NEW
}
```

### Service Changes
**File:** `Services/IUserStudyMaterialService.cs` and implementation

Add methods:
```csharp
Task<bool> GetEquationsEnabledAsync(string username);
Task SetEquationsEnabledAsync(string username, bool enabled);
```

### Controller Changes
1. **StudyMaterialsController** - Add POST action to save preference
2. **HomeController** - Load preference and pass to view

### View Changes
1. **Views/StudyMaterials/Manage.cshtml** - Add equations toggle setting
2. **Views/Home/Index.cshtml** - Conditional rendering of equation panels

## Testing Strategy

### Unit Tests
- Default value is `true` for new metadata
- Metadata serialization/deserialization with new property
- Service methods for getting/setting preference

### Integration Tests
- Full flow: change setting → save → reload home page
- Verify correct panels visible/hidden
- Verify persistence across sessions

### Manual Testing Scenarios
1. New user sees all panels
2. Disable equations → three panels visible
3. Enable equations → six panels visible  
4. Log out, log in → preference persisted
5. Error handling if metadata file locked/corrupted

## Dependencies
- Existing `UserStudyMaterialService`
- Existing metadata JSON file structure
- Bootstrap for form controls

## Risks and Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Breaking existing user metadata | High | Low | Default to `true`, backward compatible deserialization |
| Users can't find disabled features | Medium | Medium | Clear help text, consider "Show All" option |
| Performance impact on home page | Low | Low | Metadata already loaded for study materials |
| Confusion about what "equations" means | Medium | Medium | Comprehensive help text, list affected features |

## Success Metrics
- Number of users who change the default setting (engagement)
- Support tickets related to UI clutter (should decrease)
- User feedback on customization (qualitative)

## Open Questions
1. Should there be a master "Show All" override on the home page?
2. Should we add similar toggles for other feature types in the future?
3. Should disabling equations also disable equation file uploads?

**Decision:** No for all - keep scope limited to home page visibility only.

## Approvals
- **Product Owner:** _Pending_
- **Technical Lead:** _Pending_
- **UX Designer:** _Pending_

---

**Document Version:** 1.0  
**Created:** 2025  
**Last Updated:** 2025
