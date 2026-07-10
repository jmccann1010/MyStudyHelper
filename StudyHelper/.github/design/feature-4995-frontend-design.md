# Feature #4995: Login Authentication - Frontend Design

## UI/UX Overview

### Design Goals
- Clean, professional login/register pages
- Consistent with existing application theme
- Mobile-responsive design
- Clear error messaging
- Minimal friction for new users

### Page Structure
```
Views/
  Account/
	Login.cshtml           - Login page
	Register.cshtml        - Registration page
  Shared/
	_Layout.cshtml         - Updated with user info/logout
```

## Login Page Design

### Route
- **URL**: `/Account/Login?returnUrl={optional}`
- **Allowed**: Anonymous users only
- **Redirect**: Authenticated users to home page

### Layout
```
┌─────────────────────────────────────┐
│      Study Helper         │
│         Login                        │
├─────────────────────────────────────┤
│                                     │
│  ┌─────────────────────────────┐   │
│  │ Username                    │   │
│  │ [_____________________]     │   │
│  │                             │   │
│  │ Password                    │   │
│  │ [_____________________]     │   │
│  │                             │   │
│  │ [ ] Remember me             │   │
│  │                             │   │
│  │    [    Login    ]          │   │
│  │                             │   │
│  │ Don't have an account?      │   │
│  │ Create one                  │   │
│  └─────────────────────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

### Views/Account/Login.cshtml
```html
@model StudyHelper.ViewModels.LoginViewModel
@{
	ViewData["Title"] = "Login";
}

<div class="row justify-content-center mt-5">
	<div class="col-md-6 col-lg-4">
		<div class="card shadow">
			<div class="card-body p-4">
				<h2 class="card-title text-center mb-4">Login</h2>

				<form asp-action="Login" method="post">
					<input type="hidden" asp-for="@ViewData["ReturnUrl"]" />

					<div asp-validation-summary="ModelOnly" class="alert alert-danger" role="alert"></div>

					<div class="mb-3">
						<label asp-for="Username" class="form-label"></label>
						<input asp-for="Username" class="form-control" autofocus />
						<span asp-validation-for="Username" class="text-danger"></span>
					</div>

					<div class="mb-3">
						<label asp-for="Password" class="form-label"></label>
						<input asp-for="Password" class="form-control" type="password" />
						<span asp-validation-for="Password" class="text-danger"></span>
					</div>

					<div class="mb-3 form-check">
						<input asp-for="RememberMe" class="form-check-input" type="checkbox" />
						<label asp-for="RememberMe" class="form-check-label"></label>
					</div>

					<div class="d-grid mb-3">
						<button type="submit" class="btn btn-primary btn-lg">Login</button>
					</div>

					<div class="text-center">
						<p class="mb-0">Don't have an account?</p>
						<a asp-action="Register" class="text-decoration-none">Create one</a>
					</div>
				</form>
			</div>
		</div>
	</div>
</div>

@section Scripts {
	<partial name="_ValidationScriptsPartial" />
}
```

### Features
- **Auto-focus**: Username field receives focus on page load
- **Remember Me**: Extends session to 30 days
- **Return URL**: Redirects to originally requested page after login
- **Validation**: Client and server-side validation
- **Error Display**: Model-level errors shown in alert box
- **Registration Link**: Easy navigation to registration page

## Registration Page Design

### Route
- **URL**: `/Account/Register`
- **Allowed**: Anonymous users only
- **Redirect**: Authenticated users to home page

### Layout
```
┌─────────────────────────────────────┐
│      Study Helper         │
│       Create Account                 │
├─────────────────────────────────────┤
│                                     │
│  ┌─────────────────────────────┐   │
│  │ Username                    │   │
│  │ [_____________________]     │   │
│  │                             │   │
│  │ Password                    │   │
│  │ [_____________________]     │   │
│  │                             │   │
│  │ Confirm Password            │   │
│  │ [_____________________]     │   │
│  │                             │   │
│  │  [  Create Account  ]       │   │
│  │                             │   │
│  │ Already have an account?    │   │
│  │ Login                       │   │
│  └─────────────────────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

### Views/Account/Register.cshtml
```html
@model StudyHelper.ViewModels.RegisterViewModel
@{
	ViewData["Title"] = "Register";
}

<div class="row justify-content-center mt-5">
	<div class="col-md-6 col-lg-4">
		<div class="card shadow">
			<div class="card-body p-4">
				<h2 class="card-title text-center mb-4">Create Account</h2>

				<form asp-action="Register" method="post">
					<div asp-validation-summary="ModelOnly" class="alert alert-danger" role="alert"></div>

					<div class="mb-3">
						<label asp-for="Username" class="form-label"></label>
						<input asp-for="Username" class="form-control" autofocus />
						<span asp-validation-for="Username" class="text-danger"></span>
						<small class="form-text text-muted">3-50 characters</small>
					</div>

					<div class="mb-3">
						<label asp-for="Password" class="form-label"></label>
						<input asp-for="Password" class="form-control" type="password" />
						<span asp-validation-for="Password" class="text-danger"></span>
						<small class="form-text text-muted">Minimum 8 characters</small>
					</div>

					<div class="mb-3">
						<label asp-for="ConfirmPassword" class="form-label"></label>
						<input asp-for="ConfirmPassword" class="form-control" type="password" />
						<span asp-validation-for="ConfirmPassword" class="text-danger"></span>
					</div>

					<div class="d-grid mb-3">
						<button type="submit" class="btn btn-primary btn-lg">Create Account</button>
					</div>

					<div class="text-center">
						<p class="mb-0">Already have an account?</p>
						<a asp-action="Login" class="text-decoration-none">Login</a>
					</div>
				</form>
			</div>
		</div>
	</div>
</div>

@section Scripts {
	<partial name="_ValidationScriptsPartial" />
}
```

### Features
- **Password Requirements**: Helper text shows requirements
- **Confirm Password**: Validates match client-side
- **Username Validation**: Real-time format validation
- **Auto-Login**: User logged in immediately after registration
- **Login Link**: Easy navigation back to login page

## Layout Updates

### Views/Shared/_Layout.cshtml

#### Navbar User Info Section
```html
<ul class="navbar-nav ms-auto">
	@if (User.Identity?.IsAuthenticated == true)
	{
		<li class="nav-item dropdown">
			<a class="nav-link dropdown-toggle" href="#" id="userDropdown" 
			   role="button" data-bs-toggle="dropdown" aria-expanded="false">
				<i class="bi bi-person-circle"></i> @User.Identity.Name
			</a>
			<ul class="dropdown-menu dropdown-menu-end" aria-labelledby="userDropdown">
				<li><h6 class="dropdown-header">@User.Identity.Name</h6></li>
				<li><hr class="dropdown-divider"></li>
				<li>
					<form asp-controller="Account" asp-action="Logout" method="post" class="d-inline">
						<button type="submit" class="dropdown-item">
							<i class="bi bi-box-arrow-right"></i> Logout
						</button>
					</form>
				</li>
			</ul>
		</li>
	}
</ul>
```

**Features:**
- User icon with username displayed
- Dropdown menu for user actions
- Logout button submits form (prevents CSRF)
- Bootstrap icons for visual clarity

## CSS Styling

### wwwroot/css/account.css
```css
/* Login and Register Page Styles */

.account-page {
	min-height: 100vh;
	display: flex;
	align-items: center;
	background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.account-card {
	border: none;
	border-radius: 1rem;
}

.account-card .card-body {
	padding: 2rem;
}

.account-card h2 {
	font-weight: 600;
	color: #2d3748;
}

.form-control:focus {
	border-color: #667eea;
	box-shadow: 0 0 0 0.2rem rgba(102, 126, 234, 0.25);
}

.btn-primary {
	background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
	border: none;
	padding: 0.75rem;
	font-weight: 600;
	transition: transform 0.2s;
}

.btn-primary:hover {
	transform: translateY(-2px);
	box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.text-danger {
	font-size: 0.875rem;
	margin-top: 0.25rem;
}

.alert-danger {
	border-radius: 0.5rem;
	background-color: #fee;
	border-color: #fcc;
	color: #c33;
}

/* User dropdown in navbar */
.navbar .dropdown-menu {
	border-radius: 0.5rem;
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
	border: 1px solid #e2e8f0;
}

.navbar .dropdown-header {
	color: #4a5568;
	font-weight: 600;
}

.navbar .dropdown-item {
	padding: 0.5rem 1rem;
	transition: background-color 0.2s;
}

.navbar .dropdown-item:hover {
	background-color: #f7fafc;
}

.navbar .dropdown-item i {
	margin-right: 0.5rem;
}
```

**Features:**
- Professional gradient background for account pages
- Smooth animations on button hover
- Consistent color scheme with validation states
- Responsive card design
- Accessible focus states

## Responsive Design

### Mobile Breakpoints
```css
/* Small devices (landscape phones, 576px and up) */
@media (max-width: 767.98px) {
	.account-page {
		padding: 1rem;
	}

	.account-card .card-body {
		padding: 1.5rem;
	}

	.account-card h2 {
		font-size: 1.5rem;
	}
}

/* Extra small devices (portrait phones, less than 576px) */
@media (max-width: 575.98px) {
	.account-card .card-body {
		padding: 1rem;
	}

	.btn-primary {
		padding: 0.5rem;
	}
}
```

### Mobile Features
- Touch-friendly button sizes
- Reduced padding on small screens
- Stacked form layout
- Larger touch targets for links

## Client-Side Validation

### jQuery Validation Rules
```javascript
// Custom validation messages
$.validator.addMethod("strongPassword", function(value, element) {
	return this.optional(element) || 
		   /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/.test(value);
}, "Password must contain at least one uppercase letter, one lowercase letter, and one number");

// Apply to registration form
$("#registerForm").validate({
	rules: {
		Password: {
			strongPassword: true
		}
	}
});
```

**Validation Features:**
- Real-time validation as user types
- Password strength indicator
- Username format validation
- Confirm password match
- Bootstrap styling for error states

## Accessibility

### WCAG 2.1 AA Compliance
- **Labels**: All inputs have associated labels
- **ARIA**: Error messages linked with `aria-describedby`
- **Focus Management**: Clear focus indicators
- **Keyboard Navigation**: Full keyboard support
- **Color Contrast**: Text meets 4.5:1 contrast ratio
- **Screen Readers**: Semantic HTML and ARIA labels

### ARIA Labels
```html
<input asp-for="Username" 
	   class="form-control" 
	   aria-required="true"
	   aria-describedby="usernameHelp usernameError" />
<span id="usernameHelp" class="form-text">3-50 characters</span>
<span id="usernameError" asp-validation-for="Username" class="text-danger"></span>
```

## Error Messaging

### Field Validation Errors
- Display below input field
- Red text with icon
- Clear description of issue

### Form-Level Errors
- Alert box at top of form
- Dismissible with close button
- List of all errors if multiple

### Error Messages
```
Username is required
Username must be between 3 and 50 characters
Username already exists

Password is required
Password must be at least 8 characters
Password must contain at least one uppercase letter
Password must contain at least one lowercase letter
Password must contain at least one number

Passwords do not match

Invalid username or password
An error occurred. Please try again later.
```

## Loading States

### Submit Button
```html
<button type="submit" class="btn btn-primary btn-lg" id="submitBtn">
	<span class="spinner-border spinner-border-sm d-none" role="status"></span>
	<span class="btn-text">Login</span>
</button>

<script>
document.getElementById('loginForm').addEventListener('submit', function() {
	const btn = document.getElementById('submitBtn');
	btn.disabled = true;
	btn.querySelector('.spinner-border').classList.remove('d-none');
	btn.querySelector('.btn-text').textContent = 'Logging in...';
});
</script>
```

**Features:**
- Spinner animation during submission
- Button disabled to prevent double-submit
- Text changes to show progress

## Theme Integration

### Theme Support
All account pages inherit the current theme from `theme-loader.js`:
- Background colors
- Text colors
- Button styles
- Card styling
- Input styling

### Dark Mode Considerations
```css
[data-theme="theme-dark-mode"] .account-page {
	background: linear-gradient(135deg, #1a202c 0%, #2d3748 100%);
}

[data-theme="theme-dark-mode"] .account-card {
	background-color: #2d3748;
}

[data-theme="theme-dark-mode"] .account-card h2 {
	color: #f7fafc;
}

[data-theme="theme-dark-mode"] .form-control {
	background-color: #1a202c;
	border-color: #4a5568;
	color: #f7fafc;
}
```

## User Experience Flow

### First-Time User
1. Navigate to application
2. Redirected to clean, welcoming login page
3. See clear "Create Account" link
4. Fill simple registration form
5. Immediately logged in
6. Welcome message or tour (future enhancement)

### Returning User
1. Navigate to application
2. Redirected to login page
3. Remember username from last visit
4. Check "Remember me" for extended session
5. Login with saved credentials
6. Redirected to last visited page

### Error Recovery
1. Enter invalid credentials
2. See clear error message
3. Username field retained
4. Password field cleared
5. Focus on password field
6. Try again without leaving page

## Progressive Enhancement

### Base Experience (No JavaScript)
- Forms work with standard POST
- Server-side validation
- Full page refreshes
- Basic Bootstrap styling

### Enhanced Experience (JavaScript)
- Client-side validation
- Loading indicators
- Smooth animations
- AJAX form submission (future)
- Password strength meter (future)

## Future Enhancements

### Password Reset
- "Forgot password?" link on login
- Security question verification
- Email-based reset (if email added)

### Social Login
- OAuth provider buttons
- Google, Microsoft login
- Link social accounts

### Two-Factor Authentication
- Optional 2FA setup
- QR code for authenticator apps
- Backup codes

### Remember Device
- Option to remember device
- Reduce authentication prompts
- Device management page
