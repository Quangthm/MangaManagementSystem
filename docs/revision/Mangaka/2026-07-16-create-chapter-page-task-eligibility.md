# Create Chapter Page Task Eligibility Handoff

**Date:** 2026-07-16
**Branch:** `feature/Mangaka`

## Final data flows

Single task:

```text
Creator Workspace caller
→ typed Mangaka task client
→ MangakaTaskController.CreateTaskAsync
→ ChapterPageTaskService.CreateChapterPageTaskAsync
→ ChapterPageTaskRepository.CreateChapterPageTaskAsync
→ RepeatableRead EF transaction
→ task + task-region links
→ audit.usp_AuditEvent_Append in the active transaction
→ commit
→ task reload
→ post-commit notification
→ response
```

Quick Select:

```text
QuickSelectService
→ QuickSelectRepository.PersistQuickSelectAssignmentAsync
→ existing app lock
→ RepeatableRead authoritative rechecks
→ batch task/FULL_PAGE-region/audit inserts
→ commit
```

## Stored-procedure-to-EF comparison

The single-task repository no longer calls `manga.usp_ChapterPageTask_Create`. EF now preserves its region normalization, exact existence check, same-page-version rule, context derivation, task insert, region links, and returned task ID. The legacy procedure remains unchanged in bootstrap SQL.

The EF flow adds checks absent from the procedure:

- creator is an active Mangaka and active contributor of the resolved series;
- assignee is an active Assistant and active contributor of the same series;
- parent series passes `SeriesProductionPolicy`;
- owning chapter passes `ChapterPageTaskCreationPolicy`.

## Policy behavior

- Series: only `SERIALIZED` and `HIATUS` permit production.
- Chapter: only `DRAFT` and `REVISION_REQUESTED` permit a new page task.
- Null, blank, unknown, terminal, review, approved, scheduled, on-hold, and released states are blocked.

Both policies run inside both authoritative write transactions.

## Input and constraint ownership

Workflow validation covers nonempty actor/assignee IDs, an effective region list, exact context, authorization, and normalized nonblank required title/description. Null due dates retain the seven-day default and null compensation retains zero normalization.

The database remains authoritative for type codes, priority, compensation range/precision, maximum lengths, FKs, task status checks, and junction uniqueness. Confirmed task constraint/conflict SQL failures receive a safe message; unexpected `DbUpdateException` failures are logged and rethrown.

## Transaction and audit behavior

`RepeatableRead` holds successful exact-row reads for series, chapter, users, contributors, regions, pages, and versions through commit. Competing lifecycle, chapter-status, account-disable, or membership-end writes must wait. No range/phantom-dependent rule requires `Serializable`.

Single-task audit continues through `audit.usp_AuditEvent_Append` in the EF transaction. The procedure resolves the actor-role snapshot and holds its transaction-owned audit app lock. Audit failure rolls back the task and links.

Quick Select retains its existing chapter-scoped application lock and atomic EF batch/audit behavior.

## Files changed

Production:

- `Domain/Policies/ChapterPageTaskCreationPolicy.cs`
- `Application/Services/ChapterPageTaskService.cs`
- `Infrastructure/Repositories/ChapterPageTaskRepository.cs`
- `Infrastructure/Repositories/QuickSelectRepository.cs`
- `API/Controllers/Mangaka/MangakaTaskController.cs`

Documentation:

- `docs/revision/_CURRENT_SESSION.md`
- `docs/revision/Mangaka/2026-07-16-create-chapter-page-task-eligibility.md`

## Protected files

No Creator Workspace, Quick Select UI/service/DTO/API/client, task DTO/interface, SQL, schema, migration, `SeriesProductionPolicy`, Create Chapter, or unrelated lifecycle file was modified.

## Verification

Static verification only. No restore, build, test, run, server, watcher, migration, or SQL command was authorized.

Performed:

- `git diff --check`: passed, with only LF/CRLF working-copy notices.
- Changed-file and protected-file inspection: passed.
- Create-call/task-construction search: both authoritative task writers identified and guarded.
- Legacy create-procedure call search: no application call remains; bootstrap definition is unchanged.
- Policy/transaction inspection: both repositories use both policies inside `RepeatableRead`.
- Audit inspection: the single-task audit procedure uses the active EF transaction.
- Quick Select inspection: its existing app lock remains in place.

Manual checks for later execution:

- Allow both creation paths for `SERIALIZED`/`HIATUS` plus `DRAFT`/`REVISION_REQUESTED` with valid users.
- Block every other series/chapter combination.
- Block inactive, wrong-role, or ended-membership users.
- Reject empty, missing, or mixed-version regions atomically.
- Confirm normalized title/description appear in the task and audit payload.
- Confirm schema-invalid task fields return safe responses without internal details.
- Confirm audit failure rolls back single-task creation.
- Confirm Quick Select remains atomic and retains its app lock.
- Confirm single-task reload and notification happen only after commit.
