# Quick Select Audit Actor Attribution Fix

Status: **runtime verified fixed**.

New Quick Select audit rows now persist both the logged-in Mangaka user ID and
the authoritative `Mangaka` role snapshot.

## Runtime history and proven findings

Initial Quick Select database checks showed incomplete task-creation actor
attribution. The source already assigned the JWT-derived actor to
`AuditEvent.ActorUserId`, so focused EF diagnostics were used to trace the
complete persistence boundary.

The investigation proved:

- `QuickSelectAssignmentPlan.ActorUserId` contained the authenticated Mangaka
  user ID;
- the constructed and tracked `AuditEvent.ActorUserId` contained that ID;
- `AddRange` did not clear or replace the actor;
- effective EF metadata mapped the writable, non-shadow property to
  `actor_user_id`;
- generated SQL included `actor_user_id`;
- the SQL actor parameter contained the same Guid;
- latest verified audit row `231` persisted `actor_user_id` correctly.

The temporary explicit `HasColumnName("actor_user_id")` change was not the root
cause. The project-wide snake-case convention already provides that mapping,
so the redundant isolated configuration was removed.

## Root cause and production fix

The remaining null attribution was `actor_role_name`. Quick Select explicitly
constructed each audit event with `ActorRoleName = null`.

Working direct-EF audit paths snapshot an authoritative role validated on the
server. Quick Select already reloads the actor and `Role` from the database
inside `RecheckGuardsAsync` and confirms:

- the actor exists;
- the account is active;
- the current role is `Mangaka`;
- the actor remains an active series contributor.

`RecheckGuardsAsync` now returns that validated current database role name.
The per-task audit initializer uses it:

```csharp
ActorUserId = plan.ActorUserId,
ActorRoleName = actorRoleName,
```

No browser/request role or actor data is accepted. No duplicate actor lookup
was added.

## Final actor flow

```text
Bearer JWT
-> IAuthenticatedActorResolver
-> controller actorUserId
-> QuickSelectService
-> QuickSelectAssignmentPlan.ActorUserId
-> repository transaction and guard recheck
-> current active auth.Users + auth.Roles Mangaka validation
-> ChapterPageTask.CreatedByUserId
-> new PageRegion.CreatedByUserId
-> AuditEvent.ActorUserId
-> AuditEvent.ActorRoleName ("Mangaka")
-> one EF SaveChangesAsync
-> transaction commit
```

`AssignedToUserId` remains the selected Assistant target and is not used as
the audit actor.

## Preserved behavior

- One `CHAPTER_PAGE_TASK_CREATED` audit event remains staged per created task.
- `EntityType`, each real task `EntityId`, and `detail_json` are unchanged.
- `page_region_ids` remains a JSON array.
- Existing `AddRange`, execution strategy, locks, guard checks, task/region
  staging, notifications, one `SaveChangesAsync`, commit, and rollback remain.
- Reused regions retain their original creator.
- Quick Select remains direct EF Core persistence.
- No stored procedure or raw SQL audit insertion was introduced.
- No request actor/role field, UI change, JWT change, or
  `X-Actor-User-Id` behavior was introduced.
- Working Chapter On Hold and series lifecycle audit paths were not changed.

## Diagnostic cleanup

The ActorUserId persistence boundary is fully proven. The following temporary
instrumentation was removed:

- Quick Select construction/change-tracker/metadata diagnostic logs;
- `QuickSelectAuditSqlDiagnosticInterceptor`;
- its Infrastructure DI registration.

Normal production logging remains.

## Files changed

- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Repositories/QuickSelectRepository.cs`
- `docs/revision/Mangaka/2026-07-24-quick-select-audit-actor-fix.md`

The temporary interceptor, DI registration, and isolated explicit mapping were
removed rather than retained as production changes.

## Optional final regression validation

The user has already runtime-verified the fix. For any final regression pass,
create one Quick Select task and run:

```sql
SELECT TOP (5)
    audit_event_id,
    actor_user_id,
    actor_role_name,
    action_code,
    entity_type,
    entity_id,
    occurred_at_utc
FROM audit.AuditEvent
WHERE action_code = N'CHAPTER_PAGE_TASK_CREATED'
ORDER BY audit_event_id DESC;
```

Expected newest row:

- `actor_user_id` is the logged-in Mangaka user ID;
- `actor_role_name` is `Mangaka`;
- `action_code` is `CHAPTER_PAGE_TASK_CREATED`;
- `entity_id` is the newly created task ID.

## Verification status

Build verification: **NOT RUN -- user will perform the build.**

Automated tests: **NOT RUN -- user will perform validation as applicable.**

Manual black-box functional testing:
**NOT RUN during final cleanup -- user already verified the fix and will
perform any final regression validation as needed.**

No commit or push was performed during final cleanup.
