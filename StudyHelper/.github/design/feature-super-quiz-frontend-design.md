# Super Quiz Feature - Frontend Design

## Overview
This document specifies the frontend implementation for Super Quiz, including controllers, views, view models, routing, and UI/UX patterns.

## Controller Design

### SuperQuizController
**Location:** `Controllers/SuperQuizController.cs`

**Responsibilities:**
- HTTP request handling for Super Quiz workflow
- Session ID management via query strings
- View model construction
- Error handling and user feedback

**Attributes:**
```csharp
[Authorize]  // All actions require authentication
```

### Controller Actions

#### GET /SuperQuiz/Start
**Purpose:** Display session start page with question count preview.

```csharp
[HttpGet]
public async Task<IActionResult> Start()
{
	try
	{
		var username = User.Identity?.Name;
		if (string.IsNullOrEmpty(username))
		{
			return RedirectToAction("Login", "Account");
		}

		// Preview question count without starting session
		var sections = await _markdownParserService.ParseMarkdownFilesAsync(username);

		if (sections.Count < 4)
		{
			ViewBag.ErrorMessage = $"At least 4 terms required for Super Quiz. You currently have {sections.Count} term(s). Please add more study materials.";
			return View("InsufficientContent");
		}

		var viewModel = new SuperQuizStartViewModel
		{
			TotalQuestions = sections.Count,
			EstimatedTimeMinutes = sections.Count * 0.5 // 30 seconds per question
		};

		return View(viewModel);
	}
	catch (FileNotFoundException)
	{
		ViewBag.ErrorMessage = "No study materials found. Please upload study materials first.";
		return View("NoStudyMaterials");
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Error loading Super Quiz start page");
		return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
	}
}
```

#### POST /SuperQuiz/Start
**Purpose:** Create session and redirect to first question.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Start([FromForm] bool confirmed)
{
	try
	{
		var username = User.Identity?.Name;
		if (string.IsNullOrEmpty(username))
		{
			return RedirectToAction("Login", "Account");
		}

		var sessionId = await _superQuizService.StartSuperQuizAsync(username);

		_logger.LogInformation("Super Quiz session {SessionId} started for user {Username}", 
			sessionId, username);

		return RedirectToAction(nameof(Question), new { sessionId });
	}
	catch (InvalidOperationException ex)
	{
		_logger.LogWarning(ex, "Failed to start Super Quiz for user {Username}", User.Identity?.Name);
		TempData["ErrorMessage"] = ex.Message;
		return RedirectToAction(nameof(Start));
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Error starting Super Quiz session");
		return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
	}
}
```

#### GET /SuperQuiz/Question?sessionId={guid}
**Purpose:** Display current question with answer options.

```csharp
[HttpGet]
public async Task<IActionResult> Question([FromQuery] string sessionId)
{
	try
	{
		if (string.IsNullOrEmpty(sessionId))
		{
			TempData["ErrorMessage"] = "Invalid session. Please start a new Super Quiz.";
			return RedirectToAction(nameof(Start));
		}

		var username = User.Identity?.Name;
		if (!await _superQuizService.ValidateSessionOwnershipAsync(sessionId, username))
		{
			_logger.LogWarning("User {Username} attempted to access session {SessionId} they don't own", 
				username, sessionId);
			return Forbid();
		}

		var question = await _superQuizService.GetCurrentQuestionAsync(sessionId);
		if (question == null)
		{
			TempData["ErrorMessage"] = "Your session expired. Please start a new Super Quiz.";
			return RedirectToAction(nameof(Start));
		}

		var progress = await _superQuizService.GetProgressAsync(sessionId);

		var viewModel = new SuperQuizQuestionViewModel
		{
			SessionId = sessionId,
			QuestionText = question.QuestionText,
			AnswerOptions = question.AnswerOptions,
			Module = question.Module,
			Topic = question.Topic,
			Direction = question.Direction,
			DirectionLabel = question.Direction == QuestionDirection.TermToDefinition 
				? "Term → Definition" 
				: "Definition → Term",
			Progress = progress
		};

		return View(viewModel);
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Error loading question for session {SessionId}", sessionId);
		return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
	}
}
```

#### POST /SuperQuiz/SubmitAnswer
**Purpose:** Validate answer and advance to next state.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SubmitAnswer([FromForm] string sessionId, [FromForm] int selectedAnswerIndex)
{
	try
	{
		if (selectedAnswerIndex < 0 || selectedAnswerIndex > 3)
		{
			_logger.LogWarning("Invalid answer index submitted: {Index}", selectedAnswerIndex);
			return BadRequest("Invalid answer selection.");
		}

		var username = User.Identity?.Name;
		if (!await _superQuizService.ValidateSessionOwnershipAsync(sessionId, username))
		{
			return Forbid();
		}

		var result = await _superQuizService.SubmitAnswerAsync(sessionId, selectedAnswerIndex);

		var viewModel = new SuperQuizResultViewModel
		{
			SessionId = sessionId,
			IsCorrect = result.IsCorrect,
			FeedbackMessage = result.IsCorrect ? "Correct!" : "Incorrect",
			CorrectAnswerText = result.CorrectAnswerText,
			UserAnswerText = result.UserAnswerText,
			Explanation = result.Explanation,
			Progress = result.Progress,
			NextAction = result.NextAction
		};

		return View("Result", viewModel);
	}
	catch (InvalidOperationException ex)
	{
		_logger.LogWarning(ex, "Session not found: {SessionId}", sessionId);
		TempData["ErrorMessage"] = "Your session expired. Please start a new Super Quiz.";
		return RedirectToAction(nameof(Start));
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Error submitting answer for session {SessionId}", sessionId);
		return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
	}
}
```

#### GET /SuperQuiz/RoundSummary?sessionId={guid}
**Purpose:** Display round completion summary and prompt for next round.

```csharp
[HttpGet]
public async Task<IActionResult> RoundSummary([FromQuery] string sessionId)
{
	try
	{
		var username = User.Identity?.Name;
		if (!await _superQuizService.ValidateSessionOwnershipAsync(sessionId, username))
		{
			return Forbid();
		}

		var summary = await _superQuizService.GetLastRoundSummaryAsync(sessionId);
		if (summary == null)
		{
			TempData["ErrorMessage"] = "Session not found.";
			return RedirectToAction(nameof(Start));
		}

		var progress = await _superQuizService.GetProgressAsync(sessionId);

		var viewModel = new SuperQuizRoundSummaryViewModel
		{
			SessionId = sessionId,
			RoundSummary = summary,
			Progress = progress
		};

		return View(viewModel);
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Error loading round summary for session {SessionId}", sessionId);
		return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
	}
}
```

#### POST /SuperQuiz/ContinueNextRound
**Purpose:** Start next round after round summary.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ContinueNextRound([FromForm] string sessionId)
{
	try
	{
		var username = User.Identity?.Name;
		if (!await _superQuizService.ValidateSessionOwnershipAsync(sessionId, username))
		{
			return Forbid();
		}

		await _superQuizService.StartNextRoundAsync(sessionId);

		return RedirectToAction(nameof(Question), new { sessionId });
	}
	catch (InvalidOperationException ex)
	{
		_logger.LogWarning(ex, "Cannot start next round for session {SessionId}", sessionId);
		TempData["ErrorMessage"] = "Unable to start next round.";
		return RedirectToAction(nameof(Start));
	}
}
```

#### GET /SuperQuiz/Complete?sessionId={guid}
**Purpose:** Display completion summary with all statistics.

```csharp
[HttpGet]
public async Task<IActionResult> Complete([FromQuery] string sessionId)
{
	try
	{
		var username = User.Identity?.Name;
		if (!await _superQuizService.ValidateSessionOwnershipAsync(sessionId, username))
		{
			return Forbid();
		}

		var summary = await _superQuizService.GetCompletionSummaryAsync(sessionId);
		if (summary == null)
		{
			TempData["ErrorMessage"] = "Session not found or not completed.";
			return RedirectToAction(nameof(Start));
		}

		var viewModel = new SuperQuizCompleteViewModel
		{
			Summary = summary
		};

		return View(viewModel);
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Error loading completion summary for session {SessionId}", sessionId);
		return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
	}
}
```

## View Models

### SuperQuizStartViewModel
**Location:** `ViewModels/SuperQuizStartViewModel.cs`

```csharp
namespace StudyHelper.ViewModels;

public class SuperQuizStartViewModel
{
	public int TotalQuestions { get; set; }
	public double EstimatedTimeMinutes { get; set; }
	public string EstimatedTimeFormatted => 
		EstimatedTimeMinutes < 60 
			? $"{EstimatedTimeMinutes:F0} minutes" 
			: $"{EstimatedTimeMinutes / 60:F1} hours";
}
```

### SuperQuizQuestionViewModel
**Location:** `ViewModels/SuperQuizQuestionViewModel.cs`

```csharp
namespace StudyHelper.ViewModels;

public class SuperQuizQuestionViewModel
{
	public string SessionId { get; set; } = string.Empty;
	public string QuestionText { get; set; } = string.Empty;
	public List<string> AnswerOptions { get; set; } = new();
	public string Module { get; set; } = string.Empty;
	public string Topic { get; set; } = string.Empty;
	public QuestionDirection Direction { get; set; }
	public string DirectionLabel { get; set; } = string.Empty;
	public SuperQuizProgress? Progress { get; set; }
}
```

### SuperQuizResultViewModel
**Location:** `ViewModels/SuperQuizResultViewModel.cs`

```csharp
namespace StudyHelper.ViewModels;

public class SuperQuizResultViewModel
{
	public string SessionId { get; set; } = string.Empty;
	public bool IsCorrect { get; set; }
	public string FeedbackMessage { get; set; } = string.Empty;
	public string CorrectAnswerText { get; set; } = string.Empty;
	public string UserAnswerText { get; set; } = string.Empty;
	public string Explanation { get; set; } = string.Empty;
	public SuperQuizProgress Progress { get; set; } = new();
	public SuperQuizNextAction NextAction { get; set; }

	public string NextButtonText => NextAction switch
	{
		SuperQuizNextAction.NextQuestion => "Next Question",
		SuperQuizNextAction.RoundComplete => "View Round Summary",
		SuperQuizNextAction.QuizComplete => "View Results",
		_ => "Continue"
	};

	public string NextActionUrl => NextAction switch
	{
		SuperQuizNextAction.NextQuestion => $"/SuperQuiz/Question?sessionId={SessionId}",
		SuperQuizNextAction.RoundComplete => $"/SuperQuiz/RoundSummary?sessionId={SessionId}",
		SuperQuizNextAction.QuizComplete => $"/SuperQuiz/Complete?sessionId={SessionId}",
		_ => "/"
	};
}
```

### SuperQuizRoundSummaryViewModel
**Location:** `ViewModels/SuperQuizRoundSummaryViewModel.cs`

```csharp
namespace StudyHelper.ViewModels;

public class SuperQuizRoundSummaryViewModel
{
	public string SessionId { get; set; } = string.Empty;
	public RoundSummary RoundSummary { get; set; } = new();
	public SuperQuizProgress? Progress { get; set; }
}
```

### SuperQuizCompleteViewModel
**Location:** `ViewModels/SuperQuizCompleteViewModel.cs`

```csharp
namespace StudyHelper.ViewModels;

public class SuperQuizCompleteViewModel
{
	public SuperQuizCompletionSummary Summary { get; set; } = new();
}
```

## Views

### Views/SuperQuiz/Start.cshtml
**Purpose:** Session start page with information and confirmation.

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

					<div class="alert alert-info">
						<h5><i class="bi bi-info-circle"></i> How Super Quiz Works</h5>
						<ul>
							<li>Answer questions from <strong>all</strong> your study materials</li>
							<li>Questions you miss will be asked again in the next round</li>
							<li>Continue until you've answered <strong>every question correctly</strong></li>
							<li>Track your progress with round-by-round statistics</li>
						</ul>
					</div>

					<div class="row text-center my-4">
						<div class="col-md-6">
							<div class="card bg-light">
								<div class="card-body">
									<h3 class="text-primary">@Model.TotalQuestions</h3>
									<p class="mb-0">Total Questions</p>
								</div>
							</div>
						</div>
						<div class="col-md-6">
							<div class="card bg-light">
								<div class="card-body">
									<h3 class="text-primary">@Model.EstimatedTimeFormatted</h3>
									<p class="mb-0">Estimated Time</p>
								</div>
							</div>
						</div>
					</div>

					<form method="post" asp-action="Start">
						@Html.AntiForgeryToken()
						<input type="hidden" name="confirmed" value="true" />
						<div class="d-grid gap-2">
							<button type="submit" class="btn btn-primary btn-lg">
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
```

### Views/SuperQuiz/Question.cshtml
**Purpose:** Display question with progress indicator.

```razor
@model SuperQuizQuestionViewModel
@{
	ViewData["Title"] = "Super Quiz - Question";
}

<!-- Reuse quiz.css for consistent styling -->
<link rel="stylesheet" href="~/css/quiz.css" />

<div class="container mt-4">
	<!-- Progress Bar -->
	@if (Model.Progress != null)
	{
		<div class="card mb-3 shadow-sm">
			<div class="card-body">
				<div class="d-flex justify-content-between mb-2">
					<span><strong>Round @Model.Progress.CurrentRound</strong></span>
					<span>@Model.Progress.Mastered / @Model.Progress.TotalQuestions Mastered</span>
				</div>
				<div class="progress" style="height: 25px;">
					<div class="progress-bar bg-success" role="progressbar" 
						 style="width: @Model.Progress.OverallProgress%"
						 aria-valuenow="@Model.Progress.OverallProgress" aria-valuemin="0" aria-valuemax="100">
						@Model.Progress.OverallProgress.ToString("F1")%
					</div>
				</div>
				<div class="text-muted small mt-1">
					@Model.Progress.QuestionsLeftThisRound questions left this round
				</div>
			</div>
		</div>
	}

	<!-- Question Card (reuse existing quiz styling) -->
	<div class="card quiz-card shadow-lg">
		<div class="card-header quiz-header">
			<div class="d-flex justify-content-between align-items-center">
				<h4 class="mb-0">Super Quiz</h4>
				<span class="badge bg-light text-dark">@Model.DirectionLabel</span>
			</div>
		</div>
		<div class="card-body">
			<div class="quiz-question-text mb-4">
				@Html.Raw(Model.QuestionText)
			</div>

			<form method="post" asp-action="SubmitAnswer">
				@Html.AntiForgeryToken()
				<input type="hidden" name="sessionId" value="@Model.SessionId" />

				<div class="quiz-answer-options">
					@for (int i = 0; i < Model.AnswerOptions.Count; i++)
					{
						var letter = ((char)('A' + i)).ToString();
						<label class="quiz-answer-option">
							<input type="radio" name="selectedAnswerIndex" value="@i" required />
							<span class="option-letter">@letter</span>
							<span class="option-text">@Html.Raw(Model.AnswerOptions[i])</span>
						</label>
					}
				</div>

				<div class="d-grid mt-4">
					<button type="submit" class="btn btn-primary btn-lg">
						Submit Answer
					</button>
				</div>
			</form>
		</div>
		<div class="card-footer text-muted">
			<small>@Model.Module - @Model.Topic</small>
		</div>
	</div>
</div>
```

### Views/SuperQuiz/Result.cshtml
**Purpose:** Show answer feedback and next action button.

```razor
@model SuperQuizResultViewModel
@{
	ViewData["Title"] = "Super Quiz - Result";
}

<link rel="stylesheet" href="~/css/quiz.css" />

<div class="container mt-4">
	<!-- Progress Bar -->
	@if (Model.Progress != null)
	{
		<div class="card mb-3 shadow-sm">
			<div class="card-body">
				<div class="d-flex justify-content-between mb-2">
					<span><strong>Round @Model.Progress.CurrentRound</strong></span>
					<span>@Model.Progress.Mastered / @Model.Progress.TotalQuestions Mastered</span>
				</div>
				<div class="progress" style="height: 25px;">
					<div class="progress-bar bg-success" role="progressbar" 
						 style="width: @Model.Progress.OverallProgress%">
						@Model.Progress.OverallProgress.ToString("F1")%
					</div>
				</div>
			</div>
		</div>
	}

	<!-- Result Card -->
	<div class="card shadow-lg">
		<div class="card-header @(Model.IsCorrect ? "bg-success" : "bg-danger") text-white">
			<h3 class="mb-0">
				@if (Model.IsCorrect)
				{
					<i class="bi bi-check-circle-fill"></i> @Model.FeedbackMessage
				}
				else
				{
					<i class="bi bi-x-circle-fill"></i> @Model.FeedbackMessage
				}
			</h3>
		</div>
		<div class="card-body">
			<div class="mb-3">
				<strong>Your Answer:</strong>
				<div class="p-3 bg-light rounded">@Html.Raw(Model.UserAnswerText)</div>
			</div>

			@if (!Model.IsCorrect)
			{
				<div class="mb-3">
					<strong>Correct Answer:</strong>
					<div class="p-3 bg-success text-white rounded">@Html.Raw(Model.CorrectAnswerText)</div>
				</div>
			}

			@if (!string.IsNullOrEmpty(Model.Explanation))
			{
				<div class="alert alert-info">
					<strong>Explanation:</strong>
					<div>@Html.Raw(Model.Explanation)</div>
				</div>
			}

			<div class="d-grid mt-4">
				<a href="@Model.NextActionUrl" class="btn btn-primary btn-lg">
					@Model.NextButtonText <i class="bi bi-arrow-right"></i>
				</a>
			</div>
		</div>
	</div>
</div>
```

### Views/SuperQuiz/RoundSummary.cshtml
**Purpose:** Show round statistics and prompt to continue.

```razor
@model SuperQuizRoundSummaryViewModel
@{
	ViewData["Title"] = "Round Complete";
}

<div class="container mt-4">
	<div class="row justify-content-center">
		<div class="col-lg-8">
			<div class="card shadow-lg">
				<div class="card-header bg-primary text-white">
					<h2 class="mb-0">Round @Model.RoundSummary.RoundNumber Complete!</h2>
				</div>
				<div class="card-body">
					<div class="row text-center mb-4">
						<div class="col-md-4">
							<div class="card bg-light">
								<div class="card-body">
									<h3 class="text-success">@Model.RoundSummary.CorrectAnswers</h3>
									<p class="mb-0">Correct</p>
								</div>
							</div>
						</div>
						<div class="col-md-4">
							<div class="card bg-light">
								<div class="card-body">
									<h3 class="text-danger">@Model.RoundSummary.IncorrectAnswers</h3>
									<p class="mb-0">Missed</p>
								</div>
							</div>
						</div>
						<div class="col-md-4">
							<div class="card bg-light">
								<div class="card-body">
									<h3 class="text-primary">@Model.RoundSummary.AccuracyPercent.ToString("F1")%</h3>
									<p class="mb-0">Accuracy</p>
								</div>
							</div>
						</div>
					</div>

					@if (Model.Progress != null && Model.Progress.Remaining > 0)
					{
						<div class="alert alert-warning">
							<h5><i class="bi bi-arrow-repeat"></i> Let's try those again!</h5>
							<p class="mb-0">
								You have <strong>@Model.Progress.Remaining question(s)</strong> remaining to master.
								Don't worry—practice makes perfect!
							</p>
						</div>

						<form method="post" asp-action="ContinueNextRound">
							@Html.AntiForgeryToken()
							<input type="hidden" name="sessionId" value="@Model.SessionId" />
							<div class="d-grid">
								<button type="submit" class="btn btn-primary btn-lg">
									<i class="bi bi-arrow-right-circle"></i> Continue to Round @(Model.RoundSummary.RoundNumber + 1)
								</button>
							</div>
						</form>
					}
				</div>
			</div>
		</div>
	</div>
</div>
```

### Views/SuperQuiz/Complete.cshtml
**Purpose:** Final completion summary with all round statistics.

```razor
@model SuperQuizCompleteViewModel
@{
	ViewData["Title"] = "Super Quiz Complete!";
}

<div class="container mt-4">
	<div class="row justify-content-center">
		<div class="col-lg-10">
			<div class="card shadow-lg">
				<div class="card-header bg-success text-white text-center">
					<h1 class="mb-0">
						<i class="bi bi-trophy-fill"></i> Congratulations!
					</h1>
					<p class="lead mb-0">You've mastered all @Model.Summary.TotalQuestions questions!</p>
				</div>
				<div class="card-body">
					<!-- Overall Stats -->
					<div class="row text-center mb-4">
						<div class="col-md-3">
							<div class="card bg-light">
								<div class="card-body">
									<h3 class="text-primary">@Model.Summary.TotalQuestions</h3>
									<p class="mb-0 small">Questions Mastered</p>
								</div>
							</div>
						</div>
						<div class="col-md-3">
							<div class="card bg-light">
								<div class="card-body">
									<h3 class="text-primary">@Model.Summary.TotalRounds</h3>
									<p class="mb-0 small">Rounds Completed</p>
								</div>
							</div>
						</div>
						<div class="col-md-3">
							<div class="card bg-light">
								<div class="card-body">
									<h3 class="text-primary">@Model.Summary.TotalTime.ToString(@"mm\:ss")</h3>
									<p class="mb-0 small">Total Time</p>
								</div>
							</div>
						</div>
						<div class="col-md-3">
							<div class="card bg-light">
								<div class="card-body">
									<h3 class="text-primary">@Model.Summary.OverallAccuracy.ToString("F1")%</h3>
									<p class="mb-0 small">Overall Accuracy</p>
								</div>
							</div>
						</div>
					</div>

					<!-- Round History -->
					<h4 class="mb-3"><i class="bi bi-graph-up"></i> Round-by-Round Progress</h4>
					<div class="table-responsive">
						<table class="table table-striped">
							<thead>
								<tr>
									<th>Round</th>
									<th>Questions</th>
									<th>Correct</th>
									<th>Missed</th>
									<th>Accuracy</th>
								</tr>
							</thead>
							<tbody>
								@foreach (var round in Model.Summary.RoundHistory)
								{
									<tr>
										<td>Round @round.RoundNumber</td>
										<td>@round.TotalQuestions</td>
										<td class="text-success">@round.CorrectAnswers</td>
										<td class="text-danger">@round.IncorrectAnswers</td>
										<td>@round.AccuracyPercent.ToString("F1")%</td>
									</tr>
								}
							</tbody>
						</table>
					</div>

					<!-- Actions -->
					<div class="d-grid gap-2 mt-4">
						<a asp-action="Start" class="btn btn-primary btn-lg">
							<i class="bi bi-arrow-repeat"></i> Start New Super Quiz
						</a>
						<a asp-controller="Home" asp-action="Index" class="btn btn-outline-secondary btn-lg">
							<i class="bi bi-house-fill"></i> Return to Home
						</a>
					</div>
				</div>
			</div>
		</div>
	</div>
</div>
```

### Error Views

#### Views/SuperQuiz/NoStudyMaterials.cshtml
```razor
@{
	ViewData["Title"] = "No Study Materials";
}

<div class="container mt-4">
	<div class="alert alert-warning">
		<h4><i class="bi bi-exclamation-triangle"></i> No Study Materials Found</h4>
		<p>@ViewBag.ErrorMessage</p>
		<a asp-controller="StudyMaterials" asp-action="Manage" class="btn btn-primary">
			Upload Study Materials
		</a>
	</div>
</div>
```

#### Views/SuperQuiz/InsufficientContent.cshtml
```razor
@{
	ViewData["Title"] = "Insufficient Content";
}

<div class="container mt-4">
	<div class="alert alert-warning">
		<h4><i class="bi bi-exclamation-triangle"></i> More Content Needed</h4>
		<p>@ViewBag.ErrorMessage</p>
		<a asp-controller="StudyMaterials" asp-action="Manage" class="btn btn-primary">
			Add More Terms
		</a>
	</div>
</div>
```

## Home Page Integration

### Views/Home/Index.cshtml Addition
Add Super Quiz card to the feature grid:

```razor
<!-- Add after regular Quiz card -->
<div class="col-md-6 col-lg-4 mb-4">
	<div class="card h-100 shadow hover-lift">
		<div class="card-body text-center">
			<div class="feature-icon bg-gradient-warning mb-3">
				<i class="bi bi-lightning-charge-fill"></i>
			</div>
			<h3 class="card-title">Super Quiz</h3>
			<p class="card-text">
				Master every term through multi-round practice until you achieve 100% accuracy.
			</p>
		</div>
		<div class="card-footer bg-transparent border-0">
			<a asp-controller="SuperQuiz" asp-action="Start" class="btn btn-warning btn-block">
				Start Super Quiz <i class="bi bi-arrow-right"></i>
			</a>
		</div>
	</div>
</div>
```

## CSS Additions

### wwwroot/css/super-quiz.css (optional custom styling)
```css
/* Reuse existing quiz.css for most styling */

/* Progress card specific styling */
.super-quiz-progress {
	position: sticky;
	top: 20px;
	z-index: 100;
}

/* Round transition animations */
.round-complete-animation {
	animation: fadeInUp 0.5s ease-in-out;
}

@keyframes fadeInUp {
	from {
		opacity: 0;
		transform: translateY(20px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

/* Completion confetti effect (optional enhancement) */
.completion-celebration {
	position: relative;
	overflow: hidden;
}
```

## Routing

All routes follow RESTful pattern:
- `GET /SuperQuiz/Start` - Start page
- `POST /SuperQuiz/Start` - Create session
- `GET /SuperQuiz/Question?sessionId={guid}` - Question display
- `POST /SuperQuiz/SubmitAnswer` - Answer submission
- `GET /SuperQuiz/RoundSummary?sessionId={guid}` - Round complete
- `POST /SuperQuiz/ContinueNextRound` - Start next round
- `GET /SuperQuiz/Complete?sessionId={guid}` - Final summary

## User Experience Flow

1. **Home** → Click "Super Quiz" card
2. **Start Page** → View question count and estimated time → Click "Start"
3. **Question Loop**:
   - View progress bar (mastered / total)
   - Answer question
   - See result (correct/incorrect)
   - Click "Next Question" or "View Round Summary"
4. **Round Summary** (if missed questions):
   - View round statistics
   - Click "Continue to Round N"
   - Return to Question Loop
5. **Completion**:
   - View all statistics and round history
   - Options: "Start New Super Quiz" or "Return to Home"

## Accessibility

- All forms include CSRF tokens
- Radio buttons properly labeled for screen readers
- Progress bars include aria attributes
- Semantic HTML structure
- Keyboard navigation support
- Color contrast meets WCAG AA standards

## Testing Requirements

### Controller Tests
- Session creation and redirect
- Question retrieval
- Answer submission (correct/incorrect)
- Round transitions
- Completion flow
- Session ownership validation
- Error handling (expired session, invalid ID)

### View Tests
- Model binding
- Form submission
- Conditional rendering (progress, round summary)
- Link generation with session ID

### Integration Tests
- Full user flow from start to completion
- Multi-round flow
- UI consistency with existing quiz patterns

## Future Enhancements

- Real-time progress updates with SignalR
- Confetti/celebration animation on completion
- Export results to PDF
- Social sharing of completion stats
- Leaderboard for fastest completion times
