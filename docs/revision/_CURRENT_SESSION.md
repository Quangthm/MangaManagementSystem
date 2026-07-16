# Current Session — Create Chapter Production Eligibility

**Date:** 2026-07-16
**Branch:** `feature/Mangaka`
**Status:** Implemented; static verification only

## Goal

Allow Mangaka chapter creation only when the parent series is `SERIALIZED` or `HIATUS`, while preserving existing authorization, input normalization, draft initialization, and database-owned duplicate handling.

## Production files changed

- `MangaManagementSystem/src/MangaManagementSystem.Domain/Policies/SeriesProductionPolicy.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Repositories/MangakaChapterRepository.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Mangaka/Chapters.razor`

## Architecture flow

```text
Chapters.razor
→ typed chapter API client
→ Mangaka Chapters API controller
→ CreateChapterDraftCommand
→ CreateChapterDraftCommandHandler
→ MangakaChapterRepository
→ EF Core / SQL Server
```

No API, command, handler, client, DTO, task, workspace, SQL, migration, or configuration file was changed.

## Implementation

- Added `SeriesProductionPolicy.AllowsNormalProduction(string?)` with case-insensitive allow rules for `SERIALIZED` and `HIATUS` only.
- Create Chapter now loads the exact parent series row inside the existing repository transaction, distinguishes a missing series, validates the existing active Mangaka contributor rule, and rejects ineligible series states.
- Create Chapter uses `IsolationLevel.RepeatableRead`. The exact series-row read is retained through commit, so completion or board cancellation must wait for the chapter transaction; no phantom/range protection is needed.
- The Chapters page retains all series for list filtering but exposes only production-eligible series in the create dialog, disables creation when none are eligible, and protects stale handler invocation.
- Existing database-first duplicate-label handling remains unchanged; no pre-insert duplicate query was added.

## Separate follow-up

`ChapterConfiguration` declares an unfiltered unique index while `MangaManagementSystem_Schema.sql` defines the intended filtered index for non-cancelled chapters. This task intentionally did not modify EF metadata, SQL, schema, or migrations.

## Verification boundary

Only static checks are authorized: status/diff inspection, `git diff --check`, and targeted source searches. Restore, build, test, run, server, watcher, and migration commands were not run.

## Final handoff

`docs/revision/2026-07-16-create-chapter-production-eligibility.md`
