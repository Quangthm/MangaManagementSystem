# Logout and Anti-forgery Hardening

**Date:** 2026-06-17

**Branch:** `feature/security-rbac-hardening`

**Status:** Completed and locally verified

## Scope

This session completed Phase 2 security item 28:

* Consolidate logout into one protected POST endpoint.

* Require anti-forgery validation for logout.

* Remove the legacy GET logout endpoint.

* Remove logout implementations that only navigate to Login without deleting the authentication cookie.

* Ensure protected pages require authentication again after logout.

## Implemented Changes

### 1. Consolidated logout flow

The application now uses one logout endpoint:

`POST /api/auth/logout`

The endpoint:

* Requires an authenticated user.

* Validates the anti-forgery token.

* Deletes the Cookie Authentication session.

* Redirects the browser to `/login`.

### 2. Removed legacy GET logout

The following endpoint was removed:

`GET /signout`

Opening `/signout` now returns HTTP 404.

This prevents state-changing logout operations from being performed through a GET request.

### 3. Added anti-forgery protection

Logout requests must include a valid anti-forgery token.

A request without a token returns:

```text

400 Bad Request

{"message":"Invalid logout request."}

```

The invalid request does not delete the current authentication session.

### 4. Added a global logout form

A hidden global form is rendered with:

* `POST` method.

* `/api/auth/logout` action.

* ASP.NET Core anti-forgery token.

* Enhanced navigation disabled.

Logout buttons submit this global form.

This allows logout buttons inside dynamically rendered menus to use a valid token.

### 5. Replaced fake logout navigation

Logout controls no longer use:

```text

NavigateTo("/login")

```

or:

```text

NavigateTo("/signout")

```

Navigation alone did not delete the authentication cookie.

All logout controls now submit the protected logout form.

### 6. Simplified authentication state provider

The unused `MarkUserAsLoggedOut` method was removed.

Logout state is now controlled by the server-side authentication cookie and the canonical POST endpoint.

## Files Changed

* `src/MangaManagementSystem.Web/Components/App.razor`

* `src/MangaManagementSystem.Web/Components/Pages/Mangaka/MangakaDashboard.razor`

* `src/MangaManagementSystem.Web/Components/Shared/LogoutForm.razor`

* `src/MangaManagementSystem.Web/Components/Shared/WorkspaceDemo.razor`

* `src/MangaManagementSystem.Web/Program.cs`

* `src/MangaManagementSystem.Web/Services/CustomAuthenticationStateProvider.cs`

## Verification

### Build

Executed:

```text

dotnet build

```

Result:

```text

Build succeeded with warnings and 0 errors.

```

The warnings were existing warnings outside the scope of this security item.

### Missing anti-forgery token test

Submitted:

```text

POST /api/auth/logout

```

without an anti-forgery token.

Result:

```text

400 Bad Request

{"message":"Invalid logout request."}

```

The authenticated Mangaka session remained active.

Result: **Passed**

### Legacy GET endpoint test

Opened:

```text

GET /signout

```

Result:

```text

HTTP 404 Not Found

```

Result: **Passed**

### Valid logout test

An active Mangaka used the Logout button in the account menu.

Result:

```text

/login

```

The authentication cookie was removed.

Result: **Passed**

### Protected route after logout test

After logout, the browser opened:

```text

/mangaka

```

Result:

```text

/login?ReturnUrl=%2Fmangaka

```

The protected Mangaka dashboard was not accessible without logging in again.

Result: **Passed**

## Security Result

The logout workflow now follows the required security model:

```text

Authenticated user

→ POST logout with anti-forgery token

→ Cookie session deleted

→ Redirect to Login

→ Protected routes require authentication again

```

## Current Phase 2 Progress

Completed:

* Item 25: Protect Profile and Portfolio endpoints.

* Item 26: Remove the Portfolio debug endpoint.

* Item 27: Separate Pending Approval and Access Denied.

* Item 28: Consolidate Logout and enforce anti-forgery protection.
