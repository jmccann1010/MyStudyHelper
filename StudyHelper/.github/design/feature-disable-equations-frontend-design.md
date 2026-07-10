# Frontend Design: Disable Equations Setting

## Overview
This document details the frontend implementation for the equations toggle feature, including view modifications, UI components, and user experience considerations.

## Views to Modify

### 1. Study Materials Management Page
### 2. Home Page

---

## 1. Study Materials Management Page

**File:** `Views/StudyMaterials/Manage.cshtml` (MODIFY)

### Current Structure Analysis
The page currently displays:
- Uploaded study materials list
- Upload form for new materials
- Delete functionality

### Proposed Changes

#### A. Add Preferences Section

Add a new card section for user preferences, placed after the existing materials list and before or after the upload form.

```razor
@model ManageStudyMaterialsViewModel

@{
	ViewData["Title"] = "Manage Study Materials";
}

<!-- Existing success/error message display -->
@if (!string.IsNullOrEmpty(TempData["SuccessMessage"]?.ToString()))
{
	<div class="alert alert-success alert-dismissible fade show" role="alert">
		@TempData["SuccessMessage"]
		<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
	</div>
}

@if (!string.IsNullOrEmpty(TempData["ErrorMessage"]?.ToString()))
{
	<div class="alert alert-danger alert-dismissible fade show" role="alert">
		@TempData["ErrorMessage"]
		<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
	</div>
}

<div class="container mt-4">
	<h1 class="mb-4">
		<i class="bi bi-folder-fill text-primary"></i> Manage Study Materials
	</h1>

	<!-- EXISTING MATERIALS LIST SECTION -->
	<!-- (Keep existing code) -->

	<hr class="my-5">

	<!-- NEW: PREFERENCES SECTION -->
	<div class="card shadow-sm mb-4">
		<div class="card-header bg-light">
			<h5 class="mb-0">
				<i class="bi bi-gear-fill text-secondary"></i> Study Preferences
			</h5>
		</div>
		<div class="card-body">
			<form asp-controller="StudyMaterials" asp-action="UpdatePreferences" method="post">
				@Html.AntiForgeryToken()

				<div class="mb-3">
					<div class="form-check form-switch">
						<input 
							class="form-check-input" 
							type="checkbox" 
							role="switch" 
							id="equationsEnabledSwitch" 
							name="equationsEnabled"
							value="true"
							@(Model.EquationsEnabled ? "checked" : "")
						>
						<label class="form-check-label fw-semibold" for="equationsEnabledSwitch">
							Enable Equation-Based Features
						</label>
					</div>
					<div class="form-text mt-2">
						<i class="bi bi-info-circle text-info"></i>
						When enabled, the following study methods will appear on your home page:
						<ul class="mt-2 mb-0">
							<li><strong>Exercise</strong> - Practice solving equation problems</li>
							<li><strong>Graded Exercises</strong> - Scored equation problem sets</li>
							<li><strong>Equation Flashcards</strong> - Timed equation review</li>
						</ul>
						<small class="text-muted d-block mt-2">
							Note: Disabling this option only hides these features from your home page. 
							They remain accessible via direct links if you have equation files uploaded.
						</small>
					</div>
				</div>

				<div class="d-flex gap-2">
					<button type="submit" class="btn btn-primary">
						<i class="bi bi-check-circle"></i> Save Preferences
					</button>
					<a asp-controller="Home" asp-action="Index" class="btn btn-outline-secondary">
						<i class="bi bi-x-circle"></i> Cancel
					</a>
				</div>
			</form>
		</div>
	</div>

	<!-- EXISTING UPLOAD SECTION -->
	<!-- (Keep existing code) -->
</div>

@section Scripts {
	<!-- Existing scripts -->
}
```

### UI Component Breakdown

#### Form Switch (Toggle)
```html
<div class="form-check form-switch">
	<input class="form-check-input" type="checkbox" role="switch" 
		   id="equationsEnabledSwitch" name="equationsEnabled" value="true"
		   @(Model.EquationsEnabled ? "checked" : "")>
	<label class="form-check-label fw-semibold" for="equationsEnabledSwitch">
		Enable Equation-Based Features
	</label>
</div>
```

**Design Decisions:**
- **Component Type:** Bootstrap 5 form-switch (modern toggle)
- **Label:** Clear, action-oriented ("Enable...")
- **State:** Checked when `Model.EquationsEnabled == true`
- **Accessibility:** `role="switch"` for screen readers

#### Help Text
```html
<div class="form-text mt-2">
	<i class="bi bi-info-circle text-info"></i>
	When enabled, the following study methods will appear on your home page:
	<ul class="mt-2 mb-0">
		<li><strong>Exercise</strong> - Practice solving equation problems</li>
		<li><strong>Graded Exercises</strong> - Scored equation problem sets</li>
		<li><strong>Equation Flashcards</strong> - Timed equation review</li>
	</ul>
	<small class="text-muted d-block mt-2">
		Note: Disabling this option only hides these features from your home page. 
		They remain accessible via direct links if you have equation files uploaded.
	</small>
</div>
```

**Design Decisions:**
- **Icon:** Info circle to indicate helpful information
- **Content:** Bullet list of affected features
- **Clarification:** Explain scope (UI visibility only)
- **Styling:** Bootstrap `form-text` for proper spacing and color

#### Action Buttons
```html
<div class="d-flex gap-2">
	<button type="submit" class="btn btn-primary">
		<i class="bi bi-check-circle"></i> Save Preferences
	</button>
	<a asp-controller="Home" asp-action="Index" class="btn btn-outline-secondary">
		<i class="bi bi-x-circle"></i> Cancel
	</a>
</div>
```

**Design Decisions:**
- **Primary Action:** Save button with check icon
- **Secondary Action:** Cancel (returns to home)
- **Layout:** Flexbox with gap for spacing
- **Icons:** Bootstrap Icons for visual clarity

---

## 2. Home Page

**File:** `Views/Home/Index.cshtml` (MODIFY)

### Current Structure
The home page displays 6 study method panels in a grid:
1. Quiz (col-md-6)
2. Graded Quiz (col-md-6)
3. Exercise (col-md-6) ← **Conditional**
4. Graded Exercises (col-md-6) ← **Conditional**
5. Term Flashcards (col-md-6)
6. Equation Flashcards (col-md-6) ← **Conditional**

### Proposed Changes

#### A. Add Conditional Rendering

Wrap equation-based panels in `@if` blocks:

```razor
@{
	ViewData["Title"] = "Home Page";
	var equationsEnabled = ViewBag.EquationsEnabled ?? true; // Default to true for anonymous users
}

<div class="text-center mb-5">
	<h1 class="display-4 mb-3">Study Helper</h1>
	<p class="lead text-muted">Choose how you want to study today</p>
</div>

<div class="row g-4">
	<!-- Quiz Panel - ALWAYS VISIBLE -->
	<div class="col-md-6">
		<!-- Existing Quiz panel code -->
	</div>

	<!-- Graded Quiz Panel - ALWAYS VISIBLE -->
	<div class="col-md-6">
		<!-- Existing Graded Quiz panel code -->
	</div>

	<!-- Exercise Panel - CONDITIONAL -->
	@if (equationsEnabled)
	{
		<div class="col-md-6">
			<div class="card h-100 shadow-sm border-success">
				<!-- Existing Exercise panel code -->
			</div>
		</div>
	}

	<!-- Graded Exercises Panel - CONDITIONAL -->
	@if (equationsEnabled)
	{
		<div class="col-md-6">
			<div class="card h-100 shadow-sm border-secondary">
				<!-- Existing Graded Exercises panel code -->
			</div>
		</div>
	}

	<!-- Term Flashcards Panel - ALWAYS VISIBLE -->
	<div class="col-md-6">
		<!-- Existing Term Flashcards panel code -->
	</div>

	<!-- Equation Flashcards Panel - CONDITIONAL -->
	@if (equationsEnabled)
	{
		<div class="col-md-6">
			<div class="card h-100 shadow-sm border-warning">
				<!-- Existing Equation Flashcards panel code -->
			</div>
		</div>
	}
</div>

<!-- Existing styles -->
<style>
	.card {
		transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
	}

	.card:hover {
		transform: translateY(-5px);
		box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
	}

	.btn-lg {
		padding: 0.75rem 2rem;
		font-size: 1.1rem;
	}
</style>
```

### Visual Layout Changes

#### All Features Enabled (6 Panels)
```
┌─────────────┬─────────────┐
│    Quiz     │ Graded Quiz │
├─────────────┼─────────────┤
│  Exercise   │   Graded    │
│             │  Exercises  │
├─────────────┼─────────────┤
│    Term     │  Equation   │
│ Flashcards  │ Flashcards  │
└─────────────┴─────────────┘
```

#### Equations Disabled (3 Panels)
```
┌─────────────┬─────────────┐
│    Quiz     │ Graded Quiz │
├─────────────┼─────────────┤
│    Term     │             │
│ Flashcards  │             │
└─────────────┴─────────────┘
```

**Grid Behavior:**
- Bootstrap's `col-md-6` ensures 2 columns on medium+ screens
- On small screens, panels stack vertically (full width)
- Grid naturally reflows with 3 or 6 panels
- No custom CSS needed for layout

---

## Accessibility Considerations

### Form Switch
- ✅ `role="switch"` attribute for ARIA
- ✅ Associated label with `for` attribute
- ✅ Keyboard accessible (native checkbox behavior)
- ✅ Screen reader announces "Enable Equation-Based Features, switch, checked/unchecked"

### Help Text
- ✅ `form-text` class provides semantic association
- ✅ Icon includes descriptive text for screen readers
- ✅ List structure (`<ul>`) properly announced

### Conditional Panels
- ✅ No hidden panels in DOM (removed entirely with `@if`)
- ✅ No confusion for screen reader users
- ✅ Tab order unaffected

---

## Responsive Design

### Desktop (≥768px)
- Toggle switch: Full width within card, comfortable hit target
- Help text: Readable line length, proper spacing
- Home page: 2-column grid (2×3 or 2×1.5)

### Tablet (≥576px, <768px)
- Toggle switch: Same as desktop
- Home page: Panels stack, full width

### Mobile (<576px)
- Toggle switch: Larger hit target (44×44px minimum)
- Help text: Shorter lines, more whitespace
- Home page: Vertical stack, full width

---

## User Experience Flow

### Enable Equations
1. User navigates to `/StudyMaterials/Manage`
2. Sees toggle switch (checked by default)
3. If disabled, checks the toggle
4. Clicks "Save Preferences"
5. Sees success message: "Your preferences have been saved successfully."
6. Clicks "Back to Home" or navigates to home
7. Sees all 6 study method panels

### Disable Equations
1. User navigates to `/StudyMaterials/Manage`
2. Sees toggle switch (checked)
3. Unchecks the toggle
4. Clicks "Save Preferences"
5. Sees success message: "Your preferences have been saved successfully."
6. Clicks "Back to Home" or navigates to home
7. Sees only 3 study method panels (Quiz, Graded Quiz, Term Flashcards)

### Error Handling
1. User attempts to save preference
2. Backend error occurs (file locked, permissions, etc.)
3. Redirects back to `/StudyMaterials/Manage`
4. Sees error message: "Failed to save your preferences. Please try again."
5. Toggle state remains at previous value (unchanged)
6. User can retry

---

## CSS Considerations

### Existing Styles
No additional CSS needed. Relies on:
- Bootstrap 5 grid system (`row`, `col-md-6`, `g-4`)
- Bootstrap form components (`form-check`, `form-switch`)
- Existing card hover animations (preserved)

### Optional Enhancements
If desired, add subtle animation to toggle:

```css
/* Optional: Smooth toggle transition */
.form-check-input[type="checkbox"] {
	transition: background-color 0.15s ease-in-out, 
				border-color 0.15s ease-in-out, 
				box-shadow 0.15s ease-in-out;
}

/* Optional: Highlight toggle when focused */
.form-check-input[type="checkbox"]:focus {
	box-shadow: 0 0 0 0.25rem rgba(13, 110, 253, 0.25);
}
```

*(This is likely already provided by Bootstrap defaults)*

---

## Browser Compatibility

### Supported Browsers
- ✅ Chrome/Edge (latest)
- ✅ Firefox (latest)
- ✅ Safari (latest)
- ✅ Mobile browsers (iOS Safari, Chrome Android)

### Fallbacks
- Form switch degrades to checkbox in older browsers (functional, less pretty)
- Grid layout uses Bootstrap's proven responsive system
- No JavaScript required for core functionality

---

## JavaScript Requirements

### Form Submission
- ✅ Standard HTML form POST (no JavaScript needed)
- ✅ Browser handles form validation (checkbox is never invalid)
- ✅ Anti-forgery token included via Razor helper

### Optional Enhancement
If desired, add confirmation dialog for disabling:

```javascript
document.getElementById('equationsEnabledSwitch').addEventListener('change', function(e) {
	if (!this.checked) {
		const confirmed = confirm(
			'Disabling equations will hide Exercise, Graded Exercises, and Equation Flashcards from your home page. Continue?'
		);
		if (!confirmed) {
			this.checked = true; // Revert
		}
	}
});
```

**Decision:** Not implementing confirmation dialog initially. Setting is easily reversible, no data loss risk.

---

## Success/Error Messages

### Success Message Display
```razor
@if (!string.IsNullOrEmpty(TempData["SuccessMessage"]?.ToString()))
{
	<div class="alert alert-success alert-dismissible fade show" role="alert">
		<i class="bi bi-check-circle-fill me-2"></i>
		@TempData["SuccessMessage"]
		<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
	</div>
}
```

**Styling:**
- Green success alert (Bootstrap `alert-success`)
- Dismissible with close button
- Icon for visual emphasis
- Auto-fade after 5 seconds (optional)

### Error Message Display
```razor
@if (!string.IsNullOrEmpty(TempData["ErrorMessage"]?.ToString()))
{
	<div class="alert alert-danger alert-dismissible fade show" role="alert">
		<i class="bi bi-exclamation-triangle-fill me-2"></i>
		@TempData["ErrorMessage"]
		<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
	</div>
}
```

**Styling:**
- Red danger alert (Bootstrap `alert-danger`)
- Dismissible with close button
- Warning icon for visual emphasis

---

## Testing Checklist

### Visual Testing
- [ ] Toggle switch renders correctly
- [ ] Toggle state matches `Model.EquationsEnabled`
- [ ] Help text is readable and well-formatted
- [ ] Success message displays after save
- [ ] Error message displays on failure
- [ ] Home page shows correct panels (3 or 6)
- [ ] Grid layout reflows properly on all screen sizes

### Functional Testing
- [ ] Checking toggle and saving enables equations
- [ ] Unchecking toggle and saving disables equations
- [ ] Changes persist after logout/login
- [ ] Cancel button returns to home without saving
- [ ] Form submission includes anti-forgery token
- [ ] Preference loads correctly on page load

### Accessibility Testing
- [ ] Toggle is keyboard accessible (Tab, Space)
- [ ] Screen reader announces toggle state
- [ ] Focus indicator visible on toggle
- [ ] Help text associated with toggle
- [ ] Success/error messages announced to screen readers

### Cross-Browser Testing
- [ ] Chrome (Windows/Mac)
- [ ] Firefox (Windows/Mac)
- [ ] Safari (Mac/iOS)
- [ ] Edge (Windows)
- [ ] Chrome Android

---

## Mobile Mockup

```
┌─────────────────────────┐
│  Manage Study Materials │
├─────────────────────────┤
│                         │
│  [Materials List]       │
│                         │
├─────────────────────────┤
│  📁 Study Preferences   │
│                         │
│  ⚪─────────────────     │
│  Enable Equation-Based  │
│  Features               │
│                         │
│  ℹ️ When enabled, the    │
│  following study        │
│  methods will appear    │
│  on your home page:     │
│  • Exercise             │
│  • Graded Exercises     │
│  • Equation Flashcards  │
│                         │
│  ┌─────────────────┐    │
│  │ Save Preferences│    │
│  └─────────────────┘    │
│  ┌─────────────────┐    │
│  │     Cancel      │    │
│  └─────────────────┘    │
└─────────────────────────┘
```

---

## Success Criteria

- ✅ Toggle switch displays in Study Materials settings
- ✅ Help text explains affected features
- ✅ Form saves preference on submit
- ✅ Success/error messages display correctly
- ✅ Home page conditionally renders panels
- ✅ Grid layout reflows properly
- ✅ Responsive on all screen sizes
- ✅ Accessible via keyboard and screen reader
- ✅ No visual regressions
- ✅ No console errors

---

**Document Version:** 1.0  
**Status:** Approved for Implementation
