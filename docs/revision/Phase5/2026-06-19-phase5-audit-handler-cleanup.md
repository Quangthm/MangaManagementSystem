# Phase 5 Audit Handler Cleanup

Date: 2026-06-19

## Review finding

Phase 5 still injected `IAuditEventRepository` into `AdminUserCommandHandlers` and called `AppendAsync` directly after sending an administrator password-reset link.

Phase 4 review cleanup converted the audit repository to read-only because audit writes are owned by database stored procedures.

## Correction

- Removed the `IAuditEventRepository` dependency from `AdminUserCommandHandlers`.
- Removed the direct `ADMIN_PASSWORD_RESET_LINK_SENT` Audit append.
- Retained the read-only Audit query methods used by administrator Audit views.
- Did not restore `@audit_event_id OUTPUT` or `SCOPE_IDENTITY()`.

## Architecture note

Application code must not call `audit.usp_AuditEvent_Append` directly.

A password-reset email dispatch is not persisted as a separate Audit event by this cleanup. If that event is required later, it must be implemented through a dedicated database-owned business procedure rather than by restoring a direct application-layer Audit append.

## Verification

- No direct C# Audit write remains.
- Solution build and automated-test command must pass before commit.
