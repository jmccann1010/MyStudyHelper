# Quick Reference: Disable Equations Feature

## Feature Overview
Users can now toggle equation-based study features on/off from the Study Materials settings page, allowing them to customize which study modes appear on their home page.

## Affected Study Modes

### Hidden When Disabled ❌
- Exercise (equation problem solving)
- Graded Exercises (scored equation assessments)
- Equation Flashcards (formula review)

### Always Visible ✅
- Quiz (multiple-choice questions)
- Graded Quiz (scored quiz assessments)
- Term Flashcards (terms and definitions)

## User Path
1. **Settings** → **Study Materials**
2. Scroll to **"Study Preferences"** section
3. Toggle **"Enable Equation-Based Features"** switch
4. Click **"Save Preferences"**

## Help Documentation Locations

| Page | Section | Content |
|------|---------|---------|
| `Help/Index.cshtml` | Latest Features banner | Announcement of new feature |
| `Help/StudyMaterials.cshtml` | Study Preferences | Detailed instructions and impact |
| `Help/Settings.cshtml` | Study Materials Settings | Visual comparison and use cases |

## Key User Messages

### 🎯 **Focus Your Study**
"If you're preparing for a terms-and-definitions exam, disable equations to focus your home page on Quiz and Term Flashcards only."

### 💾 **Data Preservation**
"Disabling equation features hides them from the home page, but your uploaded equation files remain saved. You can re-enable these features anytime."

### 🔄 **Flexibility**
"Control which study features appear on your home page based on your current study needs."

## Design Philosophy
- **User Control**: Let users customize their experience
- **Fail-Open**: Default to enabled (backward compatible)
- **Non-Destructive**: Hiding features doesn't delete content
- **Clear Communication**: Help documentation explains impact and use cases

## Testing Notes
- Default state: Equations enabled (all 6 study modes visible)
- After toggling off: Only 3 study modes visible on home page
- After toggling back on: All 6 study modes return
- Preference persists across sessions
- New users see equations enabled by default

## Related Documentation
- Feature Spec: `.github/design/feature-disable-equations-spec.md`
- Design Summary: `.github/design/feature-disable-equations-design-summary.md`
- Backend Design: `.github/design/feature-disable-equations-backend-design.md`
- Frontend Design: `.github/design/feature-disable-equations-frontend-design.md`
- Test Coverage: `StudyHelper.Tests/TEST_COVERAGE_REPORT.md`
- Help Updates: `.github/documentation/help-pages-update-summary.md`
