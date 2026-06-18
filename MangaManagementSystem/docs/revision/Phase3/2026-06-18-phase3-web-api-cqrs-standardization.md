# Phase 3 Web-to-API and CQRS Standardization

**Date:** 2026-06-18

**Branch:** `feature/phase3-web-api-standardization`

**Status:** Implemented and solution build verified; database migration and runtime regression tests remain

## Scope

This change set implements Phase 3 items 33-37 as one integrated Web-to-API standardization package.

## Item 33 - Admin User API Boundary

Added:

* `IAdminUserApiClient`
* `AdminUserApiClient`
* `AdminUsersController`
* Admin user API contracts and structured error codes
* Admin user CQRS commands, queries, and handlers

The Admin API resolves the actor from authenticated Web claims forwarded through protected internal headers. The actor id is not accepted from Razor page input.

## Item 34 - Admin Pages Migrated from Direct IUserService

The following pages now use `IAdminUserApiClient`:

* `AdminDashboard.razor`
* `RegistrationRequests.razor`
* `UserAccounts.razor`
* `UserApproval.razor`

The User Approval page also loads portfolio metadata through the Admin API rather than directly injecting `IFileResourceService`.

## Item 35 - Google Authentication Boundary

Google OAuth remains owned by the Web host because it owns the authentication cookie and external challenge.

After Google returns the external identity, application-user resolution now goes through:

`Web callback -> IAuthApiClient -> AuthController -> MediatR query -> IAuthService -> repository`

Google sign-up continues through:

`Web callback -> IAuthApiClient -> AuthController -> MediatR command -> IAuthService -> stored procedure`

The Web Google callback no longer calls `IAuthService` directly.

## Item 36 - Standard Authentication Error Codes

Added stable auth error codes such as:

* `invalid_credentials`
* `account_pending`
* `account_rejected`
* `account_disabled`
* `account_not_found`
* `google_email_missing`
* `google_oauth_failed`
* `google_signup_failed`
* `invalid_role`
* `invalid_otp`

API errors now use a structured `{ code, message }` response. Web redirects and pages branch on the stable code rather than inspecting free-text exception messages.

## Item 37 - CQRS, EF Reads, Stored-Procedure Writes and Audit

Added CQRS queries for:

* Password login
* Google login application-user resolution
* Admin user listing
* Admin dashboard status counts
* Admin portfolio metadata

Added CQRS commands for:

* Registration completion
* Approve user
* Reject user
* Disable user
* Activate user

Admin and authentication queries use EF Core read methods. User workflow writes continue through existing stored procedures, including `auth.usp_Admin_ChangeUserStatus` and user creation/update procedures.

Audit writes were removed from EF `AddAsync` / `SaveChangesAsync` paths. `AuditEventService` and profile audit now use `audit.usp_AuditEvent_Append` through `IAuditEventRepository`.

The audit procedure now returns the inserted audit id through an optional output parameter. Existing stored procedure callers remain compatible.

## Database Migration

Run:

`docs/revision/Phase3/2026-06-18-phase3-audit-event-procedure.sql`

against `MangaManagementDB` before runtime testing audit writes.

## Validation Completed

* The implementation script verified the expected Phase 3 branch.
* No direct `IUserService` usage remains in the migrated Admin pages.
* Google login resolution no longer calls `IAuthService` directly from the Web callback.
* No direct EF audit insert remains in `AuditEventService` or `UserService`.
* `git diff --check` passed.
* Full solution build succeeded with zero compilation errors.

## Runtime Tests Remaining

* Admin dashboard counts load through the Admin API.
* Pending users can be approved and rejected.
* Active users can be disabled.
* Admin cannot disable their own account.
* Admin portfolio preview metadata loads through the Admin API.
* Password login returns the expected standardized codes.
* Google login covers active, pending, rejected, disabled, missing-email, and missing-account flows.
* Google sign-up covers new, pending, active, rejected, and disabled accounts.
* Profile audit and Audit Event creation succeed after applying the database procedure migration.

## Scope Note

The EF-read/stored-procedure-write rule is applied to the Admin, authentication, registration, profile-audit, and audit workflows changed by Phase 3. Unrelated legacy CRUD services were not rewritten as part of this phase.

## Runtime Verification

The following runtime regression tests were completed locally.

### Build

* Full solution build succeeded.
* Compilation errors: 0.
* Existing warnings: 58.

### Database

* Executed `2026-06-18-phase3-audit-event-procedure.sql` against `MangaManagementDB`.
* Verified stored procedure `audit.usp_AuditEvent_Append` exists.
* Verified the procedure was updated successfully on 2026-06-18.

### Admin read workflows

* Admin Dashboard loaded account statistics successfully.
* User Accounts loaded all account records through the Admin Web-to-API boundary.
* User Approval loaded pending accounts successfully.
* Admin Razor pages no longer inject or call `IUserService` directly.

### Admin write and audit workflows

Test account:

* Display name: `3P`
* Username: `Phanphonphon`
* Role: `Assistant`

Verified transitions:

1. `PENDING_APPROVAL` to `ACTIVE`
   * UI displayed the successful approval message.
   * User disappeared from the pending approval list.
   * User Accounts displayed the new `ACTIVE` status.
   * Audit event ID: `149`.
   * Audit action: `USER_STATUS_CHANGED`.
   * Audit old status: `PENDING_APPROVAL`.
   * Audit new status: `ACTIVE`.

2. `ACTIVE` to `DISABLED`
   * Confirmation dialog displayed correctly.
   * UI displayed the successful disable message.
   * User Accounts displayed the new `DISABLED` status.
   * Audit event ID: `150`.
   * Audit action: `USER_STATUS_CHANGED`.
   * Audit old status: `ACTIVE`.
   * Audit new status: `DISABLED`.

The runtime test confirms the write flow:

`Admin UI -> AdminUserApiClient -> AdminUsersController -> CQRS Command -> stored procedure status transition -> stored procedure audit append`

### Authentication error codes

* Invalid password redirected with `error=invalid_credentials`.
* Login UI displayed a safe generic credentials message.
* Failed Google OAuth redirected with `error=google_oauth_failed`.
* Login UI displayed a safe generic Google sign-in message.
* No exception, stack trace, SQL error, class name, or internal implementation detail was exposed.

### UI correction

* Fixed the corrupted `Back to Home` text on the Login page.
* Rebuilt and verified the corrected text in the browser.

## Known Follow-up

* The test account `Phanphonphon` remains `DISABLED` after runtime verification.
* The current User Accounts UI does not provide an Activate action for disabled accounts.
* This is an existing UI capability gap and does not block the Phase 3 Web-to-API and CQRS standardization scope.
