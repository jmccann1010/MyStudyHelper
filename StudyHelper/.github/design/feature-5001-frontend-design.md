# Frontend Design: User Custom Study Materials Upload (Feature #5001)

**Feature**: User Custom Study Materials Upload  
**Azure DevOps**: https://dev.azure.com/SchneiderDowns/Jeff/_workitems/edit/5001  
**Date**: 2026-05-27  
**Architect**: Solutions Architect  
**Target**: ASP.NET Core MVC Razor Views, Bootstrap 5, .NET 10

---

## 1. Executive Summary

This design provides a user-friendly interface for uploading, managing, and deleting custom study materials (TermsAndDefinitions.md and Equations.md). The UI will integrate seamlessly with the existing application theme system, provide clear feedback on upload status, and indicate whether custom or default content is being used.

---

## 2. User Interface Overview

### 2.1 Page Structure

```
┌────────────────────────────────────────────────────────┐
│  Navigation Bar (existing _Layout.cshtml)              │
│  [Home] [Flashcards] [Quizzes] [Exercises] [Settings] │
│                                      [Study Materials]  │ <- New
└────────────────────────────────────────────────────────┘
│
│  ┌─────────────────────────────────────────────────┐
│  │  Manage Study Materials                         │
│  ├─────────────────────────────────────────────────┤
│  │                                                   │
│  │  ┌─────────────────────────────────────────┐   │
│  │  │ Terms and Definitions                   │   │
│  │  │ Status: Using Default Content     🔵    │   │
│  │  │ [Choose File] [Upload]                  │   │
│  │  └─────────────────────────────────────────┘   │
│  │                                                   │
│  │  ┌─────────────────────────────────────────┐   │
│  │  │ Equations                               │   │
│  │  │ Status: Custom Content Uploaded   ✅    │   │
│  │  │ File: Equations.md (245 KB)             │   │
│  │  │ Uploaded: May 27, 2026                  │   │
│  │  │ [Replace] [Delete] [Download]           │   │
│  │  └─────────────────────────────────────────┘   │
│  │                                                   │
│  │  ℹ️  Upload your custom markdown files to      │
│  │      personalize your study experience          │
│  │                                                   │
│  │  📄 Download sample templates:                  │
│  │     [Terms Template] [Equations Template]       │
│  │                                                   │
│  └─────────────────────────────────────────────────┘
```

---

## 3. Views and View Models

### 3.1 ManageStudyMaterialsViewModel

```csharp
namespace StudyHelper.ViewModels;

public class ManageStudyMaterialsViewModel
{
	public List<UserStudyMaterial> UserMaterials { get; set; } = new();
	public bool HasCustomTerms { get; set; }
	public bool HasCustomEquations { get; set; }

	public UserStudyMaterial? TermsMaterial => 
		UserMaterials.FirstOrDefault(m => m.MaterialType == StudyMaterialType.TermsAndDefinitions);

	public UserStudyMaterial? EquationsMaterial => 
		UserMaterials.FirstOrDefault(m => m.MaterialType == StudyMaterialType.Equations);
}
```

---

## 4. Views

### 4.1 Views/StudyMaterials/Manage.cshtml

```razor
@model ManageStudyMaterialsViewModel
@{
	ViewData["Title"] = "Manage Study Materials";
}

<div class="container mt-4">
	<div class="row">
		<div class="col-lg-10 mx-auto">
			<h1 class="mb-4">
				<i class="bi bi-file-earmark-text"></i> Manage Study Materials
			</h1>

			@if (TempData["SuccessMessage"] != null)
			{
				<div class="alert alert-success alert-dismissible fade show" role="alert">
					<i class="bi bi-check-circle-fill"></i> @TempData["SuccessMessage"]
					<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
				</div>
			}

			@if (TempData["ErrorMessage"] != null)
			{
				<div class="alert alert-danger alert-dismissible fade show" role="alert">
					<i class="bi bi-exclamation-triangle-fill"></i> @TempData["ErrorMessage"]
					<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
				</div>
			}

			<!-- Terms and Definitions Section -->
			<div class="card mb-4 shadow-sm">
				<div class="card-header bg-primary text-white">
					<h5 class="mb-0">
						<i class="bi bi-journal-text"></i> Terms and Definitions
					</h5>
				</div>
				<div class="card-body">
					@if (Model.HasCustomTerms)
					{
						var material = Model.TermsMaterial!;
						<div class="alert alert-success mb-3">
							<i class="bi bi-check-circle-fill"></i> 
							<strong>Status:</strong> Using Custom Content
						</div>

						<div class="row mb-3">
							<div class="col-md-6">
								<p class="mb-1"><strong>File Name:</strong> @material.FileName</p>
								<p class="mb-1"><strong>Size:</strong> @FormatFileSize(material.FileSizeBytes)</p>
							</div>
							<div class="col-md-6">
								<p class="mb-1"><strong>Uploaded:</strong> @material.UploadedDate.ToString("MMM dd, yyyy")</p>
							</div>
						</div>

						<div class="btn-group" role="group">
							<button type="button" class="btn btn-outline-primary" onclick="document.getElementById('replaceTermsFile').click()">
								<i class="bi bi-arrow-repeat"></i> Replace
							</button>
							<button type="button" class="btn btn-outline-danger" data-bs-toggle="modal" data-bs-target="#deleteTermsModal">
								<i class="bi bi-trash"></i> Delete
							</button>
						</div>

						<!-- Hidden form for replace -->
						<form asp-action="UploadTerms" method="post" enctype="multipart/form-data" id="replaceTermsForm">
							@Html.AntiForgeryToken()
							<input type="file" id="replaceTermsFile" name="file" accept=".md" style="display: none;" 
								   onchange="document.getElementById('replaceTermsForm').submit();" />
						</form>
					}
					else
					{
						<div class="alert alert-info mb-3">
							<i class="bi bi-info-circle-fill"></i> 
							<strong>Status:</strong> Using Default Content
						</div>

						<p class="text-muted">
							Upload a custom TermsAndDefinitions.md file to personalize your flashcard study content.
						</p>

						<form asp-action="UploadTerms" method="post" enctype="multipart/form-data" id="uploadTermsForm">
							@Html.AntiForgeryToken()
							<div class="mb-3">
								<label for="termsFile" class="form-label">Choose File</label>
								<input type="file" class="form-control" id="termsFile" name="file" accept=".md" required />
								<div class="form-text">Maximum file size: 10 MB. Only .md files accepted.</div>
							</div>
							<button type="submit" class="btn btn-primary">
								<i class="bi bi-upload"></i> Upload Terms and Definitions
							</button>
						</form>
					}
				</div>
			</div>

			<!-- Equations Section -->
			<div class="card mb-4 shadow-sm">
				<div class="card-header bg-success text-white">
					<h5 class="mb-0">
						<i class="bi bi-calculator"></i> Equations
					</h5>
				</div>
				<div class="card-body">
					@if (Model.HasCustomEquations)
					{
						var material = Model.EquationsMaterial!;
						<div class="alert alert-success mb-3">
							<i class="bi bi-check-circle-fill"></i> 
							<strong>Status:</strong> Using Custom Content
						</div>

						<div class="row mb-3">
							<div class="col-md-6">
								<p class="mb-1"><strong>File Name:</strong> @material.FileName</p>
								<p class="mb-1"><strong>Size:</strong> @FormatFileSize(material.FileSizeBytes)</p>
							</div>
							<div class="col-md-6">
								<p class="mb-1"><strong>Uploaded:</strong> @material.UploadedDate.ToString("MMM dd, yyyy")</p>
							</div>
						</div>

						<div class="btn-group" role="group">
							<button type="button" class="btn btn-outline-success" onclick="document.getElementById('replaceEquationsFile').click()">
								<i class="bi bi-arrow-repeat"></i> Replace
							</button>
							<button type="button" class="btn btn-outline-danger" data-bs-toggle="modal" data-bs-target="#deleteEquationsModal">
								<i class="bi bi-trash"></i> Delete
							</button>
						</div>

						<!-- Hidden form for replace -->
						<form asp-action="UploadEquations" method="post" enctype="multipart/form-data" id="replaceEquationsForm">
							@Html.AntiForgeryToken()
							<input type="file" id="replaceEquationsFile" name="file" accept=".md" style="display: none;" 
								   onchange="document.getElementById('replaceEquationsForm').submit();" />
						</form>
					}
					else
					{
						<div class="alert alert-info mb-3">
							<i class="bi bi-info-circle-fill"></i> 
							<strong>Status:</strong> Using Default Content
						</div>

						<p class="text-muted">
							Upload a custom Equations.md file to personalize your quiz and exercise content.
						</p>

						<form asp-action="UploadEquations" method="post" enctype="multipart/form-data" id="uploadEquationsForm">
							@Html.AntiForgeryToken()
							<div class="mb-3">
								<label for="equationsFile" class="form-label">Choose File</label>
								<input type="file" class="form-control" id="equationsFile" name="file" accept=".md" required />
								<div class="form-text">Maximum file size: 10 MB. Only .md files accepted.</div>
							</div>
							<button type="submit" class="btn btn-success">
								<i class="bi bi-upload"></i> Upload Equations
							</button>
						</form>
					}
				</div>
			</div>

			<!-- Help Section -->
			<div class="card mb-4 shadow-sm border-info">
				<div class="card-body">
					<h5 class="card-title">
						<i class="bi bi-info-circle"></i> Need Help?
					</h5>
					<p class="card-text">
						Your uploaded files should follow the same markdown format as the default content.
						Download sample templates to get started:
					</p>
					<a href="@Url.Action("DownloadTemplate", new { type = "terms" })" class="btn btn-outline-primary btn-sm me-2">
						<i class="bi bi-download"></i> Terms Template
					</a>
					<a href="@Url.Action("DownloadTemplate", new { type = "equations" })" class="btn btn-outline-success btn-sm">
						<i class="bi bi-download"></i> Equations Template
					</a>
				</div>
			</div>

		</div>
	</div>
</div>

<!-- Delete Terms Modal -->
<div class="modal fade" id="deleteTermsModal" tabindex="-1" aria-labelledby="deleteTermsModalLabel" aria-hidden="true">
	<div class="modal-dialog">
		<div class="modal-content">
			<div class="modal-header">
				<h5 class="modal-title" id="deleteTermsModalLabel">Delete Custom Terms?</h5>
				<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
			</div>
			<div class="modal-body">
				Are you sure you want to delete your custom TermsAndDefinitions.md file?
				<br /><br />
				<strong>You will revert to using the default content.</strong>
			</div>
			<div class="modal-footer">
				<button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
				<form asp-action="Delete" method="post" style="display: inline;">
					@Html.AntiForgeryToken()
					<input type="hidden" name="materialType" value="@((int)StudyMaterialType.TermsAndDefinitions)" />
					<button type="submit" class="btn btn-danger">Delete</button>
				</form>
			</div>
		</div>
	</div>
</div>

<!-- Delete Equations Modal -->
<div class="modal fade" id="deleteEquationsModal" tabindex="-1" aria-labelledby="deleteEquationsModalLabel" aria-hidden="true">
	<div class="modal-dialog">
		<div class="modal-content">
			<div class="modal-header">
				<h5 class="modal-title" id="deleteEquationsModalLabel">Delete Custom Equations?</h5>
				<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
			</div>
			<div class="modal-body">
				Are you sure you want to delete your custom Equations.md file?
				<br /><br />
				<strong>You will revert to using the default content.</strong>
			</div>
			<div class="modal-footer">
				<button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
				<form asp-action="Delete" method="post" style="display: inline;">
					@Html.AntiForgeryToken()
					<input type="hidden" name="materialType" value="@((int)StudyMaterialType.Equations)" />
					<button type="submit" class="btn btn-danger">Delete</button>
				</form>
			</div>
		</div>
	</div>
</div>

@functions {
	string FormatFileSize(long bytes)
	{
		if (bytes < 1024) return $"{bytes} B";
		if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} KB";
		return $"{bytes / (1024.0 * 1024.0):F2} MB";
	}
}

@section Scripts {
	<script src="~/js/study-materials.js"></script>
}
```

---

## 5. JavaScript Enhancement

### 5.1 wwwroot/js/study-materials.js

```javascript
// Study Materials Upload Enhancement
(function () {
	'use strict';

	// File size validation (client-side)
	const maxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

	function validateFileSize(input) {
		if (input.files && input.files[0]) {
			const file = input.files[0];

			if (file.size > maxFileSizeBytes) {
				alert('File is too large. Maximum size is 10 MB.');
				input.value = '';
				return false;
			}

			// Check file extension
			if (!file.name.toLowerCase().endsWith('.md')) {
				alert('Please select a .md (Markdown) file.');
				input.value = '';
				return false;
			}
		}
		return true;
	}

	// Attach validation to file inputs
	document.addEventListener('DOMContentLoaded', function () {
		const termsFile = document.getElementById('termsFile');
		const equationsFile = document.getElementById('equationsFile');

		if (termsFile) {
			termsFile.addEventListener('change', function () {
				validateFileSize(this);
			});
		}

		if (equationsFile) {
			equationsFile.addEventListener('change', function () {
				validateFileSize(this);
			});
		}

		// Show loading spinner on form submit
		const uploadForms = document.querySelectorAll('form[enctype="multipart/form-data"]');
		uploadForms.forEach(form => {
			form.addEventListener('submit', function () {
				const btn = this.querySelector('button[type="submit"]');
				if (btn) {
					btn.disabled = true;
					btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Uploading...';
				}
			});
		});

		// Auto-dismiss alerts after 5 seconds
		const alerts = document.querySelectorAll('.alert');
		alerts.forEach(alert => {
			setTimeout(() => {
				const bsAlert = new bootstrap.Alert(alert);
				bsAlert.close();
			}, 5000);
		});
	});
})();
```

---

## 6. CSS Styling

### 6.1 wwwroot/css/study-materials.css

```css
/* Study Materials Management Page Styles */

.study-materials-container {
	max-width: 900px;
	margin: 0 auto;
}

.material-card {
	transition: box-shadow 0.3s ease;
}

.material-card:hover {
	box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
}

.file-upload-area {
	border: 2px dashed var(--bs-border-color);
	border-radius: 0.375rem;
	padding: 2rem;
	text-align: center;
	transition: border-color 0.3s ease, background-color 0.3s ease;
}

.file-upload-area:hover {
	border-color: var(--bs-primary);
	background-color: var(--bs-light);
}

.file-upload-area.drag-over {
	border-color: var(--bs-success);
	background-color: rgba(25, 135, 84, 0.1);
}

.material-status-badge {
	font-size: 0.875rem;
	padding: 0.5rem 1rem;
}

.btn-group-file-actions {
	display: flex;
	gap: 0.5rem;
	flex-wrap: wrap;
}

@media (max-width: 576px) {
	.btn-group-file-actions {
		flex-direction: column;
	}

	.btn-group-file-actions .btn {
		width: 100%;
	}
}

/* Loading spinner for uploads */
.upload-spinner {
	position: fixed;
	top: 50%;
	left: 50%;
	transform: translate(-50%, -50%);
	z-index: 9999;
	background: rgba(255, 255, 255, 0.9);
	padding: 2rem;
	border-radius: 0.5rem;
	box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.3);
	display: none;
}

.upload-spinner.active {
	display: block;
}
```

---

## 7. Navigation Integration

### 7.1 Update Views/Shared/_Layout.cshtml

Add a new navigation item for Study Materials:

```razor
<li class="nav-item">
	<a class="nav-link" asp-controller="StudyMaterials" asp-action="Manage">
		<i class="bi bi-file-earmark-text"></i> Study Materials
	</a>
</li>
```

Insert this after the "Settings" nav item and before the logout section.

---

## 8. User Flows

### 8.1 Upload New Custom Terms

1. User navigates to **Study Materials** from navigation
2. User sees "Terms and Definitions" card showing "Using Default Content"
3. User clicks **Choose File**, selects `MyTerms.md`
4. Client-side validation checks file size and extension
5. User clicks **Upload Terms and Definitions**
6. Loading spinner appears on button ("Uploading...")
7. Server validates and stores file
8. Page refreshes with success message
9. Card now shows "Using Custom Content" with file details

### 8.2 Replace Existing Custom File

1. User sees "Using Custom Content" status
2. User clicks **Replace** button
3. File picker opens immediately
4. User selects new file
5. Form auto-submits
6. Loading indicator appears
7. Server processes and replaces file
8. Page refreshes with success message

### 8.3 Delete Custom File and Revert to Default

1. User clicks **Delete** button
2. Confirmation modal appears: "Delete Custom Terms?"
3. Modal warns: "You will revert to using the default content"
4. User confirms deletion
5. Server deletes custom file
6. Page refreshes with success message
7. Card now shows "Using Default Content"

### 8.4 Download Template

1. User clicks **Terms Template** or **Equations Template** in help section
2. Browser downloads sample markdown file
3. User can edit and upload the template

---

## 9. Responsive Design

### 9.1 Mobile (< 768px)
- Single column layout
- Buttons stack vertically
- File input takes full width
- Modals adjust to screen size

### 9.2 Tablet (768px - 1024px)
- Two-column card layout where appropriate
- Buttons remain in button groups

### 9.3 Desktop (> 1024px)
- Centered container with max-width of 900px
- Cards side-by-side for file metadata
- Full button groups

---

## 10. Accessibility

### 10.1 ARIA Labels
- All form inputs have associated labels
- File inputs have `aria-describedby` for help text
- Modals have proper `aria-labelledby` and `aria-hidden`

### 10.2 Keyboard Navigation
- All interactive elements accessible via Tab key
- Modal can be dismissed with Escape key
- Forms can be submitted with Enter key

### 10.3 Screen Reader Support
- Status indicators announced with icon + text
- Loading states announced
- Success/error messages announced via alert roles

---

## 11. Theme Integration

All styles use CSS variables from the existing theme system:

```css
--bs-primary
--bs-success
--bs-danger
--bs-info
--bs-border-color
--bs-light
--bs-dark
```

The page will automatically adapt to the user's selected theme (default, dark-mode, high-contrast, etc.).

---

## 12. Error States

### 12.1 File Too Large
```html
<div class="alert alert-danger">
	<i class="bi bi-exclamation-triangle-fill"></i>
	Upload failed. File size exceeds 10 MB limit.
</div>
```

### 12.2 Invalid Format
```html
<div class="alert alert-danger">
	<i class="bi bi-exclamation-triangle-fill"></i>
	Upload failed. File format validation failed. Please check your markdown syntax.
</div>
```

### 12.3 Malicious Content Detected
```html
<div class="alert alert-danger">
	<i class="bi bi-exclamation-triangle-fill"></i>
	Upload failed. File content failed security validation.
</div>
```

### 12.4 Network/Server Error
```html
<div class="alert alert-danger">
	<i class="bi bi-exclamation-triangle-fill"></i>
	Upload failed. Please try again later.
</div>
```

---

## 13. Success States

### 13.1 Upload Success
```html
<div class="alert alert-success alert-dismissible fade show">
	<i class="bi bi-check-circle-fill"></i>
	Terms and definitions uploaded successfully!
	<button type="button" class="btn-close" data-bs-dismiss="alert"></button>
</div>
```

### 13.2 Delete Success
```html
<div class="alert alert-success alert-dismissible fade show">
	<i class="bi bi-check-circle-fill"></i>
	TermsAndDefinitions deleted. Using default content.
	<button type="button" class="btn-close" data-bs-dismiss="alert"></button>
</div>
```

---

## 14. Loading States

### 14.1 Upload Button
```html
<button type="submit" class="btn btn-primary" disabled>
	<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
	Uploading...
</button>
```

### 14.2 Page Load
Show skeleton loaders for cards while data loads (if implemented as SPA in future).

---

## 15. Template Download Feature

### 15.1 Controller Method

```csharp
[HttpGet]
public IActionResult DownloadTemplate(string type)
{
	string fileName;
	string defaultPath;

	if (type == "terms")
	{
		fileName = "TermsAndDefinitions_Template.md";
		defaultPath = Path.Combine(_environment.ContentRootPath, 
			"../InputDocuments/Accumulating/TermsAndDefinitions.md");
	}
	else if (type == "equations")
	{
		fileName = "Equations_Template.md";
		defaultPath = Path.Combine(_environment.ContentRootPath, 
			"../InputDocuments/Accumulating/Equations.md");
	}
	else
	{
		return NotFound();
	}

	var fileBytes = System.IO.File.ReadAllBytes(defaultPath);
	return File(fileBytes, "text/markdown", fileName);
}
```

---

## 16. Bootstrap Icons Required

Ensure Bootstrap Icons are included in _Layout.cshtml:

```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css">
```

Icons used:
- `bi-file-earmark-text` - File document
- `bi-journal-text` - Terms book
- `bi-calculator` - Equations
- `bi-check-circle-fill` - Success
- `bi-info-circle-fill` - Info
- `bi-exclamation-triangle-fill` - Error/Warning
- `bi-upload` - Upload action
- `bi-download` - Download action
- `bi-arrow-repeat` - Replace action
- `bi-trash` - Delete action

---

## 17. Testing Checklist

### 17.1 Functional Tests
- [ ] Upload terms file successfully
- [ ] Upload equations file successfully
- [ ] Replace existing file
- [ ] Delete file and revert to default
- [ ] Download template files
- [ ] Client-side file size validation
- [ ] Client-side file extension validation

### 17.2 UI/UX Tests
- [ ] Responsive layout on mobile
- [ ] Responsive layout on tablet
- [ ] Responsive layout on desktop
- [ ] Theme compatibility (all 17 themes)
- [ ] Loading states display correctly
- [ ] Success messages auto-dismiss
- [ ] Error messages display clearly

### 17.3 Accessibility Tests
- [ ] Keyboard navigation works
- [ ] Screen reader announces status
- [ ] Focus management in modals
- [ ] Color contrast passes WCAG AA

---

## 18. Implementation Dependencies

### 18.1 Backend Dependencies
- StudyMaterialsController must be implemented
- IUserStudyMaterialService must be registered in DI
- All backend endpoints tested

### 18.2 Frontend Assets
- study-materials.css created
- study-materials.js created
- Bootstrap Icons CDN included
- Navigation link added to _Layout.cshtml

---

## 19. Success Criteria

1. ✅ User can navigate to Study Materials page
2. ✅ User can upload custom terms file
3. ✅ User can upload custom equations file
4. ✅ UI clearly shows custom vs default status
5. ✅ User can replace existing files
6. ✅ User can delete files with confirmation
7. ✅ User can download template files
8. ✅ All feedback messages are clear and helpful
9. ✅ Page is fully responsive
10. ✅ Page works with all themes
11. ✅ Accessibility requirements met

---

## 20. Future Enhancements (Out of Scope)

- Drag-and-drop file upload
- Live markdown preview before upload
- File versioning UI
- Bulk upload multiple files
- Export/backup user materials
- Share materials between users

---

**Document Status**: ✅ Ready for Human Review  
**Next Step**: Human Approval → Frontend + Backend Engineers → Implementation
