# Frontend Design: Fix Encrypted Passwords

## Overview

This feature has **no frontend UI changes**. All five User Stories (US-001 through US-005)
are backend-only fixes within `UserService.cs` and `Program.cs`.

This document exists to confirm that scope, record the reasoning, and document the
one indirect frontend consideration: the login error message shown to users when
`ValidateUserAsync` returns `null`.

---

## User Stories Addressed

- US-001: Hash passwords on user registration
- US-002: Fix password verification to use hash comparison
- US-003: Remove hardcoded username allowlist from login
- US-004: Migrate existing plaintext stored passwords to hashed passwords
- US-005: Re-enable file encryption for `users.dat`

---

## Component Design

### No new or modified components

The existing login and registration views are correct and require no changes:

| Component | File | Status |
|---|---|---|
| Login view | `Views/Account/Login.cshtml` | No change |
| Register view | `Views/Account/Register.cshtml` | No change |
| `LoginViewModel` | `ViewModels/LoginViewModel.cs` | No change |
| `RegisterViewModel` | `ViewModels/RegisterViewModel.cs` | No change |
| `AccountController` | `Controllers/AccountController.cs` | No change |
| Layout / navigation | `Views/Shared/_Layout.cshtml` | No change |

---

## Data Flow

The login and registration flows are unchanged end-to-end from the frontend perspective:

### Login Flow (unchanged)

```
User submits Login form
  → POST /Account/Login
  → AccountController.Login(LoginViewModel)
  → IUserService.ValidateUserAsync(username, password)   ← fix is here (backend)
  → Returns User or null
  → Redirect to Home or redisplay Login with error
```

### Registration Flow (unchanged)

```
User submits Register form
  → POST /Account/Register
  → AccountController.Register(RegisterViewModel)
  → IUserService.CreateUserAsync(username, password)     ← fix is here (backend)
  → Returns bool success
  → Auto-login and redirect to Home, or redisplay Register with error
```

---

## UI/UX Considerations

### Login error message

`AccountController.Login` already adds a generic model error on failed login:

```csharp
ModelState.AddModelError(string.Empty, "Invalid username or password");
```

This message is correct and must **not** be changed. It deliberately avoids confirming
whether the username exists or the password was wrong (username enumeration prevention).

After US-003 removes the hardcoded allowlist, users who previously could not log in
(because their username was not `mccannj5` or `hoffmanj7`) will now correctly receive this
generic error rather than silently failing — which is the expected improvement in behavior.

### No visible change for successful logins

Users whose passwords are already stored correctly (post-fix registrations) will
experience no change in the login flow. The migration (US-004) runs silently at startup
with no user-facing output.

### Registration password length

`RegisterViewModel` already enforces an 8-character minimum via data annotation:

```csharp
[StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
```

This aligns with `appsettings.json` `PasswordPolicy:RequiredLength = 8`. No change needed.

---

## Technology Decisions

| Decision | Rationale |
|---|---|
| No frontend changes | All defects are in the service layer; the view/controller contract is unaffected |
| Generic login error message retained | Prevents username enumeration; already correct |

---

## Open Questions / Risks

| # | Question / Risk |
|---|---|
| 1 | None. The frontend surface is stable and correct. All risk is in the backend (see backend design document). |
