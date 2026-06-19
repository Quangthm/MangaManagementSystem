# Phase 4 Register, Login, and Logout Completion

**Date:** 2026-06-18

**Branch:** `feature/phase4-auth-completion`

**Status:** Implementation completed; runtime verification pending

## Leader Decisions Preserved

* Public registration keeps all six roles, including Admin.
* Google Signup uses the role selected in the Register UI.
* Email/password registration requires OTP before account creation.
* Google Signup skips the extra OTP step.
* New accounts remain `PENDING_APPROVAL` until an Admin approves them.
* Rejected accounts may be approved again and disabled accounts may be activated again by Admin workflows.

## Work Completed

### Registration and OTP

* Preserved the existing six-role registration decision.
* Preserved Google Signup role propagation.
* Removed the obsolete second email-verification OTP flow that ran after account creation.
* Kept the active registration OTP flow that verifies email before creating the pending account.

### Server-side Portfolio Validation

* Added a 10 MB server-side limit.
* Validates safe file name, extension, MIME type, and file signature.
* DOCX validation checks the ZIP package for required Word document entries.
* Supported formats remain PDF, DOCX, PNG, JPG/JPEG, and WEBP.
* Validation runs before the registration OTP is consumed.

### Pending Approval

* Email registration now redirects to `/pending-approval` after OTP completion.
* Google Signup and pending login already use the same page.
* Updated the page to explain selected-role review rather than role assignment.
* Removed the corrupted icon text by using a MudBlazor icon.

### Forgot Password and Token Reset

* Added public Forgot Password and Reset Password pages.
* Added Web-to-API password-reset client and API controller.
* Added CQRS commands and handlers.
* Reset requests always return a generic response to avoid account enumeration.
* Reset tokens are cryptographically random, stored only as SHA-256 hashes, expire after 30 minutes, and are single-use.
* Issuing a new token invalidates the previous token for that user.
* Password writes use `auth.usp_User_ResetPassword` with `TOKEN_RESET` and preserve account status.
* The existing stored procedure records `PASSWORD_RESET_BY_TOKEN` audit events.

## Scope Note

The token store follows the project's current MVP single-host cache approach, matching registration and profile OTP storage. Restarting the host invalidates outstanding reset links. A distributed token store can replace the interface later without changing the UI, API, or CQRS boundaries.

## Existing Flows Verified by Static Checks

* Google Login status handling remains standardized for ACTIVE, PENDING_APPROVAL, REJECTED, and DISABLED accounts.
* Logout remains POST-only, requires antiforgery validation, clears the authentication cookie, and redirects to Login.

## Remaining Runtime Verification

* Email registration with OTP and Pending Approval redirect.
* Google Signup role preservation without OTP.
* Valid and invalid portfolio uploads.
* Forgot Password generic response for existing and unknown email addresses.
* Valid, expired/invalid, and reused password-reset tokens.
* Password reset audit event and unchanged account status.
* Google Login status matrix and cookie-clearing Logout flow.
