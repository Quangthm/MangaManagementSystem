# 2026-06-28 — Admin API + JWT Authorization Flow

## Scope

Refactor admin-related Web flows so admin pages use API clients instead of calling application services directly. API controllers are protected with JWT/role authorization.

## Completed Changes

### Admin User / User Approval

- Refactored User Approval page to use `IAdminUserApiClient`.
- Removed direct service usage from the User Approval page flow.
- `AdminUsersController` now uses `[Authorize(Roles = "Admin")]`.
- Removed obsolete internal admin authorization header code from `AdminUsersController`.

### Admin Audit

- Registered `IAdminAuditApiClient` with `ApiAuthorizationMessageHandler`.
- `AdminAuditEventsController` now uses `[Authorize(Roles = "Admin")]`.
- Removed obsolete internal admin authorization header code from `AdminAuditEventsController`.

### Admin File Management

- Registered `IAdminFileApiClient` with `ApiAuthorizationMessageHandler`.
- `AdminFilesController` now uses `[Authorize(Roles = "Admin")]`.
- Removed obsolete internal admin authorization header code from `AdminFilesController`.
- Preserved existing file preview, download, delete, and cleanup flow.
- Verified manually that file preview opens inline in the File Management modal without navigating to another browser tab.

### Login / Session / JWT

- Login form posts to Web endpoint `/api/auth/login`.
- Web endpoint calls `IAuthApiClient.LoginAsync`.
- `AuthApiClient` calls API endpoint `api/auth/login`.
- API `AuthController` generates a JWT access token.
- Web stores the JWT access token in the authenticated cookie/session claim `api_access_token`.
- Admin API clients attach the JWT Bearer token through `ApiAuthorizationMessageHandler`.

### Logout

- Logout remains Web-session based.
- `/api/auth/logout` validates antiforgery, clears the Web authentication cookie/session, and redirects to `/login`.
- No backend token revocation endpoint was added because the current JWT design is stateless and no token blacklist/revocation store exists in the current task scope.

## Commits

- `8fea01a` — Refactor user approval page to use admin API client
- `e80fb9d` — Use JWT authorization for admin user API flow
- `d85ae61` — Use JWT authorization for admin audit API flow
- `c6d99ac` — Use JWT authorization for admin file API flow
- `1522587` — Remove obsolete internal admin authorization code

## Verification

Build command:

`dotnet build .\MangaManagementSystem.slnx`

Result:

- Build succeeded.
- No build errors.
- Existing warnings remain in unrelated Infrastructure/Web files.

Manual browser smoke check:

- Admin Dashboard opens.
- Profile Settings opens.
- User Management opens and lists users.
- Audit Logs opens and lists audit events.
- File Management opens and lists files.
- File preview opens inline in the page modal.
- User Approval opens.

## Out of Scope Note

Board Polls route mismatch was found during browser smoke check:

- Admin layout links to `/admin/board-polls`.
- `BoardPollsPage.razor` declares `@page "/demo/mangaflow/polls"`.

Git blame showed:

- `/admin/board-polls` link came from commit `7750e354`.
- `/demo/mangaflow/polls` route came from commit `b0709b33`.

This was not changed by the admin JWT/API refactor commits above. No fix was made because it is outside the current task scope unless requested separately.

## Current Branch

`feature/phase7-file-cleanup-integrated`

Only `../tem` remains untracked and was intentionally not committed.
