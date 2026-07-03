# Pending Approval and Access Denied Separation

**Date:** 2026-06-17

**Branch:** `feature/security-rbac-hardening`

**Status:** Completed and locally verified

## Scope

This session completed Phase 2 security item 27:

* Separate Pending Approval from Access Denied.

* Route unauthenticated users to Login.

* Route authenticated users with an unauthorized role to Access Denied.

* Route pending accounts to the dedicated Pending Approval page.

* Preserve normal access for active accounts with the correct role.

* Preserve explicit Google Login after changing the default authentication challenge.

## Problem

The cookie authentication configuration previously used:

`AccessDeniedPath = "/pending-approval"`

This incorrectly treated two different situations as the same:

* An account that has not yet been approved.

* An authenticated account that does not have permission to access a page.

The route authorization fallback also displayed one generic access-denied message for both anonymous and authenticated users.

Additionally, Google login returned the generic error:

`User is not active.`

Therefore, the Web project could not distinguish between:

* `PENDING\_APPROVAL`

* `DISABLED`

* `REJECTED`

The default authentication challenge scheme was also configured as Google authentication. As a result, an anonymous user opening a protected page was redirected directly to Google OAuth instead of the MangaFlow Login page.

## Implemented Changes

### 1. Added a dedicated Access Denied page

Added the following route:

`/access-denied`

The page explains that the user is already authenticated, but their current role does not permit access to the requested function.

The page provides navigation options to:

* Return to the home page.

* Open Profile Settings.

The Access Denied page does not require authorization, preventing a redirect loop.

### 2. Separated authentication and authorization redirects

The authorization router now distinguishes between two situations:

* Anonymous user: redirect to `/login`.

* Authenticated user with insufficient role: redirect to `/access-denied`.

A dedicated Razor component reads the current authentication state before selecting the redirect destination.

### 3. Corrected cookie authentication paths

Cookie authentication now uses:

* `LoginPath = "/login"`

* `AccessDeniedPath = "/access-denied"`

Pending Approval is no longer configured as the generic access-denied destination.

### 4. Corrected the default authentication challenge

The Cookie authentication scheme is now used as:

* Default authentication scheme.

* Default sign-in scheme.

* Default challenge scheme.

This ensures that an anonymous user opening a protected MangaFlow page is redirected to the MangaFlow Login page instead of being sent directly to Google OAuth.

### 5. Preserved explicit Google Login

Google authentication remains available through the explicit endpoint:

`POST /api/auth/google-login`

That endpoint explicitly challenges:

`GoogleDefaults.AuthenticationScheme`

Therefore, changing the default challenge scheme to Cookie does not remove or break the Login with Google function.

### 6. Corrected password login for pending accounts

A username/password login attempt from an account with status:

`PENDING\_APPROVAL`

now redirects directly to:

`/pending-approval`

It no longer redirects to:

`/login?error=account\_pending`

This keeps Pending Approval separate from authentication errors and authorization failures.

### 7. Corrected Google login account-status handling

Google login now uses the same account-status validation rules as password login.

The following account statuses are distinguished:

* `ACTIVE`: login is allowed.

* `PENDING\_APPROVAL`: redirect to `/pending-approval`.

* `DISABLED`: redirect to Login with the disabled-account message.

* `REJECTED`: redirect to Login with the rejected-account message.

* Unknown or invalid status: login is rejected safely.

### 8. Reused the existing account-status validation

`AuthService.GetUserByEmailAsync` now uses the existing account-status validation method instead of returning one generic inactive-account message.

This keeps password login and Google login consistent.

The shared status validation produces the following results:

* `ACTIVE`: continue authentication.

* `PENDING\_APPROVAL`: `Account pending admin approval.`

* `REJECTED`: `Account registration was rejected.`

* `DISABLED`: `Account is disabled.`

### 9. Corrected unknown-role fallback

An authenticated account with an unsupported or unauthorized role now redirects to:

`/access-denied`

It is no longer treated as an invalid username or password.

### 10. Replaced the generic NotAuthorized content

The previous `Routes.razor` implementation displayed the same Access Denied message for every authorization failure.

It has been replaced with a redirect component that determines whether the current user is:

* Anonymous.

* Authenticated but unauthorized.

This prevents misleading messages such as asking an authenticated user to log in again.

## Verification

### Build

Executed:

```text

dotnet build

```

Result:

```text

Build succeeded with 35 warning(s) and 0 errors.

```

All five projects compiled successfully:

* `MangaManagementSystem.Domain`

* `MangaManagementSystem.Application`

* `MangaManagementSystem.Infrastructure`

* `MangaManagementSystem.API`

* `MangaManagementSystem.Web`

The existing warnings were outside the scope of this security item.

### Anonymous access test

An anonymous browser session opened:

```text

/admin/user-approval

```

Result:

```text

/login?ReturnUrl=%2Fadmin%2Fuser-approval

```

The MangaFlow Login page was displayed.

The anonymous user was not redirected to:

* Google OAuth.

* Pending Approval.

* Access Denied.

Result: **Passed**

### Authenticated unauthorized-role test

An active Tantou Editor account logged in successfully and opened:

```text

/admin/user-approval

```

Result:

```text

/access-denied?ReturnUrl=%2Fadmin%2Fuser-approval

```

The dedicated Access Denied page displayed:

```text

Access Denied

You do not have permission to access this page

```

The authenticated user was not redirected to Login or Pending Approval.

Result: **Passed**

### Pending password-login test

A username/password account with status:

`PENDING\_APPROVAL`

attempted to log in.

Result:

```text

/pending-approval

```

The dedicated Pending Approval page displayed:

```text

Pending Approval

Account Request Submitted

```

The account was not signed in and was not redirected to Access Denied.

Result: **Passed**

### Pending Google-login test

A Google account linked to a MangaFlow user with status:

`PENDING\_APPROVAL`

attempted to log in.

Result:

```text

/pending-approval

```

The account was not treated as an unknown user and was not allowed to enter a dashboard.

Result: **Passed**

### Active valid-role test

An active Mangaka account logged in successfully.

Result:

```text

/mangaka

```

The correct Mangaka dashboard was displayed.

Result: **Passed**

### Active wrong-role test

An active Tantou Editor account successfully accessed its own Editor Dashboard:

```text

/editor

```

The same account was denied access to the Admin User Approval page and redirected to:

```text

/access-denied

```

This confirmed that normal role access remained functional while cross-role access was blocked.

Result: **Passed**

## Verified Routing Matrix

| Situation                                  | Expected destination           | Result      |

| ------------------------------------------ | ------------------------------ | ----------- |

| Anonymous user accesses protected page     | `/login`                       | Passed      |

| Active user accesses page for another role | `/access-denied`               | Passed      |

| Pending account uses password login        | `/pending-approval`            | Passed      |

| Pending account uses Google login          | `/pending-approval`            | Passed      |

| Disabled account attempts login            | Login disabled-account message | Implemented |

| Rejected account attempts login            | Login rejected-account message | Implemented |

| Active user with valid role                | Role dashboard                 | Passed      |

| Unknown authenticated role                 | `/access-denied`               | Implemented |

## Files Changed

* `src/MangaManagementSystem.Application/Services/AuthService.cs`

* `src/MangaManagementSystem.Web/Program.cs`

* `src/MangaManagementSystem.Web/Components/Routes.razor`

* `src/MangaManagementSystem.Web/Components/Pages/AccessDeniedPage.razor`

* `src/MangaManagementSystem.Web/Components/RedirectToLoginOrAccessDenied.razor`

## Detailed File Responsibilities

### `AuthService.cs`

* Reused the shared user-status validation for Google login.

* Distinguished Pending Approval, Disabled, Rejected, and Active users.

* Removed the generic inactive-account result.

### `Program.cs`

* Changed `AccessDeniedPath` to `/access-denied`.

* Configured Cookie authentication as the default challenge scheme.

* Redirected pending password logins to `/pending-approval`.

* Redirected pending Google logins to `/pending-approval`.

* Preserved explicit Google authentication.

* Redirected unsupported authenticated roles to `/access-denied`.

### `Routes.razor`

* Removed the generic NotAuthorized message.

* Added authentication-aware redirection.

### `AccessDeniedPage.razor`

* Added a dedicated authorization failure page.

* Clearly explains that the account is authenticated but not permitted to use the requested function.

### `RedirectToLoginOrAccessDenied.razor`

* Reads the current authentication state.

* Redirects anonymous users to Login.

* Redirects authenticated unauthorized users to Access Denied.

## Security Notes

* Pending Approval is an account-lifecycle state.

* Access Denied is an authorization result.

* Login is required when no authenticated identity exists.

* These three conditions now use separate pages and redirect rules.

* Google login is invoked explicitly and is no longer the default challenge for every anonymous authorization failure.

* No secret, password, Google credential, or authentication token is stored in this document.

* Existing `ReturnUrl` query parameters are generated by Cookie authentication and are expected behavior.

## Current Phase 2 Progress

Completed:

* Item 25: Protect Profile and Portfolio endpoints.

* Item 26: Remove the Portfolio debug endpoint.

* Item 27: Separate Pending Approval and Access Denied.

## Remaining Phase 2 Work

The following items remain for subsequent work:

* Consolidate the logout flow and enforce anti-forgery protection.

* Prevent OAuth exception messages and stack traces from being returned outside Development.

* Move Board Poll functionality out of the Admin area.

* Remove Admin from Board Decision authorization where required.

* Add actor permission checks to file soft-delete operations.

* Review the remaining role-based authorization boundaries.

* Complete Phase 2 documentation and final regression testing.
