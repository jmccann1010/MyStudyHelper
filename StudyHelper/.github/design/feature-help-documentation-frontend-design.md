# Help Documentation System - Frontend Design Document

**Feature:** Help Documentation System  
**Branch:** `feature/help-documentation`  
**Status:** Design Ready for Engineering  
**Last Updated:** 2025-01-27  

---

## Executive Summary

The Help Documentation System frontend consists of 10 Razor views providing comprehensive documentation for all StudyHelper features. The design follows Bootstrap 5 patterns, maintains consistency with the existing application design, and provides an accessible, responsive, and user-friendly experience.

**Key Design Principles:**
- **Consistency**: Match existing StudyHelper styling
- **Accessibility**: WCAG AA compliant
- **Responsive**: Mobile-first design
- **Scannable**: Easy to find information quickly
- **Actionable**: Clear instructions and steps

---

## Design System

### Color Scheme

Using existing StudyHelper color scheme:
- **Primary**: Blue (`bg-primary`, `text-primary`) - Links, headers
- **Success**: Green (`bg-success`, `text-success`) - Tips, correct answers
- **Danger**: Red (`bg-danger`, `text-danger`) - Warnings, errors
- **Info**: Teal (`bg-info`, `text-info`) - Informational callouts
- **Warning**: Orange (`bg-warning`, `text-warning`) - Cautions
- **Secondary**: Gray (`bg-secondary`, `text-secondary`) - Neutral content

### Typography

Using existing font stack from site.css:
- **Headings**: System fonts (San Francisco, Segoe UI, etc.)
- **Body**: Same system font stack
- **Code**: Monospace for UI elements and code examples

**Heading Hierarchy:**
- **H1**: Page title (40px, bold)
- **H2**: Major sections (32px, semi-bold)
- **H3**: Subsections (24px, semi-bold)
- **H4**: Minor sections (20px, medium)
- **Body**: 16px regular

### Spacing

Bootstrap spacing scale (rem-based):
- **Sections**: `mb-5` (3rem between major sections)
- **Paragraphs**: `mb-3` (1rem between paragraphs)
- **Lists**: `mb-2` (0.5rem between items)
- **Cards**: `mb-4` (1.5rem between cards)

---

## Common Components

### 1. Page Header Component

Used on all help pages:

```html
<div class="container mt-4">
	<!-- Breadcrumb Navigation -->
	<nav aria-label="breadcrumb">
		<ol class="breadcrumb">
			<li class="breadcrumb-item">
				<a asp-controller="Help" asp-action="Index">Help</a>
			</li>
			<li class="breadcrumb-item active" aria-current="page">[Feature Name]</li>
		</ol>
	</nav>

	<!-- Page Header with Icon -->
	<div class="d-flex align-items-center mb-4">
		<div class="me-3">
			[SVG Icon matching feature]
		</div>
		<div>
			<h1 class="mb-0">[Feature Name] Help</h1>
			<p class="text-muted mb-0">Learn how to use [feature]</p>
		</div>
	</div>
</div>
```

### 2. Table of Contents Component

Sticky sidebar or collapsible section:

```html
<div class="card bg-light mb-4">
	<div class="card-header">
		<h5 class="mb-0">
			<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-list-ul me-2" viewBox="0 0 16 16">
				<path fill-rule="evenodd" d="M5 11.5a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm-3 1a1 1 0 1 0 0-2 1 1 0 0 0 0 2zm0 4a1 1 0 1 0 0-2 1 1 0 0 0 0 2zm0 4a1 1 0 1 0 0-2 1 1 0 0 0 0 2z"/>
			</svg>
			Table of Contents
		</h5>
	</div>
	<div class="card-body">
		<nav class="nav flex-column">
			<a class="nav-link" href="#overview">Overview</a>
			<a class="nav-link" href="#getting-started">Getting Started</a>
			<a class="nav-link" href="#section-name">Section Name</a>
			<a class="nav-link" href="#tips">Tips & Best Practices</a>
			<a class="nav-link" href="#troubleshooting">Troubleshooting</a>
			<a class="nav-link" href="#related">Related Topics</a>
		</nav>
	</div>
</div>
```

### 3. Callout Components

**Tip Callout:**
```html
<div class="alert alert-success" role="alert">
	<h5 class="alert-heading">
		<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-lightbulb me-2" viewBox="0 0 16 16">
			<path d="M2 6a6 6 0 1 1 10.174 4.31c-.203.196-.359.4-.453.619l-.762 1.769A.5.5 0 0 1 10.5 13a.5.5 0 0 1 0 1 .5.5 0 0 1 0 1l-.224.447a1 1 0 0 1-.894.553H6.618a1 1 0 0 1-.894-.553L5.5 15a.5.5 0 0 1 0-1 .5.5 0 0 1 0-1 .5.5 0 0 1-.46-.302l-.761-1.77a1.964 1.964 0 0 0-.453-.618A5.984 5.984 0 0 1 2 6zm6-5a5 5 0 0 0-3.479 8.592c.263.254.514.564.676.941L5.83 12h4.342l.632-1.467c.162-.377.413-.687.676-.941A5 5 0 0 0 8 1z"/>
		</svg>
		Tip
	</h5>
	<p class="mb-0">[Helpful tip content]</p>
</div>
```

**Warning Callout:**
```html
<div class="alert alert-warning" role="alert">
	<h5 class="alert-heading">
		<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-exclamation-triangle me-2" viewBox="0 0 16 16">
			<path d="M7.938 2.016A.13.13 0 0 1 8.002 2a.13.13 0 0 1 .063.016.146.146 0 0 1 .054.057l6.857 11.667c.036.06.035.124.002.183a.163.163 0 0 1-.054.06.116.116 0 0 1-.066.017H1.146a.115.115 0 0 1-.066-.017.163.163 0 0 1-.054-.06.176.176 0 0 1 .002-.183L7.884 2.073a.147.147 0 0 1 .054-.057zm1.044-.45a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566z"/>
			<path d="M7.002 12a1 1 0 1 1 2 0 1 1 0 0 1-2 0zM7.1 5.995a.905.905 0 1 1 1.8 0l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995z"/>
		</svg>
		Important
	</h5>
	<p class="mb-0">[Warning content]</p>
</div>
```

**Note Callout:**
```html
<div class="alert alert-info" role="alert">
	<h5 class="alert-heading">
		<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-info-circle me-2" viewBox="0 0 16 16">
			<path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
			<path d="m8.93 6.588-2.29.287-.082.38.45.083c.294.07.352.176.288.469l-.738 3.468c-.194.897.105 1.319.808 1.319.545 0 1.178-.252 1.465-.598l.088-.416c-.2.176-.492.246-.686.246-.275 0-.375-.193-.304-.533L8.93 6.588zM9 4.5a1 1 0 1 1-2 0 1 1 0 0 1 2 0z"/>
		</svg>
		Note
	</h5>
	<p class="mb-0">[Note content]</p>
</div>
```

### 4. Step-by-Step Instructions Component

```html
<div class="card mb-4">
	<div class="card-header bg-primary text-white">
		<h3 class="mb-0">Getting Started</h3>
	</div>
	<div class="card-body">
		<ol class="list-group list-group-numbered">
			<li class="list-group-item">
				<strong>Step 1:</strong> [First action]
				<p class="mb-0 text-muted">[Additional details]</p>
			</li>
			<li class="list-group-item">
				<strong>Step 2:</strong> [Second action]
				<p class="mb-0 text-muted">[Additional details]</p>
			</li>
			<li class="list-group-item">
				<strong>Step 3:</strong> [Third action]
				<p class="mb-0 text-muted">[Additional details]</p>
			</li>
		</ol>
	</div>
</div>
```

### 5. Troubleshooting Component

```html
<div class="accordion mb-4" id="troubleshootingAccordion">
	<div class="accordion-item">
		<h2 class="accordion-header">
			<button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#problem1">
				<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-exclamation-circle me-2 text-danger" viewBox="0 0 16 16">
					<path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
					<path d="M7.002 11a1 1 0 1 1 2 0 1 1 0 0 1-2 0zM7.1 4.995a.905.905 0 1 1 1.8 0l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 4.995z"/>
				</svg>
				Problem: [Common issue description]
			</button>
		</h2>
		<div id="problem1" class="accordion-collapse collapse" data-bs-parent="#troubleshootingAccordion">
			<div class="accordion-body">
				<p><strong>Solution:</strong> [How to fix the issue]</p>
				<ul>
					<li>[Specific step 1]</li>
					<li>[Specific step 2]</li>
				</ul>
			</div>
		</div>
	</div>
	<!-- Additional accordion items for other problems -->
</div>
```

### 6. Related Topics Component

```html
<div class="card bg-light">
	<div class="card-header">
		<h3 class="mb-0">Related Topics</h3>
	</div>
	<div class="card-body">
		<div class="row">
			<div class="col-md-6 mb-3">
				<div class="card h-100">
					<div class="card-body">
						<h5 class="card-title">
							<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" class="bi bi-link-45deg me-2" viewBox="0 0 16 16">
								<path d="M4.715 6.542 3.343 7.914a3 3 0 1 0 4.243 4.243l1.828-1.829A3 3 0 0 0 8.586 5.5L8 6.086a1.002 1.002 0 0 0-.154.199 2 2 0 0 1 .861 3.337L6.88 11.45a2 2 0 1 1-2.83-2.83l.793-.792a4.018 4.018 0 0 1-.128-1.287z"/>
								<path d="M6.586 4.672A3 3 0 0 0 7.414 9.5l.775-.776a2 2 0 0 1-.896-3.346L9.12 3.55a2 2 0 1 1 2.83 2.83l-.793.792c.112.42.155.855.128 1.287l1.372-1.372a3 3 0 1 0-4.243-4.243L6.586 4.672z"/>
							</svg>
							[Related Feature]
						</h5>
						<p class="card-text">[Brief description of related feature]</p>
						<a asp-controller="Help" asp-action="[RelatedFeature]" class="btn btn-sm btn-outline-primary">Learn More →</a>
					</div>
				</div>
			</div>
			<!-- Additional related topic cards -->
		</div>
	</div>
</div>
```

---

## Page 1: Help Overview (Index.cshtml)

### Purpose
Main landing page for help system with overview of all help topics.

### Layout

```html
@{
	ViewData["Title"] = "Help Center";
}

<div class="container mt-4">
	<!-- Header -->
	<div class="text-center mb-5">
		<h1 class="display-4 mb-3">StudyHelper Help Center</h1>
		<p class="lead text-muted">
			Find answers, learn features, and get the most out of StudyHelper
		</p>
	</div>

	<!-- Quick Start Section -->
	<div class="card mb-5">
		<div class="card-header bg-primary text-white">
			<h2 class="mb-0">
				<svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" fill="currentColor" class="bi bi-rocket-takeoff me-2" viewBox="0 0 16 16">
					<path d="M9.752 6.193c.599.6 1.73.437 2.528-.362.798-.799.96-1.932.362-2.531-.599-.6-1.73-.438-2.528.361-.798.8-.96 1.933-.362 2.532Z"/>
					<path d="M15.811 3.312c-.363 1.534-1.334 3.626-3.64 6.218l-.24 2.408a2.56 2.56 0 0 1-.732 1.526L8.817 15.85a.51.51 0 0 1-.867-.434l.27-1.899c.04-.28-.013-.593-.131-.956a9.42 9.42 0 0 0-.249-.657l-.082-.202c-.815-.197-1.578-.662-2.191-1.277-.614-.615-1.079-1.379-1.275-2.195l-.203-.083a9.556 9.556 0 0 0-.655-.248c-.363-.119-.675-.172-.955-.132l-1.896.27A.51.51 0 0 1 .15 7.17l2.382-2.386c.41-.410.947-.67 1.524-.734h.006l2.4-.238C9.005 1.55 11.087.582 12.623.208c.89-.217 1.59-.232 2.08-.188.244.023.435.06.57.093.067.017.12.033.16.045.184.06.279.13.351.295l.029.073a3.475 3.475 0 0 1 .157.721c.055.485.051 1.178-.159 2.065Zm-4.828 7.475.04-.04-.107 1.081a1.536 1.536 0 0 1-.44.913l-1.298 1.3.054-.38c.072-.506-.034-.993-.172-1.418a8.548 8.548 0 0 0-.164-.45c.738-.065 1.462-.38 2.087-1.006ZM5.205 5c-.625.626-.94 1.351-1.004 2.09a8.497 8.497 0 0 0-.45-.164c-.424-.138-.91-.244-1.416-.172l-.38.054 1.3-1.3c.245-.246.566-.401.91-.44l1.08-.107-.04.039Zm9.406-3.961c-.38-.034-.967-.027-1.746.163-1.558.38-3.917 1.496-6.937 4.521-.62.62-.799 1.34-.687 2.051.107.676.483 1.362 1.048 1.928.564.565 1.25.941 1.924 1.049.71.112 1.429-.067 2.048-.688 3.079-3.083 4.192-5.444 4.556-6.987.183-.771.18-1.345.138-1.713a2.835 2.835 0 0 0-.045-.283 3.078 3.078 0 0 0-.3-.041Z"/>
					<path d="M7.009 12.139a7.632 7.632 0 0 1-1.804-1.352A7.568 7.568 0 0 1 3.794 8.86c-1.102.992-1.965 5.054-1.839 5.18.125.126 3.936-.896 5.054-1.902Z"/>
				</svg>
				Quick Start Guide
			</h2>
		</div>
		<div class="card-body">
			<p class="lead">New to StudyHelper? Follow these steps to get started:</p>
			<ol class="list-group list-group-numbered">
				<li class="list-group-item">
					<strong>Create an account</strong> - Register to save your progress
					<a asp-controller="Help" asp-action="Account" class="ms-2">Learn about accounts →</a>
				</li>
				<li class="list-group-item">
					<strong>Upload study materials</strong> - Add your own content or use defaults
					<a asp-controller="Help" asp-action="StudyMaterials" class="ms-2">Learn about study materials →</a>
				</li>
				<li class="list-group-item">
					<strong>Choose a study mode</strong> - Quiz, Exercise, or Flashcards
					<a href="#study-modes" class="ms-2">See study modes below →</a>
				</li>
				<li class="list-group-item">
					<strong>Customize your experience</strong> - Change themes and settings
					<a asp-controller="Help" asp-action="Settings" class="ms-2">Learn about settings →</a>
				</li>
			</ol>
		</div>
	</div>

	<!-- Help Topics Grid -->
	<h2 class="mb-4" id="study-modes">Study Modes & Features</h2>

	<div class="row g-4 mb-5">
		<!-- Quiz Card -->
		<div class="col-md-6 col-lg-4">
			<div class="card h-100 shadow-sm border-primary">
				<div class="card-body d-flex flex-column">
					<div class="text-center mb-3">
						<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" fill="currentColor" class="bi bi-question-circle text-primary" viewBox="0 0 16 16">
							<path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
							<path d="M5.255 5.786a.237.237 0 0 0 .241.247h.825c.138 0 .248-.113.266-.25.09-.656.54-1.134 1.342-1.134.686 0 1.314.343 1.314 1.168 0 .635-.374.927-.965 1.371-.673.489-1.206 1.06-1.168 1.987l.003.217a.25.25 0 0 0 .25.246h.811a.25.25 0 0 0 .25-.25v-.105c0-.718.273-.927 1.01-1.486.609-.463 1.244-.977 1.244-2.056 0-1.511-1.276-2.241-2.673-2.241-1.267 0-2.655.59-2.75 2.286zm1.557 5.763c0 .533.425.927 1.01.927.609 0 1.028-.394 1.028-.927 0-.552-.42-.94-1.029-.94-.584 0-1.009.388-1.009.94z"/>
						</svg>
					</div>
					<h3 class="card-title text-center">Quiz</h3>
					<p class="card-text flex-grow-1">
						Practice with multiple-choice questions. Get instant feedback and continue learning.
					</p>
					<div class="text-center mt-3">
						<a asp-controller="Help" asp-action="Quiz" class="btn btn-primary">Learn More →</a>
					</div>
				</div>
			</div>
		</div>

		<!-- Graded Quiz Card -->
		<div class="col-md-6 col-lg-4">
			<div class="card h-100 shadow-sm border-danger">
				<div class="card-body d-flex flex-column">
					<div class="text-center mb-3">
						<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" fill="currentColor" class="bi bi-file-earmark-check text-danger" viewBox="0 0 16 16">
							<path d="M10.854 7.854a.5.5 0 0 0-.708-.708L7.5 9.793 6.354 8.646a.5.5 0 1 0-.708.708l1.5 1.5a.5.5 0 0 0 .708 0l3-3z"/>
							<path d="M14 14V4.5L9.5 0H4a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2zM9.5 3A1.5 1.5 0 0 0 11 4.5h2V14a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h5.5v2z"/>
						</svg>
					</div>
					<h3 class="card-title text-center">Graded Quiz</h3>
					<p class="card-text flex-grow-1">
						Take a scored assessment. Track your performance with detailed results and review.
					</p>
					<div class="text-center mt-3">
						<a asp-controller="Help" asp-action="GradedQuiz" class="btn btn-danger">Learn More →</a>
					</div>
				</div>
			</div>
		</div>

		<!-- Exercise Card -->
		<div class="col-md-6 col-lg-4">
			<div class="card h-100 shadow-sm border-success">
				<div class="card-body d-flex flex-column">
					<div class="text-center mb-3">
						<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" fill="currentColor" class="bi bi-calculator text-success" viewBox="0 0 16 16">
							<path d="M12 1a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h8zM4 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2H4z"/>
							<path d="M4 2.5a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 .5.5v2a.5.5 0 0 1-.5.5h-7a.5.5 0 0 1-.5-.5v-2zm0 4a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5v-1zm0 3a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5v-1zm0 3a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5v-1zm3-6a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5v-1zm0 3a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5v-1zm0 3a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5v-1zm3-6a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5v-1zm0 3a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v4a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5v-4z"/>
						</svg>
					</div>
					<h3 class="card-title text-center">Exercise</h3>
					<p class="card-text flex-grow-1">
						Solve accounting equation problems. Practice calculations with instant validation.
					</p>
					<div class="text-center mt-3">
						<a asp-controller="Help" asp-action="Exercise" class="btn btn-success">Learn More →</a>
					</div>
				</div>
			</div>
		</div>

		<!-- Continue with remaining feature cards... -->
		<!-- Graded Exercises, Term Flashcards, Equation Flashcards, Study Materials, Account, Settings -->

	</div>

	<!-- Additional Resources -->
	<div class="card bg-light mb-5">
		<div class="card-header">
			<h2 class="mb-0">Additional Resources</h2>
		</div>
		<div class="card-body">
			<div class="row">
				<div class="col-md-4 mb-3">
					<h5>Study Materials</h5>
					<p>Learn how to upload and manage your custom study content.</p>
					<a asp-controller="Help" asp-action="StudyMaterials" class="btn btn-sm btn-outline-primary">Study Materials Help →</a>
				</div>
				<div class="col-md-4 mb-3">
					<h5>Account & Security</h5>
					<p>Manage your account, password, and privacy settings.</p>
					<a asp-controller="Help" asp-action="Account" class="btn btn-sm btn-outline-primary">Account Help →</a>
				</div>
				<div class="col-md-4 mb-3">
					<h5>Settings & Themes</h5>
					<p>Customize your StudyHelper experience with themes and preferences.</p>
					<a asp-controller="Help" asp-action="Settings" class="btn btn-sm btn-outline-primary">Settings Help →</a>
				</div>
			</div>
		</div>
	</div>

	<!-- Back to Top Button -->
	<div class="text-center mb-4">
		<a href="#" class="btn btn-outline-secondary">
			<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-arrow-up me-2" viewBox="0 0 16 16">
				<path fill-rule="evenodd" d="M8 15a.5.5 0 0 0 .5-.5V2.707l3.146 3.147a.5.5 0 0 0 .708-.708l-4-4a.5.5 0 0 0-.708 0l-4 4a.5.5 0 1 1 .708.708L7.5 2.707V14.5a.5.5 0 0 0 .5.5z"/>
			</svg>
			Back to Top
		</a>
	</div>
</div>

<style>
	.card {
		transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
	}

	.card:hover {
		transform: translateY(-5px);
		box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
	}
</style>
```

### Key Features:
- Hero header with welcoming message
- Quick Start Guide for new users
- Feature cards with icons matching home page
- Hover effects on cards
- Additional resources section
- Back to Top button for long page

---

## Navigation Menu Integration (_Layout.cshtml)

### Location
Add after main navigation items, before user account menu

### Implementation

```html
<!-- Existing navigation items -->
<li class="nav-item">
	<a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Index">Home</a>
</li>
<!-- Other nav items... -->

<!-- NEW: Help Dropdown Menu -->
<li class="nav-item dropdown">
	<a class="nav-link dropdown-toggle text-dark" href="#" id="helpDropdown" role="button" data-bs-toggle="dropdown" aria-expanded="false">
		<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-question-circle me-1" viewBox="0 0 16 16">
			<path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
			<path d="M5.255 5.786a.237.237 0 0 0 .241.247h.825c.138 0 .248-.113.266-.25.09-.656.54-1.134 1.342-1.134.686 0 1.314.343 1.314 1.168 0 .635-.374.927-.965 1.371-.673.489-1.206 1.06-1.168 1.987l.003.217a.25.25 0 0 0 .25.246h.811a.25.25 0 0 0 .25-.25v-.105c0-.718.273-.927 1.01-1.486.609-.463 1.244-.977 1.244-2.056 0-1.511-1.276-2.241-2.673-2.241-1.267 0-2.655.59-2.75 2.286zm1.557 5.763c0 .533.425.927 1.01.927.609 0 1.028-.394 1.028-.927 0-.552-.42-.94-1.029-.94-.584 0-1.009.388-1.009.94z"/>
		</svg>
		Help
	</a>
	<ul class="dropdown-menu" aria-labelledby="helpDropdown">
		<li>
			<a class="dropdown-item" asp-controller="Help" asp-action="Index">
				<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-house-door me-2" viewBox="0 0 16 16">
					<path d="M8.354 1.146a.5.5 0 0 0-.708 0l-6 6A.5.5 0 0 0 1.5 7.5v7a.5.5 0 0 0 .5.5h4.5a.5.5 0 0 0 .5-.5v-4h2v4a.5.5 0 0 0 .5.5H14a.5.5 0 0 0 .5-.5v-7a.5.5 0 0 0-.146-.354L13 5.793V2.5a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5v1.293L8.354 1.146zM2.5 14V7.707l5.5-5.5 5.5 5.5V14H10v-4a.5.5 0 0 0-.5-.5h-3a.5.5 0 0 0-.5.5v4H2.5z"/>
				</svg>
				Help Overview
			</a>
		</li>
		<li><hr class="dropdown-divider"></li>
		<li>
			<h6 class="dropdown-header">Study Features</h6>
		</li>
		<li>
			<a class="dropdown-item" asp-controller="Help" asp-action="Quiz">Quiz Help</a>
		</li>
		<li>
			<a class="dropdown-item" asp-controller="Help" asp-action="GradedQuiz">Graded Quiz Help</a>
		</li>
		<li>
			<a class="dropdown-item" asp-controller="Help" asp-action="Exercise">Exercise Help</a>
		</li>
		<li>
			<a class="dropdown-item" asp-controller="Help" asp-action="GradedExercises">Graded Exercises Help</a>
		</li>
		<li><hr class="dropdown-divider"></li>
		<li>
			<h6 class="dropdown-header">Flashcards</h6>
		</li>
		<li>
			<a class="dropdown-item" asp-controller="Help" asp-action="TermFlashcards">Term Flashcards Help</a>
		</li>
		<li>
			<a class="dropdown-item" asp-controller="Help" asp-action="EquationFlashcards">Equation Flashcards Help</a>
		</li>
		<li><hr class="dropdown-divider"></li>
		<li>
			<h6 class="dropdown-header">Management</h6>
		</li>
		<li>
			<a class="dropdown-item" asp-controller="Help" asp-action="StudyMaterials">Study Materials Help</a>
		</li>
		<li>
			<a class="dropdown-item" asp-controller="Help" asp-action="Account">Account Help</a>
		</li>
		<li>
			<a class="dropdown-item" asp-controller="Help" asp-action="Settings">Settings Help</a>
		</li>
	</ul>
</li>
```

### Menu Structure:
- **Help Overview** at top with house icon
- **Divider**
- **Study Features** header with Quiz, Graded Quiz, Exercise, Graded Exercises
- **Flashcards** header with both flashcard types
- **Management** header with Study Materials, Account, Settings

---

## Feature Help Pages - Content Template

Due to length constraints, I'll provide the template structure. Each feature help page follows this pattern:

### Standard Structure for Each Help Page:

1. **Breadcrumb Navigation**
2. **Page Header with Icon**
3. **Brief Overview (2-3 sentences)**
4. **Table of Contents Card**
5. **Overview Section** (#overview)
6. **Getting Started Section** (#getting-started) - Numbered steps
7. **Feature-Specific Sections** (varies by feature)
8. **Tips & Best Practices Section** (#tips)
9. **Troubleshooting Section** (#troubleshooting) - Accordion
10. **Related Topics Section** (#related)
11. **Back to Help Overview Link**

---

## Responsive Design

### Breakpoints

**Mobile (<768px):**
- Single column layout
- Stacked cards
- Collapsible TOC
- Full-width buttons
- Hamburger menu for help dropdown

**Tablet (768px-991px):**
- 2-column card grid
- Sticky TOC in sidebar (optional)
- Side-by-side layout for some sections

**Desktop (≥992px):**
- 3-column card grid on overview page
- Sticky TOC sidebar on help pages
- Full-width content area
- Hover effects enabled

---

## Accessibility Features

### WCAG AA Compliance

1. **Keyboard Navigation**
   - All links focusable via Tab
   - Dropdown menus keyboard-accessible
   - Skip to content link
   - Logical tab order

2. **Screen Readers**
   - Semantic HTML (`<nav>`, `<main>`, `<article>`, `<aside>`)
   - Proper heading hierarchy (H1 → H2 → H3)
   - ARIA labels on icons
   - Alt text for visual elements

3. **Color Contrast**
   - Text: 4.5:1 minimum
   - Large text: 3:1 minimum
   - Links distinguishable from text

4. **Responsive Text**
   - Readable at 200% zoom
   - No horizontal scrolling at 320px
   - rem-based sizing

5. **Forms (if any)**
   - Labels properly associated
   - Error messages descriptive
   - Required fields indicated

---

## Testing Checklist

### Functional Testing
- [ ] All help pages load (200 OK)
- [ ] Navigation menu dropdown works
- [ ] Breadcrumb links functional
- [ ] TOC anchor links jump correctly
- [ ] Cross-reference links valid
- [ ] Back to Top button scrolls to top
- [ ] Accordion collapse/expand works

### Visual Testing
- [ ] Consistent styling across pages
- [ ] Icons display correctly
- [ ] Cards aligned properly
- [ ] Spacing consistent
- [ ] No layout shifts
- [ ] Print layout acceptable

### Responsive Testing
- [ ] Mobile (375px): Single column, readable
- [ ] Tablet (768px): Optimized layout
- [ ] Desktop (1920px): Proper spacing
- [ ] Zoom 200%: Still readable

### Browser Testing
- [ ] Chrome (Windows/Mac)
- [ ] Firefox (Windows/Mac)
- [ ] Safari (Mac/iOS)
- [ ] Edge (Windows)
- [ ] Mobile Chrome (Android)
- [ ] Mobile Safari (iOS)

### Accessibility Testing
- [ ] WAVE: No errors
- [ ] Keyboard navigation complete
- [ ] Screen reader (NVDA/JAWS) compatible
- [ ] Color contrast passing
- [ ] Focus indicators visible

---

## File Structure

### Frontend Files

```
Views/
└── Help/
	├── Index.cshtml                  (~400 lines)
	├── Quiz.cshtml                   (~500 lines)
	├── GradedQuiz.cshtml             (~500 lines)
	├── Exercise.cshtml               (~500 lines)
	├── GradedExercises.cshtml        (~500 lines)
	├── TermFlashcards.cshtml         (~400 lines)
	├── EquationFlashcards.cshtml     (~400 lines)
	├── StudyMaterials.cshtml         (~500 lines)
	├── Account.cshtml                (~400 lines)
	└── Settings.cshtml               (~300 lines)

Views/Shared/
└── _Layout.cshtml                    (Modified - add Help menu)

wwwroot/css/
└── help.css                          (Optional - ~200 lines)
```

**Total New Files:** 10-11  
**Total New/Modified Lines:** ~4,500 lines  
**Complexity:** Medium (content-heavy, design simple)

---

## Deployment Checklist

- [ ] All 10 help view files created
- [ ] _Layout.cshtml updated with Help menu
- [ ] All internal links tested
- [ ] All cross-references verified
- [ ] Content reviewed for accuracy
- [ ] Spelling and grammar checked
- [ ] Accessibility validated
- [ ] Responsive design verified
- [ ] Browser compatibility tested
- [ ] Build succeeds with no errors
- [ ] No console warnings
- [ ] Code review completed

---

## Sign-Off

**Architect:** ✅ Approved  
**Date:** 2025-01-27  
**Next Step:** Implementation
