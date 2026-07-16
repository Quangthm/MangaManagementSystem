# Create Chapter Production Eligibility Handoff

**Date:** 2026-07-16
**Branch:** `feature/Mangaka`

## Result

Mangaka users can create chapter drafts only for parent series in `SERIALIZED` or `HIATUS`. UI gating improves usability, while repository enforcement remains authoritative for direct or stale API requests.

## Final data flow

```text
Chapters.razor.CreateChapterDraft
→ MangakaChapterApiClient.CreateChapterDraftAsync
→ MangakaChaptersController.CreateChapterDraftAsync
→ CreateChapterDraftCommandHandler.Handle
→ MangakaChapterRepository.CreateChapterDraftAsync
→ RepeatableRead EF transaction
→ manga.Chapter
→ UI refresh
```

## Policy behavior

`SeriesProductionPolicy.AllowsNormalProduction(string?)` allows `SERIALIZED` and `HIATUS` using ordinal case-insensitive comparisons. Proposal, review, completed, cancelled, null, blank, and unknown values are rejected.

This policy covers normal production mutations only. It does not decide lifecycle transitions, navigation visibility, publication-calendar visibility, scheduling, release, or task/workspace capabilities.

## Checks preserved and added

Preserved:

- Valid actor and series IDs.
- Active database user with the Mangaka role.
- Active Mangaka contributor membership for the selected series.
- Required/trimmed chapter label and optional trimmed title.
- New chapter status `DRAFT`.
- `created_by_user_id` from the actor and authoritative server UTC creation time.
- Existing safe API error mapping.
- Existing database-owned duplicate-label enforcement and 2601/2627 friendly mapping.

Added:

- Explicit parent-series existence check.
- Authoritative parent-series production-policy check.
- Create-dialog filtering, button state, explanatory text, and stale-handler guard.

## Transaction and concurrency

Create Chapter now uses `IsolationLevel.RepeatableRead` and loads the exact parent `Series` row by primary key before authorization and insertion. SQL Server retains the shared lock on that existing row until commit. The completion handler updates that tracked row, and board cancellation executes an `UPDATE` against the same row; both require an exclusive lock and therefore wait until chapter creation commits or rolls back.

`Serializable` was not selected because eligibility does not depend on a range query and no phantom protection is required. Missing series are rejected without insertion.

## Constraint ownership

The database remains authoritative for foreign keys, nullability, lengths, status checks, and chapter-label uniqueness. No pre-insert duplicate query was added, so concurrency correctness and cancelled-label reuse remain owned by the existing filtered SQL index.

Separate follow-up: EF `ChapterConfiguration` currently models an unfiltered unique index while the SQL schema defines the intended filtered non-cancelled index. Per scope, no EF metadata, SQL, schema, or migration change was made.

## Files changed

Production:

- `Domain/Policies/SeriesProductionPolicy.cs`
- `Infrastructure/Repositories/MangakaChapterRepository.cs`
- `Web/Components/Pages/Mangaka/Chapters.razor`

Documentation:

- `docs/revision/_CURRENT_SESSION.md`
- `docs/revision/2026-07-16-create-chapter-production-eligibility.md`

## Protected files

No task, workspace, DTO, API controller, command, handler, typed-client, SQL, migration, or unrelated configuration file was modified.

## Verification

Static verification only. Restore, build, test, run, servers, watchers, and migrations were not authorized.

Manual smoke checks remain for later execution:

- Allow `SERIALIZED` and `HIATUS` with active Mangaka membership.
- Block proposal/review, `COMPLETED`, `CANCELLED`, null, and unknown statuses.
- Block missing series and inactive/non-contributor actors.
- Confirm duplicate active labels retain the friendly error.
- Confirm a cancelled chapter label remains reusable.
- Confirm concurrent complete/cancel waits for Create Chapter to commit.
