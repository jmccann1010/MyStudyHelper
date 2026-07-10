# Design Summary: Disable Equations Setting

## Overview
This document provides a high-level architectural overview of the "Disable Equations Setting" feature, which allows users to toggle visibility of equation-based study features on the home page.

## Feature Goals
1. Allow users to hide/show equation-based features (Exercise, Graded Exercises, Equation Flashcards)
2. Provide a simple toggle in Study Materials settings
3. Persist user preference across sessions
4. Maintain backward compatibility with existing users
5. Ensure clean UI with proper grid reflow

## Architecture Overview

### High-Level Design
```
┌─────────────┐
│   User      │
└──────┬──────┘
	   │
	   ▼
┌─────────────────────────────────┐
│  Study Materials Settings Page  │
│  - Toggle "Enable Equations"    │
└──────┬──────────────────────────┘
	   │ POST /StudyMaterials/UpdatePreferences
	   ▼
┌─────────────────────────────────┐
│  StudyMaterialsController       │
│  - UpdatePreferences(bool)      │
└──────┬──────────────────────────┘
	   │
	   ▼
┌─────────────────────────────────┐
│  UserStudyMaterialService       │
│  - GetEquationsEnabledAsync()   │
│  - SetEquationsEnabledAsync()   │
└──────┬──────────────────────────┘
	   │
	   ▼
┌─────────────────────────────────┐
│  User Metadata JSON             │
│  {                              │
│    "EquationsEnabled": true     │
│  }                              │
└─────────────────────────────────┘
	   │
	   ▼
┌─────────────────────────────────┐
│  HomeController                 │
│  - Reads preference             │
│  - Passes to view               │
└──────┬──────────────────────────┘
	   │
	   ▼
┌─────────────────────────────────┐
│  Home Page View                 │
│  - Conditional panel rendering  │
│  - 3 or 6 panels visible        │
└─────────────────────────────────┘
```

## Major Design Decisions

### Decision 1: Storage Location
**Choice:** Store in existing user metadata JSON file  
**Rationale:**
- Leverages existing infrastructure
- No database schema changes needed
- Follows established pattern for user-specific settings
- Simple file-based storage adequate for this use case

**Alternatives Considered:**
- Database table: Overkill for single boolean preference
- appsettings.json: Not user-specific
- Browser localStorage: Doesn't persist across devices

### Decision 2: Scope of Disabling
**Choice:** Hide panels on home page only; features remain accessible via direct URL  
**Rationale:**
- Simple implementation (view-level only)
- No security implications
- Users who bookmark direct URLs can still access
- Reduces complexity of route authorization changes

**Alternatives Considered:**
- Disable routes entirely: Too restrictive, breaks bookmarks
- Hide menu items: No menus exist for these features
- Disable file uploads: Out of scope, separate concern

### Decision 3: Default Value
**Choice:** Default to `true` (equations enabled)  
**Rationale:**
- Backward compatible with existing users
- Matches current behavior (all features visible)
- Opt-out model is safer than opt-in for feature discovery

**Alternatives Considered:**
- Default to `false`: Would hide features from existing users
- Prompt on first login: Added complexity, poor UX

### Decision 4: Setting Location
**Choice:** Add to Study Materials settings page (`/StudyMaterials/Manage`)  
**Rationale:**
- Contextually related to study material management
- Page already exists and is auth-protected
- Users managing materials likely to want this control

**Alternatives Considered:**
- Separate Preferences page: Doesn't exist yet, scope creep
- Account settings: Less contextually related
- Home page itself: Clutters the clean interface

### Decision 5: UI Control Type
**Choice:** Bootstrap form-switch (toggle)  
**Rationale:**
- Clear on/off state
- Modern, intuitive UX
- Matches Bootstrap 5 design system
- Mobile-friendly

**Alternatives Considered:**
- Checkbox: Less modern, less clear
- Radio buttons: Overkill for binary choice
- Dropdown: Unnecessary complexity

## Data Model Changes

### New Model: UserStudyMaterialMetadata
```csharp
public class UserStudyMaterialMetadata
{
	public string Username { get; set; } = string.Empty;
	public List<UserStudyMaterial> Materials { get; set; } = new();

	// NEW PROPERTY
	public bool EquationsEnabled { get; set; } = true;
}
```

### Metadata JSON Structure
```json
{
  "Username": "jmccann",
  "Materials": [
	{
	  "MaterialType": "Definitions",
	  "FileName": "definitions.md",
	  "FileSizeBytes": 1024,
	  "UploadedDate": "2025-01-15T10:30:00Z",
	  "FilePath": "...",
	  "FileHash": "..."
	}
  ],
  "EquationsEnabled": true
}
```

## Component Changes

### Backend Components

| Component | Change Type | Description |
|-----------|-------------|-------------|
| `Models/UserStudyMaterialMetadata.cs` | **Create** | New model class for metadata structure |
| `Services/IUserStudyMaterialService.cs` | **Modify** | Add GetEquationsEnabledAsync, SetEquationsEnabledAsync |
| `Services/UserStudyMaterialService.cs` | **Modify** | Implement new methods, update metadata operations |
| `Controllers/StudyMaterialsController.cs` | **Modify** | Add UpdatePreferences action |
| `Controllers/HomeController.cs` | **Modify** | Load preference and pass to view |
| `ViewModels/ManageStudyMaterialsViewModel.cs` | **Modify** | Add EquationsEnabled property |

### Frontend Components

| Component | Change Type | Description |
|-----------|-------------|-------------|
| `Views/StudyMaterials/Manage.cshtml` | **Modify** | Add equations toggle with help text |
| `Views/Home/Index.cshtml` | **Modify** | Conditional rendering of equation panels |

## Backward Compatibility

### Existing Users
- Metadata files without `EquationsEnabled` property will default to `true`
- Deserialization handles missing property gracefully (C# default)
- No migration script needed

### Existing Data
- No breaking changes to metadata structure
- Additive change only (new optional property)

## Security Considerations

### Authentication & Authorization
- Existing `[Authorize]` attribute on controllers sufficient
- User can only modify their own metadata (username validated)
- No new authorization rules needed

### Data Validation
- Boolean value, no injection risk
- Username validated against authenticated identity
- File system access already secured by existing service

### Privacy
- No PII stored
- User preferences isolated per user directory
- No cross-user data access possible

## Performance Considerations

### Home Page Load
- **Additional Cost:** Single metadata file read (already happening for authenticated users)
- **Expected Impact:** < 5ms
- **Optimization:** Metadata could be cached in session (future enhancement)

### Settings Page Save
- **Additional Cost:** Single metadata file write
- **Expected Impact:** < 50ms
- **Optimization:** Already using async file I/O

### Scalability
- File-based storage scales to thousands of users
- No database bottleneck
- Each user's metadata is independent

## Error Handling Strategy

### Metadata Read Errors
- **Scenario:** File not found, corrupt JSON, I/O error
- **Handling:** Default to `true` (equations enabled)
- **Logging:** Log warning with exception details
- **User Impact:** Feature fails open (shows all features)

### Metadata Write Errors
- **Scenario:** Disk full, permission denied, file locked
- **Handling:** Return error to user, don't change state
- **Logging:** Log error with exception details
- **User Impact:** User sees error message, retries

### Missing Property
- **Scenario:** Old metadata without `EquationsEnabled`
- **Handling:** C# default value (`false`) overridden to `true` in deserialization
- **Logging:** No logging needed (normal)
- **User Impact:** None (backward compatible)

## Testing Strategy

### Unit Tests
- `UserStudyMaterialService.GetEquationsEnabledAsync` returns correct value
- `UserStudyMaterialService.SetEquationsEnabledAsync` saves correctly
- Default value is `true` when property missing
- Metadata serialization/deserialization with new property

### Integration Tests
- Full flow: change setting → save → reload home page → verify panels
- Error handling: corrupt metadata file
- Persistence: log out, log in, verify preference retained

### Manual Testing Checklist
- [ ] New user sees all 6 panels
- [ ] Disable equations → only 3 panels visible (Quiz, Graded Quiz, Term Flashcards)
- [ ] Enable equations → all 6 panels visible
- [ ] Setting persists across logout/login
- [ ] Direct URLs to equation features still work when disabled
- [ ] Grid layout reflows properly with 3 vs 6 panels
- [ ] Mobile responsive behavior correct
- [ ] Help text is clear and accurate
- [ ] Success/error messages display correctly

## Deployment Considerations

### Deployment Steps
1. Deploy backend changes (models, services, controllers)
2. Deploy frontend changes (views)
3. No database migration needed
4. No manual data migration needed

### Rollback Plan
- Remove new property from models (backward compatible)
- Revert view changes
- No data cleanup needed (extra property in JSON ignored)

### Monitoring
- Log settings changes for analytics
- Monitor for metadata file errors
- Track usage of the new setting

## Future Enhancements (Out of Scope)

1. **Additional Feature Toggles**
   - Toggle for term-based features
   - Toggle for quiz vs flashcard features
   - Granular per-feature toggles

2. **Caching**
   - Cache metadata in session or memory cache
   - Reduce file I/O on each home page load

3. **Admin Override**
   - Org-level policy to force features on/off
   - Useful for educational institutions

4. **Analytics Dashboard**
   - Track which features are most/least used
   - Inform future feature development

5. **Preference Sync**
   - Sync preferences across devices (if multi-device support added)

## Open Questions Resolved

| Question | Resolution |
|----------|-----------|
| Should direct URLs be blocked? | No - keep scope limited to UI visibility |
| Should equation uploads be disabled? | No - separate concern, out of scope |
| What if metadata file is corrupted? | Fail open (default to enabled), log error |
| Should we add more toggles now? | No - defer to future, avoid scope creep |

## Dependencies

### External Dependencies
- None

### Internal Dependencies
- Existing `UserStudyMaterialService`
- Existing metadata JSON file structure
- Bootstrap 5 form controls

## Success Criteria

1. ✅ User can toggle equations on/off in settings
2. ✅ Home page shows 3 or 6 panels based on preference
3. ✅ Preference persists across sessions
4. ✅ Default is enabled for backward compatibility
5. ✅ No breaking changes to existing functionality
6. ✅ Clean error handling with user feedback
7. ✅ Build succeeds with no warnings
8. ✅ All tests pass

---

**Document Version:** 1.0  
**Author:** Development Team  
**Date:** 2025  
**Status:** Approved
