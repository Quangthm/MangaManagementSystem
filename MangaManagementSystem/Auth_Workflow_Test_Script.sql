USE MangaManagementDB;
GO

/* ============================================================
   MangaManagementSystem - Phase 7 Auth Workflow Test Script
   File: Auth_Workflow_Test_Script.sql
   Scope: Auth workflow verification checklist + SQL evidence queries
   Date: 2026-06-21

   IMPORTANT:
   - This script contains SQL evidence checks and manual HTTP/UI test steps.
   - Direct URL and direct API tests must be executed in browser/Postman/curl because
     SQL Server cannot verify Blazor route guards or HTTP headers.
   - Do not store real passwords, OTPs, internal API keys, or OAuth tokens in this file.
   ============================================================ */

SET NOCOUNT ON;

PRINT N'Phase 7 Auth Workflow Test Script started.';

/* ------------------------------------------------------------
   A. SQL evidence: current account status distribution
   ------------------------------------------------------------ */

SELECT
    u.status_code,
    r.role_name,
    COUNT(*) AS user_count
FROM auth.Users u
INNER JOIN auth.Roles r
    ON r.role_id = u.role_id
GROUP BY
    u.status_code,
    r.role_name
ORDER BY
    r.role_name,
    u.status_code;

/* ------------------------------------------------------------
   B. SQL evidence: pending/rejected/disabled Admin accounts
      Expected:
      - Pending Admin accounts may exist from public registration.
      - Pending Admin accounts are not active.
      - Rejected/disabled accounts remain stored for traceability.
   ------------------------------------------------------------ */

SELECT TOP (50)
    u.user_id,
    u.username,
    u.email,
    u.display_name,
    r.role_name,
    u.status_code,
    u.created_at_utc
FROM auth.Users u
INNER JOIN auth.Roles r
    ON r.role_id = u.role_id
WHERE r.role_name = N'Admin'
ORDER BY u.created_at_utc DESC;

/* ------------------------------------------------------------
   C. SQL evidence: recent auth/admin audit records
   ------------------------------------------------------------ */

SELECT TOP (100)
    ae.occurred_at_utc,
    ae.actor_user_id,
    ae.actor_role_name,
    ae.action_code,
    ae.entity_type,
    ae.entity_id,
    ae.detail_json
FROM audit.AuditEvent ae
WHERE ae.action_code IN
(
    N'USER_REGISTERED',
    N'USER_STATUS_CHANGED',
    N'USER_DISPLAY_NAME_UPDATED',
    N'USER_PASSWORD_RESET',
    N'USER_PASSWORD_CHANGED'
)
ORDER BY ae.occurred_at_utc DESC;

/* ------------------------------------------------------------
   D. Manual HTTP/API tests to run outside SQL Server
   ------------------------------------------------------------

   D1. Direct internal Google Signup endpoint cannot create account

   Request:
   POST {ApiBaseUrl}/api/auth/google-signup
   Header: do NOT send X-Internal-Api-Key
   Body example:
   {
     "email": "phase7.direct.google@example.test",
     "googleDisplayName": "Phase7 Direct Google",
     "roleName": "Admin"
   }

   Expected:
   - HTTP 401 Unauthorized
   - No new auth.Users row for phase7.direct.google@example.test
   - No Admin account is created by this direct request

   D2. Public Admin registration creates pending account only

   Steps:
   1. Open Register page.
   2. Select role: Admin.
   3. Complete OTP registration with a test email.
   4. Query auth.Users for the created account.

   Expected:
   - role_name = Admin
   - status_code = PENDING_APPROVAL
   - user cannot login before Admin approval
   - user cannot access /admin or Admin API before approval

   D3. Existing active Admin approves pending Admin

   Steps:
   1. Login as an existing active Admin.
   2. Open Admin User Accounts/User Approval.
   3. Approve the pending Admin account.
   4. Query auth.Users or refresh UI.

   Expected:
   - status_code changes from PENDING_APPROVAL to ACTIVE
   - audit contains USER_STATUS_CHANGED
   - newly active Admin can login and access Admin dashboard

   D4. Rejected account cannot login

   Steps:
   1. Create/register a user.
   2. Login as active Admin.
   3. Reject the user with a reason.
   4. Attempt login as the rejected user.

   Expected:
   - status_code = REJECTED
   - login blocked
   - username/email remain reserved
   - audit contains USER_STATUS_CHANGED

   D5. Direct Admin URL authorization

   Test actors:
   - anonymous user
   - pending user
   - rejected user
   - disabled user
   - active non-Admin user
   - active Admin user

   Routes:
   - /admin
   - /admin/users
   - /admin/files
   - /admin/audit-logs

   Expected:
   - anonymous -> login/unauthorized
   - pending -> pending approval/access blocked
   - rejected/disabled -> cannot login/access
   - active non-Admin -> access denied/forbidden
   - active Admin -> allowed

   D6. Direct Admin API authorization

   Endpoints to verify with missing/non-Admin actor:
   - GET  {ApiBaseUrl}/api/admin/users
   - GET  {ApiBaseUrl}/api/admin/users/search
   - POST {ApiBaseUrl}/api/admin/users/{userId}/approve
   - POST {ApiBaseUrl}/api/admin/users/{userId}/reject
   - POST {ApiBaseUrl}/api/admin/users/{userId}/disable
   - POST {ApiBaseUrl}/api/admin/files/{fileResourceId}/cleanup

   Expected:
   - missing/invalid auth -> 401 or 403
   - active non-Admin -> 403/access denied
   - active Admin -> allowed when request is otherwise valid

   Notes:
   - Do not paste real JWTs, cookies, OTPs, or internal API keys into committed files.
   - Store screenshots or manual results in docs/revision if leader asks for evidence.
   ------------------------------------------------------------ */

PRINT N'Phase 7 Auth Workflow Test Script completed. Review manual HTTP/UI checklist above.';
GO
