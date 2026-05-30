# Procedures, Views, and Bootstrap Summary

Source: MangaManagementSystem_Procedures_Views_Bootstrap.sql

This document summarizes stored procedures, key views, and the bootstrap routine present in the SQL script.

## Procedures (selected)

- audit.usp_AuditEvent_Append(actor_user_id, actor_role_name, action_code, entity_type, entity_id, detail_json, audit_event_id OUTPUT, chain_hash OUTPUT)
  - Appends an AuditEvent row and creates a SHA256 hash-chain link in audit.AuditHashChain.
  - Validates detail_json is JSON when supplied.
  - Uses sp_getapplock to serialize chain appends and writes both audit event and chain link in a transaction.

- dbo.Role_Assign(target_user_id, role_name, assigned_by_user_id, assigned_by_role_name, audit_action)
  - Assigns a role to a user by inserting dbo.UserRole and appends an audit event.
  - Prevents duplicate active assignment.

- dbo.Role_Revoke(target_user_id, role_name, revoked_by_user_id, revoked_by_role_name)
  - Revokes the most recent active role assignment row for the user.
  - Prevents revoking ADMIN role by policy.
  - Appends audit event.

- dbo.User_UpdateStatus(target_user_id, new_status, changed_by_user_id, changed_by_role_name)
  - Updates user.status with guards (prevents changing ADMIN accounts, checks existence, reports no-op)
  - Appends audit event.

- dbo.User_Create(username, password_hash, status, created_by_user_id, created_by_role_name, new_user_id OUTPUT, audit_action)
  - Creates a user, returns new_user_id, and appends audit event.
  - Handles unique username errors and includes effective status in audit diff (excludes password_hash).

- dbo.RolePerm_Grant_Batch(role_name, perm_list_json, granted_by_user_id, granted_by_role_name, audit_action)
  - Parses JSON array of permissions and inserts missing RolePerm rows.
  - Idempotent for already-granted permissions.
  - Appends audit event summarizing inserted_count.

- auth.Login_GetAccount(login_identifier)
  - Returns user account row by username or email
  - Auto-unlocks expired temporary locks

- auth.Login_RecordSuccess(user_id, actor_role_name, result_code OUTPUT)
  - Resets failed login state, updates last_login_at, appends audit event, returns result code

- auth.Login_RecordFailure(user_id, actor_role_name, result_code OUTPUT, remaining_attempts OUTPUT, locked_until OUTPUT)
  - Increments failed login attempts, applies temporary lockout after threshold (5 -> 15 minutes), appends audit events

- auth.User_ResetPassword(target_user_id, new_password_hash, actor_user_id, actor_role_name, reset_mode, reset_reason)
  - Validates reset_mode and password hash length, updates password and clears failed login state (LOCKED -> ACTIVE), appends audit

- auth.Admin_CreateApprovedUser(...)
  - Creates an active user and assigns a role, audited

- auth.Admin_ApproveRegistrationRequest(admin_user_id, admin_role_name_snapshot, registration_request_id, review_note)
  - Approves a pending user registration request, activates account, assigns requested role (if missing), appends audit

- auth.Admin_RejectRegistrationRequest(...)
  - Rejects a registration request and disables the user, appends audit

- auth.Admin_DisableUser(admin_user_id, admin_role_name_snapshot, target_user_id, disable_reason)
  - Disables a user (admin cannot disable themselves), appends audit

- auth.Admin_LockUser(admin_user_id, admin_role_name_snapshot, target_user_id, lock_minutes, lock_reason)
  - Manually locks a user for lock_minutes, appends audit

- auth.Admin_UnlockUser(...)
  - Unlocks a locked user, appends audit

- dbo.Bootstrap_Init()
  - One-time bootstrap routine: creates deterministic system users (anonymous=1, SYSTEM=2), seeds roles, creates admin user, seeds permissions, assigns role perms, and performs various device/profile seeds.
  - Many actions are audited via Audit_Append.
  - Contains a sentinel check to avoid re-applying (exists SYSTEM user guard).

## Views
- manga.vw_SeriesBoardPollVoteSummary
  - Aggregates SeriesBoardPoll and SeriesBoardVote to compute approve/reject/abstain counts, total votes, and a computed_result_code field.

## Notes
- Most mutating procedures append cryptographically chained audit events (audit.usp_AuditEvent_Append). The audit chain uses SHA-256 and enforces serialization with sp_getapplock.
- Bootstrap_Init orchestrates seeding and uses several helper procedures (RolePerm_Grant_Batch, User_Create, Device_Create, Calibration_Create, PourProfile_Create).

---
Generated from MangaManagementSystem_Procedures_Views_Bootstrap.sql
