# Super Quiz Question Count Selection - Frontend Design

## Overview

This document details the frontend architecture for the Super Quiz question count selection feature, including UI/UX design, view structure, client-side JavaScript, and accessibility considerations.

---

## UI/UX Design

### Visual Mockup (Start Page)

```
┌────────────────────────────────────────────────────────────┐
│  Super Quiz                                                │
│  Master all your study materials through practice!         │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ℹ How Super Quiz Works                                   │
│  • Answer questions from your study materials             │
│  • Questions you miss will be asked again                 │
│  • Continue until you've answered every question correctly│
│                                                            │
│  ──────────────────────────────────────────────────────── │
│                                                            │
│  Select Number of Questions:                              │
│                                                            │
│  ○ 10 Questions                                           │
│  ● Half (15 questions)         [SELECTED BY DEFAULT]      │
│  ○ All (30 questions)                                     │
│                                                            │
│  ──────────────────────────────────────────────────────── │
│                                                            │
│  ┌──────────────────┐  ┌──────────────────┐             │
│  │   15 Questions   │  │   4 minutes      │             │
│  │  Total Questions │  │  Estimated Time  │             │
│  └──────────────────┘  └──────────────────┘             │
│                                                            │
│  ┌──────────────────────────────────────┐                │
│  │      ▶ Start Super Quiz              │                │
│  └──────────────────────────────────────┘                │
│  ┌──────────────────────────────────────┐                │
│  │      ← Back to Home                  │                │
│  └──────────────────────────────────────┘                │
└────────────────────────────────────────────────────────────┘
```

### Key UX Principles

1. **Clear Default:** "10 Questions" selected by default for quick practice
2. **Dynamic Feedback:** Question count and estimated time update immediately when selection changes
3. **Visual Clarity:** Selected option visually distinct with radio button and highlighting
4. **Responsive Design:** Mobile-friendly layout with touch-friendly targets
5. **Accessibility:** ARIA labels, keyboard navigation, screen reader support

---

## View Structure

### Updated: Views/SuperQuiz/Start.cshtml

**Location:** `Views/SuperQuiz/Start.cshtml`

```razor
@model SuperQuizStartViewModel
@{
	ViewData["Title"] = "Super Quiz";
}

<div class="container mt-4">
	<div class="row justify-content-center">
		<div class="col-lg-8">
			<div class="card shadow">
				<div class="card-header bg-primary text-white">
					<h2 class="mb-0">
						<i class="bi bi-lightning-charge-fill"></i> Super Quiz
					</h2>
				</div>
				<div class="card-body">
					<p class="lead">Master all your study materials through comprehensive practice!</p>

					<!-- How It Works Section -->
					<div class="alert alert-info">
						<h5><i class="bi bi-info-circle"></i> How Super Quiz Works</h5>
						<ul class="mb-0">
							<li>Answer questions from your study materials</li>
							<li>Questions you miss will be asked again in the next round</li>
							<li>Continue until you've answered <strong>every question correctly</strong></li>
							<li>Track your progress with round-by-round statistics</li>
						</ul>
					</div>

					<!-- Validation Summary -->
					<div asp-validation-summary="All" class="text-danger mb-3"></div>

					<!-- Form Start -->
					<form method="post" asp-action="Start" id="superQuizStartForm">
						@Html.AntiForgeryToken()

						<!-- Question Count Selection -->
						<div class="mb-4">
							<h5 class="mb-3">Select Number of Questions:</h5>

							<div class="form-check mb-3">
								<input class="form-check-input question-option" 
									   type="radio" 
									   name="SelectedOption" 
									   id="option-fixed10" 
									   value="@((int)SuperQuizQuestionCountOption.Fixed10)"
									   data-count="@Model.Fixed10Count"
									   checked="@(Model.SelectedOption == SuperQuizQuestionCountOption.Fixed10)"
									   aria-label="Select 10 questions" />
								<label class="form-check-label" for="option-fixed10">
									<strong>10 Questions</strong>
									<span class="text-muted ms-2" id="label-fixed10">
										(Quick practice session)
									</span>
								</label>
							</div>

							<div class="form-check mb-3">
								<input class="form-check-input question-option" 
									   type="radio" 
									   name="SelectedOption" 
									   id="option-half" 
									   value="@((int)SuperQuizQuestionCountOption.Half)"
									   data-count="@Model.HalfCount"
									   checked="@(Model.SelectedOption == SuperQuizQuestionCountOption.Half)"
									   aria-label="Select half of available questions" />
								<label class="form-check-label" for="option-half">
									<strong>Half</strong>
									<span class="text-muted ms-2" id="label-half">
										(@Model.HalfCount questions)
									</span>
								</label>
							</div>

							<div class="form-check mb-3">
								<input class="form-check-input question-option" 
									   type="radio" 
									   name="SelectedOption" 
									   id="option-all" 
									   value="@((int)SuperQuizQuestionCountOption.All)"
									   data-count="@Model.AllCount"
									   checked="@(Model.SelectedOption == SuperQuizQuestionCountOption.All)"
									   aria-label="Select all available questions" />
								<label class="form-check-label" for="option-all">
									<strong>All</strong>
									<span class="text-muted ms-2" id="label-all">
										(@Model.AllCount questions - Master everything!)
									</span>
								</label>
							</div>

							@* Hidden field for validation if Fixed10 is unavailable *@
							<input type="hidden" id="totalAvailableTerms" value="@Model.TotalAvailableTerms" />
						</div>

						<!-- Preview Statistics -->
						<div class="row text-center my-4">
							<div class="col-md-6 mb-3 mb-md-0">
								<div class="card bg-light">
									<div class="card-body">
										<h3 class="text-primary" id="preview-question-count">
											@Model.GetSelectedQuestionCount()
										</h3>
										<p class="mb-0 text-dark">Total Questions</p>
									</div>
								</div>
							</div>
							<div class="col-md-6">
								<div class="card bg-light">
									<div class="card-body">
										<h3 class="text-primary" id="preview-estimated-time">
											@Model.EstimatedTimeFormatted
										</h3>
										<p class="mb-0 text-dark">Estimated Time</p>
									</div>
								</div>
							</div>
						</div>

						<!-- Submit Buttons -->
						<div class="d-grid gap-2">
							<button type="submit" class="btn btn-primary btn-lg" id="startQuizButton">
								<i class="bi bi-play-circle"></i> Start Super Quiz
							</button>
							<a asp-controller="Home" asp-action="Index" class="btn btn-outline-secondary">
								<i class="bi bi-arrow-left"></i> Back to Home
							</a>
						</div>
					</form>
				</div>
			</div>
		</div>
	</div>
</div>

@section Scripts {
	<script src="~/js/super-quiz-start.js"></script>
}
```

**Design Rationale:**
- Radio buttons use enum integer values for form submission
- `data-count` attributes store question counts for JavaScript updates
- Labels include descriptive text for user guidance
- Preview cards show real-time updates
- Bootstrap form styling for consistency
- Accessibility attributes (`aria-label`) for screen readers

---

## Client-Side JavaScript

### New File: wwwroot/js/super-quiz-start.js

**Location:** `wwwroot/js/super-quiz-start.js`

```javascript
/**
 * Super Quiz Start Page - Dynamic Question Count and Time Updates
 * Listens for radio button changes and updates preview statistics in real-time.
 */

(function () {
	'use strict';

	// Constants
	const SECONDS_PER_QUESTION = 15;
	const MINUTES_PER_QUESTION = SECONDS_PER_QUESTION / 60; // 0.25 minutes

	// DOM Elements
	const radioButtons = document.querySelectorAll('.question-option');
	const previewQuestionCount = document.getElementById('preview-question-count');
	const previewEstimatedTime = document.getElementById('preview-estimated-time');
	const totalAvailableTerms = parseInt(document.getElementById('totalAvailableTerms')?.value || '0');
	const startButton = document.getElementById('startQuizButton');

	/**
	 * Formats minutes into a human-readable time string.
	 * @param {number} minutes - Time in minutes
	 * @returns {string} Formatted time string
	 */
	function formatTime(minutes) {
		if (minutes < 60) {
			return `${Math.round(minutes)} minutes`;
		} else {
			const hours = (minutes / 60).toFixed(1);
			return `${hours} hours`;
		}
	}

	/**
	 * Updates the preview statistics based on the selected option.
	 * @param {number} questionCount - Number of questions
	 */
	function updatePreview(questionCount) {
		// Update question count
		if (previewQuestionCount) {
			previewQuestionCount.textContent = questionCount;
		}

		// Update estimated time
		if (previewEstimatedTime) {
			const estimatedMinutes = questionCount * MINUTES_PER_QUESTION;
			previewEstimatedTime.textContent = formatTime(estimatedMinutes);
		}
	}

	/**
	 * Validates the Fixed10 option against available terms.
	 * Disables the option and shows a warning if insufficient terms.
	 */
	function validateFixed10Option() {
		const fixed10Radio = document.getElementById('option-fixed10');
		const fixed10Label = document.getElementById('label-fixed10');

		if (!fixed10Radio || !fixed10Label) return;

		if (totalAvailableTerms < 10) {
			// Disable Fixed10 option
			fixed10Radio.disabled = true;
			fixed10Label.innerHTML = `<em class="text-danger">(Requires at least 10 terms - you have ${totalAvailableTerms})</em>`;

			// If Fixed10 was selected, auto-select Half
			if (fixed10Radio.checked) {
				const halfRadio = document.getElementById('option-half');
				if (halfRadio) {
					halfRadio.checked = true;
					const halfCount = parseInt(halfRadio.getAttribute('data-count') || '0');
					updatePreview(halfCount);
				}
			}
		}
	}

	/**
	 * Handles radio button change events.
	 * @param {Event} event - Change event
	 */
	function handleOptionChange(event) {
		const selectedRadio = event.target;
		const questionCount = parseInt(selectedRadio.getAttribute('data-count') || '0');

		updatePreview(questionCount);

		// Optional: Log selection for analytics
		console.log(`Super Quiz option selected: ${selectedRadio.id}, Count: ${questionCount}`);
	}

	/**
	 * Initializes the page functionality.
	 */
	function initialize() {
		// Validate Fixed10 option on page load
		validateFixed10Option();

		// Attach change listeners to all radio buttons
		radioButtons.forEach(function (radio) {
			radio.addEventListener('change', handleOptionChange);
		});

		// Set initial preview based on checked radio button
		const checkedRadio = document.querySelector('.question-option:checked');
		if (checkedRadio) {
			const questionCount = parseInt(checkedRadio.getAttribute('data-count') || '0');
			updatePreview(questionCount);
		}

		// Form submission validation (client-side check before server validation)
		const form = document.getElementById('superQuizStartForm');
		if (form) {
			form.addEventListener('submit', function (event) {
				const checkedOption = document.querySelector('.question-option:checked');
				if (!checkedOption) {
					event.preventDefault();
					alert('Please select a question count option.');
					return false;
				}

				// Additional validation: check if Fixed10 is selected with insufficient terms
				if (checkedOption.id === 'option-fixed10' && totalAvailableTerms < 10) {
					event.preventDefault();
					alert(`You need at least 10 terms for the '10 Questions' option. You currently have ${totalAvailableTerms} terms. Please select a different option or add more study materials.`);
					return false;
				}
			});
		}
	}

	// Initialize when DOM is ready
	if (document.readyState === 'loading') {
		document.addEventListener('DOMContentLoaded', initialize);
	} else {
		initialize();
	}

})();
```

**Design Rationale:**
- IIFE pattern prevents global namespace pollution
- Pure JavaScript (no jQuery dependency)
- Validates Fixed10 option on page load
- Auto-selects Half if Fixed10 is unavailable
- Real-time updates without page refresh
- Client-side validation provides immediate feedback
- Console logging for debugging and analytics
- Accessible: works with keyboard navigation and screen readers

---

## CSS Styling

### Optional: Enhanced Radio Button Styling

**Location:** `wwwroot/css/site.css` (or component-specific CSS)

```css
/* Super Quiz Start Page - Question Option Selection */

.question-option:checked + label {
	font-weight: 600;
	color: var(--bs-primary);
}

.question-option:disabled + label {
	opacity: 0.6;
	cursor: not-allowed;
}

.form-check-input:checked {
	background-color: var(--bs-primary);
	border-color: var(--bs-primary);
}

/* Preview cards animation */
.card.bg-light {
	transition: transform 0.2s ease-in-out;
}

.card.bg-light:hover {
	transform: translateY(-2px);
}

/* Preview values update animation */
#preview-question-count,
#preview-estimated-time {
	transition: color 0.3s ease-in-out;
}

/* Highlight change briefly */
@keyframes highlight-change {
	0%, 100% {
		color: var(--bs-primary);
	}
	50% {
		color: var(--bs-success);
	}
}

.preview-updated {
	animation: highlight-change 0.5s ease-in-out;
}
```

**Optional Enhancement:** Add `preview-updated` class in JavaScript when values change:

```javascript
function updatePreview(questionCount) {
	if (previewQuestionCount) {
		previewQuestionCount.textContent = questionCount;
		previewQuestionCount.classList.add('preview-updated');
		setTimeout(() => previewQuestionCount.classList.remove('preview-updated'), 500);
	}
	// ... rest of function
}
```

---

## Responsive Design

### Mobile Layout Considerations

```
Mobile (< 768px):
┌──────────────────────┐
│  Super Quiz          │
├──────────────────────┤
│  ℹ How It Works      │
│  • Bullet points     │
│                      │
│  Select Questions:   │
│  ○ 10 Questions      │
│  ● Half (15)         │
│  ○ All (30)          │
│                      │
│  ┌────────────────┐  │
│  │ 15 Questions   │  │
│  └────────────────┘  │
│  ┌────────────────┐  │
│  │ 4 minutes      │  │
│  └────────────────┘  │
│                      │
│  [Start Super Quiz]  │
│  [Back to Home]      │
└──────────────────────┘
```

**Bootstrap Classes Used:**
- `col-lg-8` centers content on large screens
- `col-md-6` stacks preview cards on mobile
- `mb-3 mb-md-0` adjusts spacing for mobile
- `d-grid` makes buttons full-width on mobile

---

## Accessibility (WCAG 2.1 AA Compliance)

### Keyboard Navigation

| Key | Action |
|-----|--------|
| Tab | Move between form elements |
| Space / Enter | Select radio button |
| Arrow Up/Down | Navigate between radio buttons (native behavior) |
| Enter | Submit form |

### Screen Reader Support

1. **Radio Group:**
   - Grouped by `name="SelectedOption"`
   - Each option has `aria-label` describing the choice

2. **Dynamic Updates:**
   - Consider adding `aria-live="polite"` to preview cards for screen reader announcements

```html
<div class="card-body" aria-live="polite" aria-atomic="true">
	<h3 class="text-primary" id="preview-question-count">15</h3>
	<p class="mb-0 text-dark">Total Questions</p>
</div>
```

3. **Disabled State:**
   - Disabled Fixed10 option includes descriptive text in label
   - Screen readers announce "disabled" state

### Color Contrast

- Ensure text colors meet WCAG AA standards:
  - Normal text: 4.5:1 minimum
  - Large text (18pt+): 3:1 minimum
- Bootstrap default theme meets these requirements
- Test with browser dev tools or axe DevTools

### Focus Indicators

- Browser default focus rings preserved
- Consider custom focus styling for brand consistency:

```css
.form-check-input:focus {
	box-shadow: 0 0 0 0.25rem rgba(13, 110, 253, 0.25);
	outline: none;
}
```

---

## Error Handling and Validation

### Server-Side Validation Errors

When `ModelState` has errors, the view re-renders with:

```razor
<div asp-validation-summary="All" class="text-danger mb-3"></div>
```

**Example Error Messages:**
- "At least 10 terms required for '10 Questions' option. You currently have 8 term(s)."
- "At least 4 questions required. Selected option would generate 2 questions."

### Client-Side Validation

JavaScript provides immediate feedback:
1. Disables Fixed10 if < 10 terms
2. Shows inline warning message
3. Prevents form submission with alert

### User Flow on Error

```
User selects "10 Questions" with only 8 terms
  ↓
Submits form
  ↓
Server validates and returns error
  ↓
View re-renders with:
  - Error message at top
  - User's previous selection preserved
  - Question counts updated
  ↓
User selects "Half" instead
  ↓
Preview updates to 4 questions, 1 minute
  ↓
Submits successfully
```

---

## Testing Strategy

### Manual Testing Checklist

**UI Rendering:**
- [ ] Three radio buttons display correctly
- [ ] Default selection is "10 Questions"
- [ ] Question counts display correct values
- [ ] Estimated time displays correct format

**Dynamic Updates:**
- [ ] Selecting "10 Questions" updates preview to 10 and correct time
- [ ] Selecting "Half" updates preview to half count and correct time
- [ ] Selecting "All" updates preview to all count and correct time
- [ ] Updates happen immediately without page refresh

**Validation:**
- [ ] With < 10 terms, Fixed10 option is disabled
- [ ] With < 10 terms, Fixed10 shows warning message
- [ ] With < 10 terms and Fixed10 auto-selected, Half is chosen instead
- [ ] Submitting with insufficient terms shows server error
- [ ] Error message is clear and actionable

**Responsive Design:**
- [ ] Desktop layout (> 992px): side-by-side preview cards
- [ ] Tablet layout (768-991px): side-by-side preview cards
- [ ] Mobile layout (< 768px): stacked preview cards
- [ ] Buttons are full-width and touch-friendly on mobile

**Accessibility:**
- [ ] Keyboard navigation works (Tab, Arrow keys, Enter)
- [ ] Screen reader announces options correctly
- [ ] Focus indicators are visible
- [ ] Color contrast meets WCAG AA
- [ ] Disabled state is announced by screen reader

**Cross-Browser:**
- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)
- [ ] Mobile Safari (iOS)
- [ ] Chrome Mobile (Android)

---

## Browser Compatibility

### JavaScript Features Used

| Feature | Minimum Browser Version |
|---------|------------------------|
| `const` / `let` | Chrome 49, Firefox 51, Safari 10, Edge 14 |
| Arrow functions | Chrome 45, Firefox 22, Safari 10, Edge 12 |
| `querySelector` | All modern browsers |
| `addEventListener` | All modern browsers |
| Template literals | Chrome 41, Firefox 34, Safari 9, Edge 12 |

**Fallback:** For IE11 support (if required), transpile JavaScript with Babel.

### CSS Features Used

| Feature | Minimum Browser Version |
|---------|------------------------|
| CSS Variables | Chrome 49, Firefox 31, Safari 9.1, Edge 15 |
| Flexbox | All modern browsers |
| Grid | Chrome 57, Firefox 52, Safari 10.1, Edge 16 |
| Transitions | All modern browsers |

**Note:** Bootstrap 5 dropped IE11 support. If IE11 is required, use Bootstrap 4.

---

## Performance Considerations

1. **JavaScript File Size:**
   - Unminified: ~3 KB
   - Minified: ~1 KB
   - Minimal impact on page load

2. **Event Listeners:**
   - Only 3 radio buttons have listeners
   - No performance impact

3. **DOM Updates:**
   - Only 2 elements updated (question count, time)
   - No layout thrashing

4. **Form Submission:**
   - Single POST request (no AJAX)
   - Standard form submission performance

---

## Future Enhancements (Out of Scope)

1. **Animated Transitions:**
   - Smooth number transitions when updating preview
   - Use libraries like CountUp.js

2. **Tooltips:**
   - Hover tooltips explaining each option
   - Use Bootstrap tooltip component

3. **Progress Indicator:**
   - Show visual progress bar based on selected count
   - E.g., "You're about to practice 50% of your terms!"

4. **Mobile Swipe Gestures:**
   - Swipe between options on mobile
   - Requires touch event handling

5. **Saved Preferences:**
   - Remember user's last selection
   - Requires backend preference storage

---

## Implementation Checklist

### View Tasks
- [ ] Update `Views/SuperQuiz/Start.cshtml`
- [ ] Add radio button group for question count options
- [ ] Add `data-count` attributes for JavaScript
- [ ] Update preview cards with IDs
- [ ] Add validation summary placeholder
- [ ] Include `@section Scripts` for JavaScript file

### JavaScript Tasks
- [ ] Create `wwwroot/js/super-quiz-start.js`
- [ ] Implement `updatePreview()` function
- [ ] Implement `validateFixed10Option()` function
- [ ] Implement `handleOptionChange()` event handler
- [ ] Add form submission validation
- [ ] Test with various term counts

### CSS Tasks (Optional)
- [ ] Add enhanced radio button styling
- [ ] Add preview card animations
- [ ] Add highlight-change animation
- [ ] Test color contrast for accessibility

### Testing Tasks
- [ ] Manual testing: all scenarios
- [ ] Cross-browser testing
- [ ] Mobile device testing
- [ ] Accessibility testing (screen reader, keyboard)
- [ ] Automated UI tests (Selenium/Playwright)

---

## Appendix: Alternative UI Designs

### Option A: Dropdown Instead of Radio Buttons

```html
<select name="SelectedOption" id="questionCountSelect" class="form-select">
	<option value="0">10 Questions (Quick practice)</option>
	<option value="1" selected>Half (@Model.HalfCount questions)</option>
	<option value="2">All (@Model.AllCount questions - Master everything!)</option>
</select>
```

**Pros:** More compact, easier on mobile  
**Cons:** Less visual, harder to compare options at a glance

### Option B: Button Group Instead of Radio Buttons

```html
<div class="btn-group w-100" role="group" aria-label="Question count options">
	<input type="radio" class="btn-check" name="SelectedOption" id="btn-fixed10" value="0">
	<label class="btn btn-outline-primary" for="btn-fixed10">10 Questions</label>

	<input type="radio" class="btn-check" name="SelectedOption" id="btn-half" value="1" checked>
	<label class="btn btn-outline-primary" for="btn-half">Half (@Model.HalfCount)</label>

	<input type="radio" class="btn-check" name="SelectedOption" id="btn-all" value="2">
	<label class="btn btn-outline-primary" for="btn-all">All (@Model.AllCount)</label>
</div>
```

**Pros:** Modern, visually distinct, mobile-friendly  
**Cons:** Harder to add descriptive text, requires more CSS

**Recommendation:** Stick with radio buttons for clarity and flexibility.
