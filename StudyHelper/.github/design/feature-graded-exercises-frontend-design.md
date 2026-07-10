# Graded Exercises - Frontend Design Document

**Feature:** Graded Exercises  
**Branch:** `feature/graded-exercises`  
**Status:** Design Ready for Engineering  
**Last Updated:** 2025-01-27  

---

## Executive Summary

The Graded Exercises frontend consists of four pages that follow the existing StudyHelper design patterns, specifically mirroring the Graded Quiz implementation while adapting for numerical input instead of multiple-choice selection.

**Pages:**
1. **Setup Page** — Problem count selection (1-50)
2. **Problem Page** — Exercise problem display with numerical input and real-time score tracking
3. **Results Page** — Final score report with detailed problem review and solutions
4. **Home Page Panel** — New call-to-action card on home page

All pages use Bootstrap 5, Razor templating, and existing CSS patterns. Minimal JavaScript required (form validation only).

---

## Design System & Patterns

### Existing Patterns to Follow

1. **Card Layout** — Used by Graded Quiz, Quiz, Exercise, Flashcard panels
2. **Bootstrap 5 Grid** — Responsive 12-column layout
3. **Razor Page Templating** — Server-side rendering
4. **Color Scheme** — Use existing theme variables (supports dark mode)
5. **Typography** — Existing h1-h6, body text classes
6. **Button Styling** — Bootstrap `btn btn-primary`, `btn-secondary`, `btn-outline-*`
7. **Form Controls** — Bootstrap `form-control`, `form-select`, `input-group`
8. **Icons** — Bootstrap Icons (inline SVG matching existing panels)
9. **Spacing** — Bootstrap utility classes (`mt-*`, `mb-*`, `p-*`, etc.)
10. **Alerts** — Bootstrap alerts for errors/messages

### Color Scheme for Graded Exercises

**Primary Color:** `border-secondary` (gray theme to differentiate from other features)
- Quiz: `border-primary` (blue)
- Graded Quiz: `border-danger` (red)
- Exercise: `border-success` (green)
- Flashcards: `border-info` (teal)
- **Graded Exercises:** `border-secondary` (gray/dark)

**Alternative:** `border-warning` (orange) if gray feels too neutral

---

## Page 1: Setup Page (`/GradedExercise/Setup`)

### Purpose
User selects the number of problems (1-50) before starting the graded exercise session.

### Layout

```
┌─────────────────────────────────────────────────────────┐
│              GRADED EXERCISES SETUP                     │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  [Calculator Icon]                                       │
│                                                           │
│  How many problems would you like to solve?             │
│  (Select between 1 and 50 problems)                     │
│                                                           │
│  ┌─────────────────────────────────────────────────────┐ │
│  │ [Select number of problems ▼]                      │ │
│  │  1, 5, 10, 15, 20, 25, 30, 50                        │ │
│  └─────────────────────────────────────────────────────┘ │
│                                                           │
│  [ Start Graded Exercises ]  [ Cancel ]                  │
│                                                           │
│  ⚠ Error: [Error message if validation fails]           │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

### HTML Structure

**File:** `Views/GradedExercise/Setup.cshtml`

```razor
@{
	ViewData["Title"] = "Graded Exercises Setup";
}

<div class="container mt-4">
	<div class="card shadow">
		<div class="card-header bg-secondary text-white">
			<div class="text-center">
				<svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" fill="currentColor" class="bi bi-calculator-fill mb-2" viewBox="0 0 16 16">
					<path d="M2 2a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2zm2 .5v2a.5.5 0 0 0 .5.5h7a.5.5 0 0 0 .5-.5v-2a.5.5 0 0 0-.5-.5h-7a.5.5 0 0 0-.5.5zm0 4v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5zM4.5 9a.5.5 0 0 0-.5.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1zM4 12.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5zM7.5 6a.5.5 0 0 0-.5.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1zM7 9.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5zm.5 2.5a.5.5 0 0 0-.5.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1zM10 6.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5zm.5 2.5a.5.5 0 0 0-.5.5v4a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-4a.5.5 0 0 0-.5-.5h-1z"/>
				</svg>
				<h2 class="mb-0">Graded Exercises Setup</h2>
			</div>
		</div>

		<div class="card-body">
			@if (!string.IsNullOrEmpty(ViewBag.Error as string))
			{
				<div class="alert alert-danger alert-dismissible fade show" role="alert">
					<strong>Error:</strong> @ViewBag.Error
					<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
				</div>
			}

			<form asp-action="StartExercise" method="post" class="needs-validation" novalidate>
				<div class="mb-4">
					<label for="problemCount" class="form-label h5">How many problems would you like to solve?</label>
					<p class="text-muted">Select between 1 and 50 problems for your graded exercise session.</p>

					<select class="form-select form-select-lg" id="problemCount" name="problemCount" required>
						<option value="" selected disabled>-- Select number of problems --</option>
						<option value="1">1 problem</option>
						<option value="5">5 problems</option>
						<option value="10">10 problems</option>
						<option value="15">15 problems</option>
						<option value="20">20 problems</option>
						<option value="25">25 problems</option>
						<option value="30">30 problems</option>
						<option value="50">50 problems</option>
					</select>
					<div class="invalid-feedback">
						Please select a number of problems.
					</div>
				</div>

				<div class="d-flex justify-content-center gap-3">
					<button type="submit" class="btn btn-secondary btn-lg px-5">
						<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-play-circle me-2" viewBox="0 0 16 16" style="vertical-align: text-top;">
							<path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
							<path d="M6.271 5.055a.5.5 0 0 1 .52.038l3.5 2.5a.5.5 0 0 1 0 .814l-3.5 2.5A.5.5 0 0 1 6 10.5v-5a.5.5 0 0 1 .271-.445z"/>
						</svg>
						Start Graded Exercises
					</button>
					<a href="@Url.Action("Index", "Home")" class="btn btn-outline-secondary btn-lg px-5">
						Cancel
					</a>
				</div>
			</form>
		</div>
	</div>
</div>

@section Scripts {
	<script>
		// Bootstrap form validation
		(function() {
			'use strict';
			var forms = document.querySelectorAll('.needs-validation');
			Array.prototype.slice.call(forms).forEach(function(form) {
				form.addEventListener('submit', function(event) {
					if (!form.checkValidity()) {
						event.preventDefault();
						event.stopPropagation();
					}
					form.classList.add('was-validated');
				}, false);
			});
		})();
	</script>
}
```

### Components

1. **Page Header**
   - Icon: Calculator (filled) from Bootstrap Icons
   - Title: "Graded Exercises Setup"
   - Background: `bg-secondary text-white`

2. **Error Alert**
   - Type: Bootstrap alert `alert-danger`
   - Dismissible: Yes
   - Trigger: Invalid problem count, no equation file, etc.
   - Message from `ViewBag.Error`

3. **Problem Count Selector**
   - Type: `<select>` dropdown
   - Options: 1, 5, 10, 15, 20, 25, 30, 50
   - Default: Placeholder "-- Select number of problems --"
   - Required: Yes
   - Validation: Bootstrap client-side validation

4. **Start Button**
   - Text: "Start Graded Exercises"
   - Icon: Play circle
   - Class: `btn btn-secondary btn-lg`
   - Action: POST to `/GradedExercise/StartExercise`

5. **Cancel Button**
   - Text: "Cancel"
   - Class: `btn btn-outline-secondary btn-lg`
   - Action: Navigate to home page

---

## Page 2: Problem Page (`/GradedExercise/Problem`)

### Purpose
Display current exercise problem with equation, given values, and numerical input field. Show real-time score tracking.

### Layout

```
┌─────────────────────────────────────────────────────────┐
│                  GRADED EXERCISES                       │
│              Problem 3 of 10                            │
├─────────────────────────────────────────────────────────┤
│  Progress: [████████░░░░░░░░░░] 20%                    │
├─────────────────────────────────────────────────────────┤
│  Current Score                                          │
│  ✓ Correct: 2    ✗ Incorrect: 0                        │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  Module: Financial Ratios                               │
│                                                           │
│  Equation: Current Ratio = Current Assets ÷ Current     │
│            Liabilities                                   │
│                                                           │
│  Given Values:                                           │
│    • Current Assets = $45,000                           │
│    • Current Liabilities = $30,000                      │
│                                                           │
│  Solve for: Current Ratio                               │
│                                                           │
│  Your Answer:                                            │
│  ┌─────────────────────────────────────────────────────┐ │
│  │ [      1.50      ]                                  │ │
│  └─────────────────────────────────────────────────────┘ │
│                                                           │
│  [ Submit Answer ]                                       │
│                                                           │
│  ⚠ Error: [Validation error if any]                     │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

### HTML Structure

**File:** `Views/GradedExercise/Problem.cshtml`

```razor
@model GradedExerciseProblemViewModel

@{
	ViewData["Title"] = "Graded Exercise Problem";
}

<div class="container mt-4">
	<div class="card shadow">

		<!-- Header -->
		<div class="card-header bg-secondary text-white">
			<div class="d-flex justify-content-between align-items-center">
				<h4 class="mb-0">Graded Exercises</h4>
				<span>Problem @Model.ProblemNumber of @Model.TotalProblems</span>
			</div>
		</div>

		<!-- Progress Bar -->
		<div class="card-body bg-light pb-2">
			<div class="progress">
				<div class="progress-bar bg-secondary" style="width: @Model.ProgressPercentage%">
					@Math.Round(Model.ProgressPercentage)%
				</div>
			</div>
		</div>

		<!-- Score Display -->
		<div class="card-body">
			<div class="row text-center mb-4">
				<div class="col-6">
					<div class="d-flex align-items-center justify-content-center">
						<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" class="bi bi-check-circle-fill text-success me-2" viewBox="0 0 16 16">
							<path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 5.384 7.323a.75.75 0 0 0-1.06 1.06L6.97 11.03a.75.75 0 0 0 1.079-.02l3.992-4.99a.75.75 0 0 0-.01-1.05z"/>
						</svg>
						<div>
							<h2 class="mb-0 text-success">@Model.CorrectCount</h2>
							<small class="text-muted">Correct</small>
						</div>
					</div>
				</div>
				<div class="col-6">
					<div class="d-flex align-items-center justify-content-center">
						<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" class="bi bi-x-circle-fill text-danger me-2" viewBox="0 0 16 16">
							<path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zM5.354 4.646a.5.5 0 1 0-.708.708L7.293 8l-2.647 2.646a.5.5 0 0 0 .708.708L8 8.707l2.646 2.647a.5.5 0 0 0 .708-.708L8.707 8l2.647-2.646a.5.5 0 0 0-.708-.708L8 7.293 5.354 4.646z"/>
						</svg>
						<div>
							<h2 class="mb-0 text-danger">@Model.IncorrectCount</h2>
							<small class="text-muted">Incorrect</small>
						</div>
					</div>
				</div>
			</div>

			<hr />

			<!-- Problem Display -->
			<div class="mb-4">
				@if (!string.IsNullOrEmpty(TempData["ErrorMessage"] as string))
				{
					<div class="alert alert-danger alert-dismissible fade show" role="alert">
						<strong>Error:</strong> @TempData["ErrorMessage"]
						<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
					</div>
				}

				<h5 class="mb-3">@Model.ProblemText</h5>

				@if (Model.GivenValues != null && Model.GivenValues.Any())
				{
					<div class="mb-3">
						<strong>Given Values:</strong>
						<ul class="mt-2">
							@foreach (var (variable, value) in Model.GivenValues)
							{
								<li>@variable = @value.ToString("N2")</li>
							}
						</ul>
					</div>
				}

				<div class="mb-3">
					<strong>Solve for:</strong> @Model.SolveForVariable
				</div>
			</div>

			<!-- Answer Input Form -->
			<form asp-action="SubmitAnswer" method="post" class="needs-validation" novalidate>
				<div class="mb-4">
					<label for="userAnswer" class="form-label h5">Your Answer:</label>
					<div class="input-group input-group-lg">
						<input type="text" 
							   class="form-control" 
							   id="userAnswer" 
							   name="userAnswer" 
							   placeholder="Enter your answer (e.g., 1.50)" 
							   required 
							   pattern="^-?\d+(\.\d+)?$"
							   inputmode="decimal"
							   autocomplete="off" />
						<div class="invalid-feedback">
							Please enter a valid number.
						</div>
					</div>
					<small class="form-text text-muted">
						Enter numerical answer only. Do not include units, $, %, or commas.
					</small>
				</div>

				<div class="d-flex justify-content-center">
					<button type="submit" class="btn btn-secondary btn-lg px-5">
						<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-arrow-right-circle me-2" viewBox="0 0 16 16" style="vertical-align: text-top;">
							<path fill-rule="evenodd" d="M1 8a7 7 0 1 0 14 0A7 7 0 0 0 1 8zm15 0A8 8 0 1 1 0 8a8 8 0 0 1 16 0zM4.5 7.5a.5.5 0 0 0 0 1h5.793l-2.147 2.146a.5.5 0 0 0 .708.708l3-3a.5.5 0 0 0 0-.708l-3-3a.5.5 0 1 0-.708.708L10.293 7.5H4.5z"/>
						</svg>
						Submit Answer
					</button>
				</div>
			</form>
		</div>
	</div>
</div>

@section Scripts {
	<script>
		// Bootstrap form validation
		(function() {
			'use strict';
			var forms = document.querySelectorAll('.needs-validation');
			Array.prototype.slice.call(forms).forEach(function(form) {
				form.addEventListener('submit', function(event) {
					if (!form.checkValidity()) {
						event.preventDefault();
						event.stopPropagation();
					}
					form.classList.add('was-validated');
				}, false);
			});
		})();

		// Focus input on load
		document.getElementById('userAnswer').focus();
	</script>
}
```

### Components

1. **Header**
   - Title: "Graded Exercises"
   - Progress: "Problem X of Y"
   - Background: `bg-secondary text-white`

2. **Progress Bar**
   - Bootstrap progress bar
   - Width calculated from `ProgressPercentage`
   - Color: `bg-secondary`

3. **Score Display**
   - Two columns: Correct (green) / Incorrect (red)
   - Icons: Check circle (success) / X circle (danger)
   - Large numbers with labels

4. **Problem Display**
   - Module name (if available)
   - Equation text
   - Given values (bulleted list)
   - Variable to solve for

5. **Answer Input**
   - Type: `text` with `inputmode="decimal"` for mobile
   - Pattern: `^-?\d+(\.\d+)?$` (decimal validation)
   - Placeholder: "Enter your answer (e.g., 1.50)"
   - Required: Yes
   - Helper text: "Enter numerical answer only..."

6. **Submit Button**
   - Text: "Submit Answer"
   - Icon: Arrow right circle
   - Class: `btn btn-secondary btn-lg`
   - Action: POST to `/GradedExercise/SubmitAnswer`

### ViewModel

**File:** `ViewModels/GradedExerciseProblemViewModel.cs`

```csharp
namespace StudyHelper.ViewModels;

/// <summary>
/// ViewModel for displaying a graded exercise problem with score tracking.
/// </summary>
public class GradedExerciseProblemViewModel
{
	/// <summary>
	/// Current problem number (1-based for display).
	/// </summary>
	public int ProblemNumber { get; set; }

	/// <summary>
	/// Total problems in the graded exercise session.
	/// </summary>
	public int TotalProblems { get; set; }

	/// <summary>
	/// The problem text to display.
	/// </summary>
	public string ProblemText { get; set; } = string.Empty;

	/// <summary>
	/// Given values for the problem (variable name to value mapping).
	/// </summary>
	public Dictionary<string, decimal> GivenValues { get; set; } = new();

	/// <summary>
	/// The variable the user needs to solve for.
	/// </summary>
	public string SolveForVariable { get; set; } = string.Empty;

	/// <summary>
	/// Number of problems answered correctly so far.
	/// </summary>
	public int CorrectCount { get; set; }

	/// <summary>
	/// Number of problems answered incorrectly so far.
	/// </summary>
	public int IncorrectCount { get; set; }

	/// <summary>
	/// Gets the exercise completion percentage (0-100).
	/// </summary>
	public decimal ProgressPercentage => TotalProblems > 0
		? (decimal)(ProblemNumber - 1) / TotalProblems * 100
		: 0;
}
```

---

## Page 3: Results Page (`/GradedExercise/Results`)

### Purpose
Display final score, performance rating, and detailed review of all problems with solutions.

### Layout

```
┌─────────────────────────────────────────────────────────┐
│              GRADED EXERCISES RESULTS                   │
├─────────────────────────────────────────────────────────┤
│                                                           │
│           Your Score: 8 / 10 (80%)                       │
│           Performance: Good                              │
│                                                           │
│  [ Retake Exercises ]  [ Back to Home ]                  │
│                                                           │
├─────────────────────────────────────────────────────────┤
│  Problem Review                                          │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  Problem 1: Current Ratio                               │
│  ✓ Correct                                               │
│  Your Answer: 1.50    Correct Answer: 1.50             │
│  Given: Current Assets = $45,000, ...                   │
│  [Show Solution Steps ▼]                                │
│                                                           │
│  Problem 2: Gross Profit Margin                         │
│  ✗ Incorrect                                             │
│  Your Answer: 0.35    Correct Answer: 0.40             │
│  Given: Revenue = $100,000, COGS = $60,000             │
│  [Show Solution Steps ▼]                                │
│                                                           │
│  ... (all problems listed)                              │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

### HTML Structure

**File:** `Views/GradedExercise/Results.cshtml`

```razor
@model GradedExerciseResultViewModel

@{
	ViewData["Title"] = "Graded Exercises Results";
}

<div class="container mt-4">
	<div class="card shadow">

		<!-- Header -->
		<div class="card-header bg-secondary text-white text-center">
			<h2 class="mb-0">Graded Exercises Results</h2>
		</div>

		<!-- Score Summary -->
		<div class="card-body bg-light">
			<div class="text-center py-4">
				<h1 class="display-3 mb-3">
					@Model.CorrectCount / @Model.TotalProblems
				</h1>
				<h2 class="mb-3">
					@Model.Percentage.ToString("F1")%
				</h2>
				<h4 class="mb-0">
					Performance: 
					<span class="@GetPerformanceBadgeClass(Model.PerformanceRating)">
						@Model.PerformanceRating
					</span>
				</h4>
			</div>

			<div class="d-flex justify-content-center gap-3 mt-4">
				<a asp-action="RetakeExercise" class="btn btn-secondary btn-lg px-5">
					<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-arrow-clockwise me-2" viewBox="0 0 16 16" style="vertical-align: text-top;">
						<path fill-rule="evenodd" d="M8 3a5 5 0 1 0 4.546 2.914.5.5 0 0 1 .908-.417A6 6 0 1 1 8 2v1z"/>
						<path d="M8 4.466V.534a.25.25 0 0 1 .41-.192l2.36 1.966c.12.1.12.284 0 .384L8.41 4.658A.25.25 0 0 1 8 4.466z"/>
					</svg>
					Retake Exercises
				</a>
				<a asp-controller="Home" asp-action="Index" class="btn btn-outline-secondary btn-lg px-5">
					<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-house-door me-2" viewBox="0 0 16 16" style="vertical-align: text-top;">
						<path d="M8.354 1.146a.5.5 0 0 0-.708 0l-6 6A.5.5 0 0 0 1.5 7.5v7a.5.5 0 0 0 .5.5h4.5a.5.5 0 0 0 .5-.5v-4h2v4a.5.5 0 0 0 .5.5H14a.5.5 0 0 0 .5-.5v-7a.5.5 0 0 0-.146-.354L13 5.793V2.5a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5v1.293L8.354 1.146zM2.5 14V7.707l5.5-5.5 5.5 5.5V14H10v-4a.5.5 0 0 0-.5-.5h-3a.5.5 0 0 0-.5.5v4H2.5z"/>
					</svg>
					Back to Home
				</a>
			</div>
		</div>

		<!-- Problem Review Section -->
		<div class="card-body">
			<h4 class="mb-4">Problem Review</h4>

			@for (int i = 0; i < Model.Problems.Count; i++)
			{
				var problem = Model.Problems[i];
				var userAnswer = Model.UserAnswers.ContainsKey(i) ? Model.UserAnswers[i] : 0;
				var isCorrect = Math.Abs(userAnswer - problem.CorrectAnswer) <= 0.01m;

				<div class="card mb-3 @(isCorrect ? "border-success" : "border-danger")">
					<div class="card-header @(isCorrect ? "bg-success bg-opacity-10" : "bg-danger bg-opacity-10")">
						<div class="d-flex justify-content-between align-items-center">
							<h5 class="mb-0">
								Problem @(i + 1): @problem.Equation.Name
							</h5>
							@if (isCorrect)
							{
								<span class="badge bg-success">
									<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-check-lg me-1" viewBox="0 0 16 16">
										<path d="M12.736 3.97a.733.733 0 0 1 1.047 0c.286.289.29.756.01 1.05L7.88 12.01a.733.733 0 0 1-1.065.02L3.217 8.384a.757.757 0 0 1 0-1.06.733.733 0 0 1 1.047 0l3.052 3.093 5.4-6.425a.247.247 0 0 1 .02-.022Z"/>
									</svg>
									Correct
								</span>
							}
							else
							{
								<span class="badge bg-danger">
									<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-x-lg me-1" viewBox="0 0 16 16">
										<path d="M2.146 2.854a.5.5 0 1 1 .708-.708L8 7.293l5.146-5.147a.5.5 0 0 1 .708.708L8.707 8l5.147 5.146a.5.5 0 0 1-.708.708L8 8.707l-5.146 5.147a.5.5 0 0 1-.708-.708L7.293 8 2.146 2.854Z"/>
									</svg>
									Incorrect
								</span>
							}
						</div>
					</div>

					<div class="card-body">
						<p class="mb-2"><strong>Problem:</strong> @problem.ProblemText</p>

						@if (problem.GivenValues != null && problem.GivenValues.Any())
						{
							<p class="mb-2">
								<strong>Given:</strong>
								@string.Join(", ", problem.GivenValues.Select(kv => $"{kv.Key} = {kv.Value:N2}"))
							</p>
						}

						<div class="row mt-3">
							<div class="col-md-6">
								<p class="mb-0">
									<strong>Your Answer:</strong>
									<span class="@(isCorrect ? "text-success" : "text-danger")">
										@userAnswer.ToString("N2")
									</span>
								</p>
							</div>
							<div class="col-md-6">
								<p class="mb-0">
									<strong>Correct Answer:</strong>
									<span class="text-success">@problem.CorrectAnswer.ToString("N2")</span>
								</p>
							</div>
						</div>

						@if (!string.IsNullOrEmpty(problem.SolutionSteps))
						{
							<div class="mt-3">
								<button class="btn btn-sm btn-outline-secondary" type="button" data-bs-toggle="collapse" data-bs-target="#solution-@i">
									Show Solution Steps
								</button>
								<div class="collapse mt-2" id="solution-@i">
									<div class="card card-body bg-light">
										@Html.Raw(problem.SolutionSteps.Replace("\n", "<br />"))
									</div>
								</div>
							</div>
						}
					</div>
				</div>
			}
		</div>
	</div>
</div>

@functions {
	private string GetPerformanceBadgeClass(string rating)
	{
		return rating switch
		{
			"Excellent" => "badge bg-success",
			"Good" => "badge bg-primary",
			"Fair" => "badge bg-info",
			"Poor" => "badge bg-warning",
			"Needs Improvement" => "badge bg-danger",
			_ => "badge bg-secondary"
		};
	}
}
```

### ViewModel

**File:** `ViewModels/GradedExerciseResultViewModel.cs`

```csharp
using StudyHelper.Models;

namespace StudyHelper.ViewModels;

/// <summary>
/// ViewModel for displaying graded exercise results with score and problem review.
/// </summary>
public class GradedExerciseResultViewModel
{
	/// <summary>
	/// Number of problems answered correctly.
	/// </summary>
	public int CorrectCount { get; set; }

	/// <summary>
	/// Number of problems answered incorrectly.
	/// </summary>
	public int IncorrectCount { get; set; }

	/// <summary>
	/// Total problems in the exercise.
	/// </summary>
	public int TotalProblems { get; set; }

	/// <summary>
	/// Percentage score (0-100).
	/// </summary>
	public decimal Percentage { get; set; }

	/// <summary>
	/// Performance rating (Excellent, Good, Fair, Poor, Needs Improvement).
	/// </summary>
	public string PerformanceRating { get; set; } = string.Empty;

	/// <summary>
	/// All problems from the exercise for review.
	/// </summary>
	public List<ExerciseProblem> Problems { get; set; } = new();

	/// <summary>
	/// User's answers (problem index to answer mapping).
	/// </summary>
	public Dictionary<int, decimal> UserAnswers { get; set; } = new();
}
```

---

## Page 4: Home Page Panel

### Purpose
Add a new panel to the home page providing access to Graded Exercises feature.

### Panel Design

**Location:** After "Equation Flashcards" panel (6th panel)

**File:** `Views/Home/Index.cshtml` (add new section)

```razor
<!-- Graded Exercises Panel -->
<div class="col-md-6">
	<div class="card h-100 shadow-sm border-secondary">
		<div class="card-body d-flex flex-column">
			<div class="text-center mb-3">
				<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" fill="currentColor" class="bi bi-calculator-fill text-secondary" viewBox="0 0 16 16">
					<path d="M2 2a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2zm2 .5v2a.5.5 0 0 0 .5.5h7a.5.5 0 0 0 .5-.5v-2a.5.5 0 0 0-.5-.5h-7a.5.5 0 0 0-.5.5zm0 4v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5zM4.5 9a.5.5 0 0 0-.5.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1zM4 12.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5zM7.5 6a.5.5 0 0 0-.5.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1zM7 9.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5zm.5 2.5a.5.5 0 0 0-.5.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1zM10 6.5v1a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-1a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5zm.5 2.5a.5.5 0 0 0-.5.5v4a.5.5 0 0 0 .5.5h1a.5.5 0 0 0 .5-.5v-4a.5.5 0 0 0-.5-.5h-1z"/>
				</svg>
			</div>
			<h2 class="card-title text-center mb-3">Graded Exercises</h2>
			<p class="card-text text-center flex-grow-1">
				Take a graded exercise session to test your equation-solving skills. 
				Get scored feedback on each problem and review detailed solutions at the end.
			</p>
			<div class="text-center mt-3">
				<a asp-controller="GradedExercise" asp-action="Setup" class="btn btn-secondary btn-lg px-5">
					<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-play-circle me-2" viewBox="0 0 16 16" style="vertical-align: text-top;">
						<path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
						<path d="M6.271 5.055a.5.5 0 0 1 .52.038l3.5 2.5a.5.5 0 0 1 0 .814l-3.5 2.5A.5.5 0 0 1 6 10.5v-5a.5.5 0 0 1 .271-.445z"/>
					</svg>
					Start Graded Exercises
				</a>
			</div>
		</div>
	</div>
</div>
```

### Panel Specifications

- **Title:** "Graded Exercises"
- **Icon:** Calculator (filled), color `text-secondary`
- **Border:** `border-secondary`
- **Button:** `btn btn-secondary` (consistent with panel color)
- **Description:** "Take a graded exercise session to test your equation-solving skills. Get scored feedback on each problem and review detailed solutions at the end."
- **Button Text:** "Start Graded Exercises"
- **Link:** `/GradedExercise/Setup`

---

## Responsive Design

### Mobile (< 768px)
- Cards stack vertically
- Form inputs full width
- Buttons stack vertically with gap
- Font sizes scale down (Bootstrap default)

### Tablet (768px - 991px)
- 2-column grid for panels
- Card height adjusts automatically
- Buttons remain side-by-side

### Desktop (≥ 992px)
- 3-column grid for panels (if space allows)
- Maximum width container: 1200px
- Optimal readability

---

## Accessibility

### ARIA Labels
- Form inputs have proper labels
- Buttons have descriptive text (no icon-only buttons)
- Error messages associated with inputs via `aria-describedby`

### Keyboard Navigation
- All interactive elements focusable
- Logical tab order
- Submit button activates on Enter key

### Color Contrast
- Text-to-background ratios meet WCAG AA standards
- Error messages use both color and icons
- Success/failure indicated with symbols, not just color

### Screen Reader Support
- Semantic HTML (`<nav>`, `<main>`, `<form>`, etc.)
- Alt text for icons (via `<title>` in SVG or `aria-label`)
- Live region updates for dynamic content

---

## Browser Compatibility

- **Chrome/Edge:** Full support (latest)
- **Firefox:** Full support (latest)
- **Safari:** Full support (latest)
- **Mobile Browsers:** iOS Safari, Chrome Mobile (latest)

**No IE11 support** (Bootstrap 5 requirement)

---

## Performance Optimization

### Page Load
- Minimal JavaScript (form validation only)
- No external API calls
- Server-side rendering (fast first paint)

### Asset Loading
- Bootstrap CSS/JS from CDN (cached)
- Icons inline SVG (no external requests)
- No custom fonts (system fonts only)

### Rendering
- Progressive enhancement (works without JS)
- No layout shifts (CLS score: 0)
- Fast interaction (FID < 100ms)

---

## Testing Requirements

### Manual Testing
1. Setup page displays correctly
2. Problem count selection validates properly
3. Problem page loads with correct data
4. Answer input accepts decimal values
5. Answer validation works (correct/incorrect/invalid)
6. Progress bar updates correctly
7. Score tracking accurate throughout session
8. Results page displays all problems correctly
9. Solution steps expand/collapse properly
10. Retake button clears session and restarts
11. Session expiration handled gracefully
12. Mobile responsive design works

### Cross-Browser Testing
- Test on Chrome, Firefox, Safari
- Test on mobile (iOS/Android)
- Verify form validation works
- Check Bootstrap collapse/modal functionality

### Accessibility Testing
- Keyboard navigation works
- Screen reader compatibility
- Color contrast passes WCAG AA
- Focus indicators visible

---

## Deployment Checklist

- [ ] All views created in `Views/GradedExercise/`
- [ ] ViewModels created in `ViewModels/`
- [ ] Home page panel added
- [ ] CSS follows existing patterns (no custom styles needed)
- [ ] JavaScript minimal and validated
- [ ] Responsive design tested
- [ ] Accessibility validated
- [ ] Cross-browser tested
- [ ] Performance optimized
- [ ] Error states handled
- [ ] Success flows tested

---

## Sign-Off

**Architect:** ✅ Approved  
**Date:** 2025-01-27  
**Next Step:** Backend implementation, then frontend views
