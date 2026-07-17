# Current Session — Create Chapter Page Task Eligibility

**Date:** 2026-07-16
**Branch:** `feature/Mangaka`
**Status:** Implemented; static verification only

## Goal

Enforce production-eligible series and task-creation-eligible chapter states for both single-task and Quick Select `ChapterPageTask` creation. Replace only the single-task `manga.usp_ChapterPageTask_Create` call with EF Core while preserving its required audit behavior.

## Production files changed

- `MangaManagementSystem/src/MangaManagementSystem.Domain/Policies/ChapterPageTaskCreationPolicy.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Services/ChapterPageTaskService.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Repositories/ChapterPageTaskRepository.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Repositories/QuickSelectRepository.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/MangakaTaskController.cs`

## Result

- `ChapterPageTaskCreationPolicy` permits task creation only for `DRAFT` and `REVISION_REQUESTED` chapters.
- Both single-task and Quick Select writes reuse `SeriesProductionPolicy` and the new chapter policy inside `RepeatableRead` transactions.
- Both paths authoritatively validate active Mangaka creator and active Assistant assignee roles and series memberships.
- Single-task creation now uses EF Core for context lookup, task insertion, and region links.
- `audit.usp_AuditEvent_Append` remains inside the active EF transaction, preserving role snapshot resolution, audit locking, and atomic rollback.
- Quick Select retains its app lock, FULL_PAGE-region behavior, batch atomicity, and existing EF audit behavior.
- Reload and notification remain after the single-task commit.

## Constraint ownership

Application validation is limited to workflow inputs: nonempty IDs, nonempty effective region list, required normalized title/description, and existing null due-date/compensation defaults. Task type, priority, compensation range/precision, maximum lengths, FKs, status checks, and junction uniqueness remain database-owned.

Only confirmed SQL constraint/conflict numbers are converted to a safe client message. Unexpected `DbUpdateException` failures are logged and rethrown for the generic 500 response.

## Protected scope

No Creator Workspace, Quick Select UI/service/DTO/API/client, task DTO/interface, SQL, schema, migration, `SeriesProductionPolicy`, Create Chapter, or unrelated task-lifecycle file was changed.

## Verification boundary

Only static checks are authorized. Restore, build, test, run, servers, watchers, migrations, and SQL execution were not run.

Completed static checks:

- `git diff --check` passed; only working-copy LF/CRLF notices were emitted.
- Source search found no application call to `manga.usp_ChapterPageTask_Create`; only its unchanged bootstrap definition remains.
- Both task-writing repositories call both policies inside `RepeatableRead` transactions.
- The single-task audit command is attached to the current EF transaction.
- Quick Select still calls its existing application-lock helper.
- Protected workspace, DTO, interface, SQL, schema, migration, and configuration paths have no diff.

## Final handoff

`docs/revision/Mangaka/2026-07-16-create-chapter-page-task-eligibility.md`
