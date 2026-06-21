# Phase 7 — Documentation and Auth Test Alignment

**Date:** 2026-06-21  
**Branch:** `feature/phase5-admin-account-management`  
**PR:** `https://github.com/Quangthm/MangaManagementSystem/pull/66`  
**Latest known commit before Phase 7:** `f515632 Add Cloudinary cleanup for deleted files`  
**Scope:** Documentation and test scripts only

---

## Phase 7 Scope

Phase 7 covers:

1. Update Business Flows, Functional Requirements, User Stories, and UI Specification.
2. Synchronize `REJECTED` behavior and GUID guides.
3. Create or update Proc Test Script.
4. Create Auth Workflow Test Script.
5. Test that direct requests cannot create an active Admin or bypass internal auth boundary.
6. Test authorization by typing protected URLs directly.

---

## Leader Alignment

Leader requirement applied:

```text
C# Application: business decision + orchestration.
Do not rely on database as the main business decision layer.
Database should be called for insert/update/link persistence and audit/backstop behavior.
```

Documentation and test wording was aligned to this split:

| Layer | Phase 7 wording |
|---|---|
| C# Application | Owns business decisions, role whitelist checks, actor authorization, workflow orchestration, and safe result/error mapping. |
| API | Thin HTTP boundary that resolves requests/actors and calls Application commands/queries. |
| Infrastructure | Calls SQL procedures and external services through adapters/repositories. |
| SQL Server | Persists inserts/updates/links, owns SQL transaction consistency, enforces constraints/backstops, and writes audit events. |
| Web UI | Uses typed API clients and route authorization; does not bypass API/Application for protected workflows. |

---

## Files Updated or Created

### Documentation updated by Phase 7 addendum sections

- `business-flows-use-cases.md`
- `business-rules.md`
- `context.md`
- `functional-requirements.md`
- `user-stories.md`
- `ui-spec.md`

### Guide/database documentation synchronized

- `docs/requirements/Database_Structure.md`
- `docs/requirements/Procedures_Views_Bootstrap.md`

### Test scripts created/updated

- `Proc_Test_Script.sql`
- `Auth_Workflow_Test_Script.sql`

### Revision note created

- `docs/revision/Phase7/2026-06-21-phase7-documentation-and-auth-tests.md`

---

## Main Documentation Decisions

### Public Admin Registration

- Public registration may request `Admin` if `Admin` is present in the Application public-registration whitelist.
- This does not create an active Admin directly.
- The created account remains `PENDING_APPROVAL`.
- An existing active Admin must approve the account before Admin access is available.

### REJECTED Status

- `REJECTED` is a real account status.
- Rejected users cannot log in.
- Rejected users keep username and email reserved in MVP.
- Rejection must remain audit-visible.

### Direct Request Protection

- Direct calls to internal Google Signup must fail without `X-Internal-Api-Key`.
- Direct Admin API calls must fail for missing/non-Admin actors.
- Direct URL entry must not bypass protected route/API authorization.

### GUID Synchronization

- Current business/domain IDs use `UNIQUEIDENTIFIER`.
- GUID IDs are generated with `DEFAULT NEWID()` unless a procedure must know the ID before insert.
- `audit.AuditEvent.audit_event_id` may remain the numeric audit/ledger exception.
- Test scripts use `UNIQUEIDENTIFIER` variables and avoid old `INT/BIGINT` ID assumptions.

---

## Test Script Coverage

### `Proc_Test_Script.sql`

Covers stored-procedure persistence/backstop behavior:

- `auth.usp_User_Create` creates public Admin request as `PENDING_APPROVAL`.
- Non-Admin actor cannot activate pending Admin through `auth.usp_Admin_ChangeUserStatus`.
- Active Admin can approve pending Admin.
- Reject without reason is blocked.
- Reject with reason sets status to `REJECTED`.
- Disable and activate workflow works through Admin status procedure.
- Admin self-disable/reject is blocked.
- Recent audit evidence is selected from `audit.AuditEvent`.

### `Auth_Workflow_Test_Script.sql`

Provides SQL evidence queries and manual HTTP/UI test checklist:

- Current status distribution by role.
- Pending/rejected/disabled Admin account evidence.
- Recent auth/admin audit records.
- Direct Google Signup endpoint without internal API key should return `401 Unauthorized`.
- Public Admin registration should create pending account only.
- Active Admin approval should activate account.
- Rejected account should not login.
- Direct URL and direct Admin API access should be blocked for unauthorized actors.

---

## Build/Test Status

| Check | Status |
|---|---|
| Documentation update | Prepared |
| Proc test script | Prepared |
| Auth workflow test script | Prepared |
| C# code changes | Not performed |
| SQL schema/procedure changes | Not performed |
| UI logic changes | Not performed |
| Build result | Not run in this documentation-only step |
| SQL script runtime result | Must be run locally against `MangaManagementDB` |
| Browser/direct URL test result | Must be run manually in Web/API runtime |

---

## Remaining Work

- Run `Proc_Test_Script.sql` against local `MangaManagementDB`.
- Run `Auth_Workflow_Test_Script.sql` evidence queries.
- Manually verify direct URL authorization in browser.
- Manually verify direct internal Google Signup endpoint returns `401 Unauthorized` without internal key.
- Record actual local test results after running them.

---

## Out of Scope / Not Changed

- No C# logic was changed.
- No SQL schema was changed.
- No stored procedure definition was changed.
- No UI component logic was changed.
- No feature was added.
- No Git commit/push/merge/squash/force-push was performed.
