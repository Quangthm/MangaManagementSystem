# Leader Follow-up Fixes - Admin Audit, Search, Auth Token

Date: 2026-07-06
Branch: feature/phase7-file-cleanup-integrated

## Scope

This revision documents leader follow-up fixes for Admin Audit, Admin User Management search, and Auth token handling.

Covered items:

1. Normalize audit EntityId GUID handling.
2. Fix Admin User Management search Enter behavior.
3. Remove hard-coded 14-day token expiration from auth flow.
4. Move JWT generation logic out of AuthController.
5. Add Admin business flowchart.

Out of scope:

- No database schema change for AuditEvent.EntityId.
- No migration was added.
- No database records are deleted.
- No unrelated Admin UI or business flow changes are included.

## 1. Audit EntityId normalization

Audit user filtering no longer queries both D and N GUID string formats.
Audit write paths now store GUID entity IDs using canonical D format.

AuditEvent.EntityId remains string because changing it to Guid is a database schema and API contract change.
That should be handled as a separate migration task after checking existing audit data.

Suggested DB check before a future Guid migration:

```sql
SELECT TOP 50 entity_type, entity_id
FROM audit.AuditEvent
WHERE entity_id IS NOT NULL
  AND TRY_CONVERT(uniqueidentifier, entity_id) IS NULL;
```

## 2. Admin User Management search Enter

The Admin User Management search field now handles Enter key presses.
The Enter handler reuses ApplyFiltersAsync, so it stays aligned with the existing Search button behavior.

Changed file:

- MangaManagementSystem.Web/Components/Pages/Admin/UserAccounts.razor

## 3. Auth token expiration and JWT generation

AuthController no longer owns JWT generation logic.

JWT generation was moved to:

- MangaManagementSystem.API/Services/IJwtTokenService.cs
- MangaManagementSystem.API/Services/JwtTokenService.cs

Token lifetime is now read from Jwt:ExpireMinutes instead of DateTime.UtcNow.AddDays(14).
The Web login flow uses ExpiresAtUtc from the API login response.
AuthenticationSessionOptions was added for the legacy CustomAuthenticationStateProvider fallback path.

## 4. Admin business flowchart

```mermaid
flowchart TD
    A[Admin signs in] --> B[Admin Dashboard]
    B --> C[User Management]
    B --> D[Registration Requests]
    B --> E[Audit Logs]
    B --> F[File Management]
    C --> C1[Search users by username, email, or display name]
    C1 --> C2[Apply status and role filters]
    C2 --> C3[Review user list]
    C3 --> C4[Open user detail]
    C3 --> C5{Account action}
    C5 -->|Pending approval| C6[Approve user]
    C5 -->|Pending approval| C7[Reject user with reason]
    C5 -->|Rejected| C8[Approve again]
    C5 -->|Active| C9[Disable user with reason]
    C5 -->|Disabled| C10[Activate user]
    C6 --> C11[Write audit event]
    C7 --> C11
    C8 --> C11
    C9 --> C11
    C10 --> C11
    D --> D1[View pending users]
    D1 --> D2[Review portfolio if available]
    D2 --> D3{Registration decision}
    D3 -->|Approve| D4[Activate account]
    D3 -->|Reject| D5[Reject with reason]
    D4 --> C11
    D5 --> C11
    E --> E1[Search audit events]
    E1 --> E2[Filter by actor, action, entity, and time]
    E2 --> E3[Review activity trail]
    F --> F1[Search file resources]
    F1 --> F2[Review deleted or candidate files]
    F2 --> F3{Cleanup action}
    F3 -->|Single cleanup| F4[Cleanup selected file storage]
    F3 -->|Cleanup all candidates| F5[Cleanup candidate files]
    F4 --> F6[Update cleanup result]
    F5 --> F6
    F6 --> C11
```

## Validation

- Build succeeded after AuditEvent normalization.
- Build succeeded after Admin Search Enter fix.
- Build succeeded after Auth token refactor.
- No AddDays(14) remains in auth/token files.
- GenerateJwtToken no longer exists in AuthController.
- git diff --check did not report whitespace errors.

Existing compiler and analyzer warnings are outside this follow-up scope.
