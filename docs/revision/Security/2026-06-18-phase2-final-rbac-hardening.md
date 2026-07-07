# Phase 2 Final RBAC and Security Hardening

**Date:** 2026-06-18

**Branch:** `feature/security-rbac-hardening`

**Status:** Completed and locally verified

## Scope

This session completed the remaining Phase 2 security and role-based access control items:

* Harden Google OAuth failure handling.

* Correct Board Poll authorization.

* Correct Board Decision authorization.

* Add ownership authorization for file soft-delete.

* Run final RBAC regression tests.

* Verify the complete solution build.

## 1. Google OAuth Error Hardening

The Google remote authentication failure handler previously returned technical exception details directly to the browser, including:

* Exception message.

* Stack trace.

* OAuth handshake implementation information.

The handler now:

* Records the complete exception only in the server log.

* Redirects the browser to a safe Login error URL.

* Displays a generic user-facing message.

* Does not return exception messages or stack traces.

Safe redirect:

`/login?error=google\_oauth\_failed`

Safe user-facing message:

`Google sign-in could not be completed. Please try again.`

## 2. Board Poll Authorization

Board Poll management was previously located under:

`/admin/board-polls`

and was authorized for the `Admin` role.

This conflicted with the editorial workflow, where Board Poll management belongs to the Editorial Board Chief.

The Board Poll page now uses:

`/board/polls`

Required role:

`Editorial Board Chief`

Additional changes:

* Removed Board Poll navigation from the Admin menu.

* Removed Board Poll navigation from `AdminLayout`.

* Added Board Poll navigation for Editorial Board Chief.

* Updated the Board Chief dashboard to use `/board/polls`.

* Changed the page layout from `AdminLayout` to `RankingLayout`.

## 3. Board Decision Authorization

The Board Decision page previously allowed:

* Editorial Board Member.

* Editorial Board Chief.

* Admin.

The `Admin` role was removed because administrative platform access does not automatically grant editorial board decision authority.

The page now allows only:

* `Editorial Board Member`

* `Editorial Board Chief`

## 4. File Soft-delete Authorization

The file soft-delete service previously accepted only:

* File resource ID.

* Optional deleted-by user ID.

It did not verify whether the actor owned the file or had an authorized administrative role.

The service now requires:

* File resource ID.

* Actor user ID.

* Actor role name.

Authorization rules:

* The file uploader may soft-delete their own file.

* An Admin may soft-delete a file.

* A different non-Admin user is denied.

* A missing or already deleted file returns `false`.

* The authenticated actor is recorded as `DeletedByUserId`.

Unauthorized deletion throws:

`UnauthorizedAccessException`

with a safe permission-denied message.

There is currently no endpoint or user interface calling `DeleteFileResourceAsync`. Therefore, the authorization was verified at the service and compilation level, while runtime integration testing is deferred until a delete endpoint is connected.

## Files Changed

* `src/MangaManagementSystem.Application/Interfaces/IFileResourceService.cs`

* `src/MangaManagementSystem.Application/Services/FileResourceService.cs`

* `src/MangaManagementSystem.Web/Components/Layout/NavMenu.razor`

* `src/MangaManagementSystem.Web/Components/Layouts/AdminLayout.razor`

* `src/MangaManagementSystem.Web/Components/Pages/Admin/BoardPolls.razor`

* `src/MangaManagementSystem.Web/Components/Pages/Board/BoardChiefDashboard.razor`

* `src/MangaManagementSystem.Web/Components/Pages/LoginPage.razor`

* `src/MangaManagementSystem.Web/Components/Pages/Ranking/BoardDecision.razor`

* `src/MangaManagementSystem.Web/Program.cs`

## Verification

### Build

Executed:

`dotnet build`

Result:

`Build succeeded with 0 errors.`

The reported warnings were existing warnings outside the scope of this security change.

### Static Security Verification

Confirmed that the Web project no longer contains:

* OAuth exception messages returned to the browser.

* OAuth stack traces returned to the browser.

* The legacy `/admin/board-polls` route.

* Admin authorization on the Board Decision page.

Confirmed that the code contains:

* `google\_oauth\_failed`

* `/board/polls`

* File owner/Admin delete checks.

* Safe permission-denied handling.

## RBAC Regression Results

### Admin

`/board/polls`

Result: Access Denied.

`/board-decision`

Result: Access Denied.

`/ranking`

Result: Allowed.

### Editorial Board Member

`/board/polls`

Result: Access Denied.

`/board-decision`

Result: Allowed.

### Editorial Board Chief

`/board/polls`

Result: Allowed. Board Poll Management opened successfully.

`/board-decision`

Result: Allowed. Board Decision Workspace opened successfully.

## Google OAuth Error Verification

Opened:

`/login?error=google\_oauth\_failed`

Result:

The Login page displayed the safe message:

`Google sign-in could not be completed. Please try again.`

No exception message or stack trace was displayed.

## Phase 2 Completion Summary

Completed security items:

* Protect Profile and Portfolio endpoints.

* Remove the Portfolio debug endpoint.

* Separate Pending Approval and Access Denied.

* Consolidate Logout and enforce anti-forgery protection.

* Harden Google OAuth failure handling.

* Correct Board Poll RBAC.

* Correct Board Decision RBAC.

* Add file soft-delete ownership validation.

* Complete solution build and RBAC regression verification.

## Final Status

Phase 2 security implementation is complete on:

`feature/security-rbac-hardening`

Remaining workflow steps:

* Add and verify this revision file.

* Commit the final Phase 2 changes.

* Push the branch.

* Create or update the pull request for leader review.
