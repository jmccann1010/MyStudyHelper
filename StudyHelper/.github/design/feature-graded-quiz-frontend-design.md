# Graded Quiz - Frontend Design Document

**Feature:** Graded Quiz  
**Branch:** `feature/graded-quiz`  
**Status:** Design Ready for Engineering  
**Last Updated:** 2025-01-25  

---

## Executive Summary

The Graded Quiz frontend consists of four pages that follow the existing StudyHelper design patterns:

1. **Setup Page** — Question count selection
2. **Question Page** — Quiz question display with real-time score tracking
3. **Results Page** — Final score report with question review
4. **Home Page Panel** — New call-to-action card

All pages use Bootstrap 5, Razor templating, and existing CSS patterns. No new design system or JavaScript frameworks required.

---

## Design System & Patterns

### Existing Patterns to Follow

1. **Card Layout** — Used by Flashcard, Equation, Quiz panels
2. **Bootstrap 5 Grid** — Responsive 12-column layout
3. **Razor Page Templating** — Server-side rendering (no JavaScript required for core functionality)
4. **Color Scheme** — Existing theme variables (dark mode, default theme, etc.)
5. **Typography** — Existing h1-h6, body text classes
6. **Button Styling** — Bootstrap btn btn-primary, btn-secondary, btn-outline-*
7. **Form Controls** — Bootstrap form-control, form-select, form-check-input
8. **Icons** — Font Awesome or inline SVG (match existing panels)
9. **Spacing** — Bootstrap utility classes (mt-*, mb-*, p-*, etc.)
10. **Modals/Alerts** — Bootstrap alerts and modals (existing patterns)

---

## Page 1: Setup Page (`/GradedQuiz/Setup`)

### Purpose
User selects the number of questions (1-50) before starting the quiz.

### Layout

```
┌─────────────────────────────────────────────────────────┐
│                    GRADED QUIZ SETUP                    │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  How many questions would you like to answer?           │
│  (Valid range: 1-50)                                    │
│                                                           │
│  ┌─────────────────────────────────────────────────────┐ │
│  │ [Select number of questions ▼]                      │ │
│  │  1, 5, 10, 15, 20, 25, 30, 50                        │ │
│  └─────────────────────────────────────────────────────┘ │
│                                                           │
│  [ Start Quiz ]  [ Cancel ]                              │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

### Components

1. **Page Title**
   - Text: "Graded Quiz Setup"
   - Style: `<h1 class="text-center mb-4">`
   - Icon: Quiz icon (match existing panels)

2. **Instruction Text**
   - Text: "How many questions would you like to answer? (Valid range: 1-50)"
   - Style: `<p class="text-muted">`

3. **Question Count Selector**
   - Type: HTML `<select>` dropdown (preferred) or radio buttons
   - ID: `questionCount`
   - Default: "--Select number of questions--" (placeholder, disabled)
   - Options: 1, 5, 10, 15, 20, 25, 30, 50 (or allow custom input 1-50)
   - Required: Yes
   - Class: `form-select form-select-lg`

4. **Start Button**
   - Text: "Start Quiz"
   - Type: `<button type="submit">`
   - Class: `btn btn-primary btn-lg`
   - Behavior: Validate selection; POST to `/GradedQuiz/StartQuiz`

5. **Cancel Button**
   - Text: "Cancel"
   - Type: `<a href="/">`
   - Class: `btn btn-secondary btn-lg`
   - Behavior: Navigate to home page

6. **Error Display**
   - Location: Above form
   - Style: Bootstrap alert `alert alert-danger`
   - Trigger: Invalid selection (e.g., < 1 or > 50)
   - Message: "Please select a valid number of questions (1-50)."

### Form Structure

```html
<div class="container mt-5">
  <div class="row justify-content-center">
	<div class="col-lg-8">
	  <div class="card shadow">
		<div class="card-body p-5">

		  @if (ViewBag.Error != null)
		  {
			  <div class="alert alert-danger alert-dismissible fade show" role="alert">
				  @ViewBag.Error
				  <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
			  </div>
		  }

		  <h1 class="text-center mb-4">
			<i class="fas fa-clipboard-check"></i> Graded Quiz Setup
		  </h1>

		  <p class="text-muted text-center mb-4">
			How many questions would you like to answer? (Valid range: 1-50)
		  </p>

		  <form method="post" asp-action="StartQuiz">

			<div class="mb-4">
			  <label for="questionCount" class="form-label">Number of Questions</label>
			  <select id="questionCount" name="questionCount" class="form-select form-select-lg" required>
				<option value="" disabled selected>-- Select number of questions --</option>
				<option value="1">1 Question</option>
				<option value="5">5 Questions</option>
				<option value="10">10 Questions</option>
				<option value="15">15 Questions</option>
				<option value="20">20 Questions</option>
				<option value="25">25 Questions</option>
				<option value="30">30 Questions</option>
				<option value="50">50 Questions</option>
			  </select>
			</div>

			<div class="d-flex gap-2 justify-content-center">
			  <button type="submit" class="btn btn-primary btn-lg">
				Start Quiz
			  </button>
			  <a href="/" class="btn btn-secondary btn-lg">
				Cancel
			  </a>
			</div>

		  </form>

		</div>
	  </div>
	</div>
  </div>
</div>
```

### Responsive Behavior
- Desktop: Centered 800px card with large padding
- Tablet: Full width with reduced padding
- Mobile: Full width with smaller buttons (stacked vertically on xs)

---

## Page 2: Question Page (`/GradedQuiz/Question`)

### Purpose
Display a single question with 4 answer options, real-time score tracking, and navigation.

### Layout

```
┌─────────────────────────────────────────────────────────┐
│  GRADED QUIZ                                             │
│  Question 3 of 10   [████░░░░░░] 30%                    │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  Score: ✓ Correct: 2  ✗ Incorrect: 0                    │
│                                                           │
│  ─────────────────────────────────────────────────────  │
│                                                           │
│  What is the definition of "Asset"?                      │
│                                                           │
│  ○ A resource controlled by the entity from which       │
│    future economic benefits are expected.               │
│                                                           │
│  ○ A present obligation of the entity arising from      │
│    past events.                                          │
│                                                           │
│  ○ The residual interest in assets after deducting      │
│    liabilities.                                          │
│                                                           │
│  ○ Increases in economic benefits during the            │
│    period.                                    │
│                                                           │
│  [ ◄ Previous ]   [ Next ► ]                             │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

### Components

1. **Header/Progress Section**
   - Question Number: "Question 3 of 10"
   - Progress Bar: `<div class="progress">`
   - Progress Percentage: Calculated as `(currentQuestion / totalQuestions) * 100`
   - Style: Bootstrap progress component with filled/unfilled bars

2. **Score Display**
   - Location: Below progress bar
   - Format: "✓ Correct: 2  ✗ Incorrect: 0"
   - Icons: ✓ (checkmark) and ✗ (cross) or use Font Awesome
   - Update: Real-time as user navigates
   - Style: `<div class="row">` with `col-6` columns

3. **Question Text**
   - Location: Main content area
   - Style: `<h3>` or `<p class="lead">`
   - Content: Rendered from `QuestionText` property

4. **Answer Options**
   - Type: Radio buttons (one selectable)
   - Count: Always 4 options (A, B, C, D)
   - Labels: Rendered from `AnswerOptions` array
   - Each option in `<div class="form-check">` container
   - Required: Yes (validation prevents Next if not selected)
   - Name: `selectedAnswerIndex` (0-3)
   - Class: `form-check-input`

5. **Navigation Buttons**
   - **Previous Button**
	 - Text: "◄ Previous"
	 - Type: POST (submit form with hidden field `action=Previous`)
	 - Enabled: Except on question 1
	 - Class: `btn btn-outline-secondary`
   - **Next Button** (or Finish on last question)
	 - Text: "Next ►" (or "Finish Quiz" on last question)
	 - Type: POST (submit answer form)
	 - Enabled: Only if answer selected
	 - Class: `btn btn-primary`

6. **Answer Validation**
   - Trigger: Click Next without selecting answer
   - Display: Alert `alert alert-warning`
   - Message: "Please select an answer before continuing."

### Form Structure

```html
<div class="container mt-4">
  <div class="card shadow">

	<!-- Header -->
	<div class="card-header bg-primary text-white">
	  <div class="d-flex justify-content-between align-items-center">
		<h4 class="mb-0">Graded Quiz</h4>
		<span>Question @Model.QuestionNumber of @Model.TotalQuestions</span>
	  </div>
	</div>

	<!-- Progress Bar -->
	<div class="card-body bg-light pb-2">
	  <div class="progress">
		<div class="progress-bar" style="width: @Model.ProgressPercentage%">
		  @Model.ProgressPercentage%
		</div>
	  </div>
	</div>

	<!-- Score Display -->
	<div class="card-body">
	  <div class="row text-center mb-4">
		<div class="col-6">
		  <h5><i class="fas fa-check text-success"></i> Correct: @Model.CorrectCount</h5>
		</div>
		<div class="col-6">
		  <h5><i class="fas fa-times text-danger"></i> Incorrect: @Model.IncorrectCount</h5>
		</div>
	  </div>

	  <hr class="my-4">

	  <!-- Question -->
	  <div class="mb-4">
		<h3 class="lead">@Model.QuestionText</h3>
	  </div>

	  <!-- Validation Alert -->
	  @if (ViewBag.Error != null)
	  {
		  <div class="alert alert-warning alert-dismissible fade show" role="alert">
			  @ViewBag.Error
			  <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
		  </div>
	  }

	  <!-- Answer Form -->
	  <form method="post" asp-action="SubmitAnswer">

		<div class="mb-4">
		  @for (int i = 0; i < Model.AnswerOptions.Count; i++)
		  {
			  <div class="form-check mb-3 p-3 border rounded">
				<input 
				  type="radio" 
				  class="form-check-input" 
				  name="selectedAnswerIndex" 
				  id="answer_@i" 
				  value="@i" 
				  required
				>
				<label class="form-check-label ms-2" for="answer_@i">
				  @Model.AnswerOptions[i]
				</label>
			  </div>
		  }
		</div>

		<!-- Navigation Buttons -->
		<div class="d-flex gap-2 justify-content-center">

		  @if (Model.QuestionNumber > 1)
		  {
			  <button type="submit" name="action" value="Previous" class="btn btn-outline-secondary btn-lg">
				◄ Previous
			  </button>
		  }

		  <button 
			type="submit" 
			name="action" 
			value="@(Model.QuestionNumber == Model.TotalQuestions ? "Finish" : "Next")"
			class="btn btn-primary btn-lg"
		  >
			@if (Model.QuestionNumber == Model.TotalQuestions)
			{
				<span>Finish Quiz ✓</span>
			}
			else
			{
				<span>Next ►</span>
			}
		  </button>

		</div>

	  </form>
	</div>
  </div>
</div>

<!-- Client-side validation script -->
<script>
document.querySelector('form').addEventListener('submit', function(e) {
	const selected = document.querySelector('input[name="selectedAnswerIndex"]:checked');
	if (!selected) {
		e.preventDefault();
		alert('Please select an answer before continuing.');
	}
});
</script>
```

### Responsive Behavior
- Desktop: Full-width card with padding
- Tablet: Adjusted padding and font sizes
- Mobile: Smaller buttons, stacked vertically; larger touch targets for radio buttons

---

## Page 3: Results Page (`/GradedQuiz/Results`)

### Purpose
Display final score, performance rating, and detailed question review.

### Layout

```
┌─────────────────────────────────────────────────────────┐
│                    QUIZ RESULTS                         │
├─────────────────────────────────────────────────────────┤
│                                                           │
│                   EXCELLENT! 🎉                          │
│               You scored 90% (9 out of 10)              │
│                                                           │
│  ✓ Correct: 9           ✗ Incorrect: 1                  │
│                                                           │
│  ─────────────────────────────────────────────────────  │
│                                                           │
│  Question Review:                                        │
│                                                           │
│  Q1: What is...?                                        │
│  Your answer: A (Correct ✓)                             │
│  Explanation: ...                                        │
│                                                           │
│  Q2: What is...?                                        │
│  Your answer: B (Incorrect ✗)                           │
│  Correct answer: C                                      │
│  Explanation: ...                                        │
│                                                           │
│  ...                                                     │
│                                                           │
│  [ Retake Quiz ]   [ Return to Home ]                    │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

### Components

1. **Performance Header**
   - Title: "Quiz Results"
   - Icon: Trophy or confetti based on performance
   - Performance Rating: "EXCELLENT! 🎉" (varies by percentage)
   - Score Display: "You scored 90% (9 out of 10)"
   - Style: Large, centered, highlighted background

2. **Score Summary Cards**
   - Layout: Two-column grid (desktop), stacked (mobile)
   - Card 1: "✓ Correct: 9"
   - Card 2: "✗ Incorrect: 1"
   - Card 3 (optional): Time taken (if tracked)

3. **Performance Rating Badges**
   - 90-100%: "Excellent" (green badge)
   - 80-89%: "Good" (blue badge)
   - 70-79%: "Fair" (yellow badge)
   - 60-69%: "Poor" (orange badge)
   - <60%: "Needs Improvement" (red badge)

4. **Question Review Section**
   - Expandable/collapsible questions (accordion pattern)
   - For each question:
	 - Question number and text
	 - User's answer (with letter and correctness indicator)
	 - Correct answer (if user was incorrect)
	 - Explanation
	 - Visual feedback (green border/bg for correct, red for incorrect)

5. **Navigation Buttons**
   - **Retake Quiz Button**
	 - Text: "Retake Quiz"
	 - Action: POST to `/GradedQuiz/RetakeQuiz`
	 - Class: `btn btn-primary btn-lg`
   - **Return to Home Button**
	 - Text: "Return to Home"
	 - Action: `<a href="/">`
	 - Class: `btn btn-secondary btn-lg`

### Form Structure

```html
<div class="container mt-4">
  <div class="card shadow">

	<!-- Header -->
	<div class="card-header bg-success text-white">
	  <h2 class="text-center mb-0">Quiz Results</h2>
	</div>

	<!-- Performance Section -->
	<div class="card-body bg-light text-center py-5">
	  <h1 class="display-3 mb-2">
		@(Model.Percentage >= 90 ? "🎉 EXCELLENT!" : Model.Percentage >= 80 ? "👍 GREAT!" : "📚 KEEP PRACTICING!")
	  </h1>
	  <p class="lead">You scored <strong>@Model.Percentage%</strong> (@Model.CorrectCount out of @Model.TotalQuestions)</p>
	</div>

	<!-- Score Cards -->
	<div class="card-body">
	  <div class="row mb-4">
		<div class="col-md-6 mb-3">
		  <div class="card border-success bg-light">
			<div class="card-body text-center">
			  <h5><i class="fas fa-check text-success"></i> Correct</h5>
			  <h2 class="text-success">@Model.CorrectCount</h2>
			</div>
		  </div>
		</div>
		<div class="col-md-6 mb-3">
		  <div class="card border-danger bg-light">
			<div class="card-body text-center">
			  <h5><i class="fas fa-times text-danger"></i> Incorrect</h5>
			  <h2 class="text-danger">@Model.IncorrectCount</h2>
			</div>
		  </div>
		</div>
	  </div>

	  <hr class="my-4">

	  <!-- Question Review -->
	  <h3 class="mb-4">Question Review</h3>

	  <div class="accordion" id="questionReview">
		@for (int i = 0; i < Model.Questions.Count; i++)
		{
			var question = Model.Questions[i];
			var userAnswerIndex = Model.UserAnswers.ContainsKey(i) ? Model.UserAnswers[i] : -1;
			var isCorrect = userAnswerIndex == question.CorrectAnswerIndex;
			var borderClass = isCorrect ? "border-success" : "border-danger";
			var bgClass = isCorrect ? "bg-success-light" : "bg-danger-light";

			<div class="accordion-item border @borderClass">
			  <h2 class="accordion-header">
				<button class="accordion-button" type="button" data-bs-toggle="collapse" data-bs-target="#question_@i">
				  <strong>Q@(i + 1):</strong> @question.QuestionText.Substring(0, Math.Min(60, question.QuestionText.Length))...
				  @if (isCorrect)
				  {
					  <span class="badge bg-success ms-2">Correct ✓</span>
				  }
				  else
				  {
					  <span class="badge bg-danger ms-2">Incorrect ✗</span>
				  }
				</button>
			  </h2>
			  <div id="question_@i" class="accordion-collapse collapse" data-bs-parent="#questionReview">
				<div class="accordion-body @bgClass">
				  <p><strong>Your Answer:</strong> @((char)('A' + userAnswerIndex)) - @question.AnswerOptions[userAnswerIndex]</p>
				  @if (!isCorrect)
				  {
					  <p><strong>Correct Answer:</strong> @((char)('A' + question.CorrectAnswerIndex)) - @question.AnswerOptions[question.CorrectAnswerIndex]</p>
				  }
				  <p><strong>Explanation:</strong> @question.Explanation</p>
				</div>
			  </div>
			</div>
		}
	  </div>

	  <hr class="my-4">

	  <!-- Navigation Buttons -->
	  <div class="d-flex gap-2 justify-content-center">
		<form method="post" asp-action="RetakeQuiz" class="d-inline">
		  <button type="submit" class="btn btn-primary btn-lg">
			Retake Quiz
		  </button>
		</form>
		<a href="/" class="btn btn-secondary btn-lg">
		  Return to Home
		</a>
	  </div>

	</div>
  </div>
</div>
```

### Responsive Behavior
- Desktop: Full-width card with sidebar (optional) for performance metrics
- Tablet: Two-column cards side-by-side
- Mobile: Stacked cards, full-width question review

---

## Page 4: Home Page Panel Addition

### Purpose
Add a new call-to-action card on the home page for Graded Quiz, matching existing panel design.

### Location
- Insert after the existing Quiz panel (or in logical order alongside other study tools)
- Use existing `<div class="col-md-4">` grid layout

### Design

```html
<div class="col-md-4 mb-4">
  <div class="card h-100 shadow-sm">
	<div class="card-body text-center">
	  <h5 class="card-title">
		<i class="fas fa-star"></i> Graded Quiz
	  </h5>
	  <p class="card-text">
		Take a scored quiz and track your performance with real-time feedback.
	  </p>
	  <a href="@Url.Action("Setup", "GradedQuiz")" class="btn btn-primary btn-sm">
		Start Graded Quiz
	  </a>
	</div>
  </div>
</div>
```

### Icon
- Use Font Awesome: `<i class="fas fa-star"></i>` (or `fa-clipboard-check`, `fa-trophy`)
- Match color with other panels (primary color)

### Button
- Text: "Start Graded Quiz"
- Size: `btn-sm` (small, matching other panels)
- Link: `/GradedQuiz/Setup`

### Styling
- Card height: `h-100` (match other panels)
- Shadow: `shadow-sm` (consistent with others)
- Padding: Standard Bootstrap card padding
- Text alignment: Center
- Responsive: Uses existing grid system

---

## Shared Components

### ViewModels

**QuizQuestionViewModel.cs**
```csharp
public class QuizQuestionViewModel
{
	public int QuestionNumber { get; set; }
	public int TotalQuestions { get; set; }
	public string QuestionText { get; set; } = string.Empty;
	public List<string> AnswerOptions { get; set; } = new();
	public int CorrectCount { get; set; }
	public int IncorrectCount { get; set; }
	public decimal ProgressPercentage => (decimal)(QuestionNumber - 1) / TotalQuestions * 100;
}
```

**QuizResultViewModel.cs**
```csharp
public class QuizResultViewModel
{
	public int CorrectCount { get; set; }
	public int IncorrectCount { get; set; }
	public int TotalQuestions { get; set; }
	public decimal Percentage { get; set; }
	public string PerformanceRating { get; set; } = string.Empty;
	public List<QuizQuestion> Questions { get; set; } = new();
	public Dictionary<int, int> UserAnswers { get; set; } = new();
}
```

---

## CSS & JavaScript Notes

### CSS
- **No new CSS files required** — use existing Bootstrap + `site.css`
- **Utility Classes:** mt-*, mb-*, p-*, d-flex, justify-content-*, align-items-*
- **Cards:** Bootstrap card component
- **Progress Bar:** Bootstrap progress component
- **Badges:** Bootstrap badge component

### JavaScript
- **Minimal JavaScript** — mostly server-side rendering
- **Client-side Validation:** Simple form validation before submit (prevent empty answer)
- **Accordion:** Bootstrap 5 accordion for question review (built-in, no custom JS needed)
- **No external libraries** — use existing jQuery/Bootstrap JS

---

## Accessibility Considerations

1. **ARIA Labels** — Add `aria-label` to icon buttons
2. **Form Labels** — All inputs have associated `<label>` with `for` attribute
3. **Keyboard Navigation** — All buttons focusable; Tab/Enter works
4. **Color Contrast** — Use Bootstrap color utilities (sufficient WCAG AA contrast)
5. **Alt Text** — Icons are decorative; use `aria-hidden="true"`
6. **Focus Indicators** — Bootstrap default focus outline retained

---

## Files to Create

1. **Views/GradedQuiz/Setup.cshtml**
2. **Views/GradedQuiz/Question.cshtml**
3. **Views/GradedQuiz/Results.cshtml**
4. **ViewModels/QuizQuestionViewModel.cs**
5. **ViewModels/QuizResultViewModel.cs**

---

## Files to Modify

1. **Views/Home/Index.cshtml** — Add Graded Quiz panel

---

## Definition of Done (Frontend)

- ✅ All 3 Graded Quiz pages created and render without errors
- ✅ Setup page validates question count (1-50)
- ✅ Question page displays questions, answers, and score tracking
- ✅ Results page shows score, rating, and question review
- ✅ Home page includes Graded Quiz panel matching design
- ✅ All pages responsive on mobile/tablet/desktop
- ✅ Client-side validation prevents form submission with empty answers
- ✅ No console errors or warnings
- ✅ Accessibility standards met (ARIA, labels, focus)
- ✅ Code review approved

---

## Next Steps

1. **Design Review** — Engineering review of both frontend + backend designs
2. **Engineering Handoff** — Backend + Frontend engineers begin implementation
3. **Integration Testing** — Verify frontend-backend integration
4. **QA Testing** — Full feature testing and coverage validation

