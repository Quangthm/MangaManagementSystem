# Chapter Submit for Review — Active Task Gate (Backend)

**Date:** 2026-07-21  
**Branch:** feature/Mangaka  
**Scope:** Backend only — no frontend changes

---

## Summary

Added an authoritative backend validation rule: a Mangaka may submit a chapter for
editorial review **only when zero distinct active page tasks** (ASSIGNED or
UNDER_REVIEW) are associated with that chapter.

---

## New Prerequisite

Before `Chapter.StatusCode` transitions to `UNDER_REVIEW`:

```
eligible chapter status (DRAFT or REVISION_REQUESTED)
AND
zero active ASSIGNED/UNDER_REVIEW page tasks
```

---

## Blocking Statuses

| Task Status   | Blocks submission? |
|---------------|-------------------|
| ASSIGNED      | Yes               |
| UNDER_REVIEW  | Yes               |
| COMPLETED     | No                |
| CANCELLED     | No                |

Defined by the new domain predicate `IsActiveTaskStatus()` in
`ChapterPageTaskLifecyclePolicy` (not coupled to `CanCancel`).

---

## Relationship Path Used

```
Chapter
  ↑ ChapterPage.ChapterId
ChapterPage
  ↑ ChapterPageVersion.ChapterPageId
ChapterPageVersion
  ↑ PageRegion.ChapterPageVersionId
PageRegion
  ↕ many-to-many via ChapterPageTaskRegion (junction table)
ChapterPageTask
```

No direct `ChapterId` or `ChapterPageId` on `ChapterPageTask`. The existing
`GetByChapterIdsAsync` encapsulates this navigation chain.

---

## Distinct Task Counting

Tasks are deduplicated by `ChapterPageTaskId` via
`.GroupBy(task => task.ChapterPageTaskId).Select(group => group.First())`.

This ensures one task linked to multiple PageRegions counts as exactly one.

---

## Architecture

```
Submit Chapter ──────┐
                     ├──> GetDistinctActiveTasksByChapterIdsAsync()
Complete Series ─────┘     (reusable repository method)
```

- The new `GetDistinctActiveTasksByChapterIdsAsync` method returns
  `IReadOnlyList<ChapterPageTask>` (full entities for mutation use in
  Complete Series).
- Uses `ChapterPageTaskLifecyclePolicy.IsActiveTaskStatus()` as the active-task
  filter.
- Both completion handlers (`GetSeriesCompletionImpactQueryHandler`,
  `CompleteSeriesCommandHandler`) were refactored to use this shared method,
  replacing their previous inline dedup + `CanCancel()` pattern.

---

## Chapter-Scoped Applock

All three mutation paths that can create active tasks or change chapter status
participate in a shared exclusive applock:

**Resource key:** `manga_chapter_{ChapterId}`  
**Lock mode:** Exclusive  
**Lock owner:** Transaction  
**Timeout:** 5000 ms

### Lock participants (in acquisition order)

| Path | Lock order |
|------|-----------|
| `SubmitChapterForReviewAsync` | `manga_chapter_{ChapterId}` (only lock needed) |
| `CreateChapterPageTaskAsync` (single task) | `manga_chapter_{ChapterId}` (acquired after chapter resolved, before status validation) |
| `PersistQuickSelectAssignmentAsync` (Quick Select) | 1st: `manga_chapter_{ChapterId}`, 2nd: existing `manga_quick_select_task_assignment_*` |

### Lock result handling

`sp_getapplock` return value is checked: if `< 0` (lock not acquired), an
`InvalidOperationException` is thrown with a user-safe message. No raw SQL or
lock internals are exposed.

### Consistent ordering prevents deadlock cycles

All paths acquire `manga_chapter_{ChapterId}` first. Quick Select then acquires
its existing lock as a second step. This consistent two-resource order prevents
deadlock cycles between these application locks.

### Lock-before-read ordering

The chapter lock is acquired **before** any authoritative chapter-state reads:

- **Submit Chapter:** Lock → load chapter → validate status → check tasks
- **Single task creation:** Lock → validate actor/assignee → validate chapter status
- **Quick Select:** Lock (first) → Quick Select lock (second) → recheck guards

This ensures the invariant cannot be violated by a concurrent operation.

---

## Validation Flow (SubmitChapterForReviewAsync)

1. Begin explicit transaction (was previously implicit `SaveChangesAsync`)
2. Acquire `manga_chapter_{ChapterId}` exclusive applock
3. Load chapter (authoritative read after lock)
4. Validate actor is active Mangaka contributor
5. Validate chapter status is DRAFT or REVISION_REQUESTED
6. **NEW:** Call `GetDistinctActiveTasksByChapterIdsAsync(new[] { chapterId })`
   → if count > 0: throw `InvalidOperationException` (reject)
7. Validate at least one active Tantou Editor exists
8. Set chapter status to `UNDER_REVIEW`
9. Create `CHAPTER_REVIEW` notifications
10. Create submission audit event
11. `SaveChangesAsync` + commit transaction

---

## Clean API Error Response

| Aspect | Value |
|--------|-------|
| Exception | `InvalidOperationException` (reuses existing pattern) |
| HTTP status | `400 BadRequest` (caught by existing controller handler at `MangakaChaptersController.cs:208-210`) |
| Response body | `ApiErrorResponse{Code="request_failed", Message="..."}` |
| Message | *"This chapter cannot be submitted for editorial review while active page tasks are still assigned or under review. Complete or cancel those tasks before submitting the chapter."* |

No SQL, entity names, stack traces, or internal details exposed.

---

## Audit and Notification Behavior

| Event | Successful submit | Blocked submit |
|-------|-------------------|----------------|
| Chapter status | `UNDER_REVIEW` | Unchanged |
| Submission audit | Written (`CHAPTER_SUBMITTED_FOR_EDITORIAL_REVIEW`) | Not written |
| `CHAPTER_REVIEW` notification | Created for each active Tantou Editor | Not created |

Error thrown before any mutation → no partial writes (explicit transaction
rolls back on exception).

---

## Files Changed

### Required

| File | Change |
|------|--------|
| `Domain/Policies/ChapterPageTaskLifecyclePolicy.cs` | Made `AssignedStatusCode`/`UnderReviewStatusCode` public; added `IsActiveTaskStatus()` |
| `Domain/Interfaces/IChapterPageTaskRepository.cs` | Added `GetDistinctActiveTasksByChapterIdsAsync` signature |
| `Infrastructure/Repositories/ChapterPageTaskRepository.cs` | Added `GetDistinctActiveTasksByChapterIdsAsync` + `AcquireChapterLockAsync`; integrated into `CreateChapterPageTaskAsync` |
| `Infrastructure/Repositories/QuickSelectRepository.cs` | Added `AcquireChapterLockAsync`; chapter lock acquired before existing Quick Select lock in `PersistQuickSelectAssignmentAsync` |
| `Infrastructure/Repositories/MangakaChapterRepository.cs` | Injected `IChapterPageTaskRepository` + `ILogger`; wrapped `SubmitChapterForReviewAsync` in explicit transaction + chapter lock + active-task check |

### Refactored (reuse)

| File | Change |
|------|--------|
| `Application/Features/Series/Lifecycle/Queries/.../GetSeriesCompletionImpactQueryHandler.cs` | Replaced inline dedup+`CanCancel()` with `GetDistinctActiveTasksByChapterIdsAsync` + `.Count` |
| `Application/Features/Series/Lifecycle/Commands/.../CompleteSeriesCommandHandler.cs` | Replaced inline dedup+`CanCancel()` with `GetDistinctActiveTasksByChapterIdsAsync` |

### No change

- Web/Blazor UI files
- Database schema, migrations, stored procedures
- All entity classes
- Controller error handling (existing `catch (InvalidOperationException)` reused)
- `ApiErrorResponse` / `ApiResponses.cs`
- `ChapterPageTaskCreationPolicy`, `SeriesProductionPolicy`
- `CanCancel()` method (preserved unchanged)
- All task creation DTOs, services, `QuickSelectAssignmentPlan`

---

## Build Results

```
Build succeeded.
0 Errors
66 Warnings (all pre-existing; 0 new from changed files)
```

---

## Verified

- `git diff --check`: clean (no whitespace errors)
- `git status`: only the 7 expected files modified
- No Web/Blazor files changed
- No database/migration/SP files changed
- Blocked submit produces no status/audit/notification writes (enforced by
  exception thrown before any mutation)
- Successful submit behavior unchanged (existing flow preserved after
  active-task check passes)

---

## Frontend

Still pending — separate task.
