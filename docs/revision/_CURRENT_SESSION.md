# Current Session — Chapter Scheduling Implementation

**Branch:** `feature/Mangaka`
**Date:** 2026-07-03
**Status:** Implementation complete, build verified

## Goal
Implement chapter-level publication scheduling workflow including planned release dates, frequency validation, SCHEDULED/ON_HOLD status transitions, page mutation guards, audit events, and thin API endpoints.

## Architecture Flow
```
Blazor Web -> typed API client -> API Controller -> IMediator.Send -> Application handler -> Infrastructure EF Core / UoW -> SQL Server
```

## Files Changed (20 modified)

| File | Purpose |
|---|---|
| `API/Contracts/MangakaChapterRequests.cs` | Added `SetPlannedReleaseDateApiRequest` |
| `API/Controllers/Assistant/AssistantTaskController.cs` | Added chapter mutation guard for task submission |
| `API/Controllers/Editor/EditorChapterReviewsController.cs` | Added reschedule + hold endpoints |
| `API/Controllers/Mangaka/MangakaChaptersController.cs` | Added planned-release-date endpoint |
| `API/Controllers/Mangaka/MangakaPageController.cs` | Added chapter mutation guards (create/delete/version/set-current) |
| `Application/DTOs/Manga/MangakaChapterDtos.cs` | Added `SetPlannedReleaseDateRequest` |
| `Application/DependencyInjection.cs` | Registered `ChapterSchedulingValidator` as singleton |
| `Application/Features/.../SubmitChapterEditorialReviewCommandHandler.cs` | Routes to scheduling-aware method |
| `Application/Interfaces/IChapterService.cs` | Added `EnsureChapterAllowsContentMutationsAsync` |
| `Application/Interfaces/IMangakaChapterRepository.cs` | Made partial for scheduling extension |
| `Application/Services/ChapterService.cs` | Added mutation-guard implementation |
| `Domain/Interfaces/IEditorChapterReviewRepository.cs` | Made partial for scheduling extension |
| `Infrastructure/DependencyInjection.cs` | Registered `IPublicationPeriodRepository`, `IChapterOnHoldRepository` |
| `Infrastructure/Persistence/ApplicationDbContext.cs` | Added `PublicationPeriod` DbSet |
| `Infrastructure/Repositories/EditorChapterReviewRepository.cs` | Made partial for scheduling extension |
| `Infrastructure/Repositories/MangakaChapterRepository.cs` | Made partial for scheduling extension |
| `Web/Services/Api/EditorChapterReviewApiClient.cs` | Added reschedule + hold client methods |
| `Web/Services/Api/IEditorChapterReviewApiClient.cs` | Added reschedule + hold client interface methods |
| `Web/Services/Api/IMangakaChapterApiClient.cs` | Added `SetPlannedReleaseDateAsync` |
| `Web/Services/Api/MangakaChapterApiClient.cs` | Added `SetPlannedReleaseDateAsync` implementation |

## Files Created (16 new)

| File | Purpose |
|---|---|
| `API/Contracts/EditorChapterRequests.cs` | `EditorRescheduleChapterRequest`, `EditorPutChapterOnHoldRequest` |
| `Application/DTOs/Manga/SetChapterPlannedReleaseDateResponse.cs` | Response DTO with validation info |
| `Application/Features/.../SetChapterPlannedReleaseDate/` | Command + handler for Mangaka-side date setting |
| `Application/Features/.../RescheduleChapter/` | Command + handler + response for Editor reschedule |
| `Application/Features/.../PutScheduledChapterOnHold/` | Command + handler + response for Editor hold |
| `Application/Interfaces/IMangakaChapterRepository.Scheduling.cs` | Partial interface for scheduling methods |
| `Application/Services/ChapterSchedulingValidator.cs` | Frequency/period validation policy |
| `Domain/Entities/PublicationPeriod.cs` | EF entity for PublicationPeriod table |
| `Domain/Interfaces/IChapterOnHoldRepository.cs` | Domain interface for hold operations |
| `Domain/Interfaces/IEditorChapterReviewRepository.Scheduling.cs` | Partial interface for scheduling repo methods |
| `Domain/Interfaces/IPublicationPeriodRepository.cs` | Domain interface for period lookups |
| `Infrastructure/Persistence/Configurations/PublicationPeriodConfiguration.cs` | EF config |
| `Infrastructure/Repositories/ChapterOnHoldRepository.cs` | ON_HOLD implementation |
| `Infrastructure/Repositories/EditorChapterReviewRepository.Scheduling.cs` | Approval-to-SCHEDULED + reschedule impl |
| `Infrastructure/Repositories/MangakaChapterRepository.Scheduling.cs` | Set-planned-date implementation |
| `Infrastructure/Repositories/PublicationPeriodRepository.cs` | Period lookup implementation |

## Status Transition Rules Implemented

- **DRAFT/REVISION_REQUESTED + set planned date** → status stays DRAFT/REVISION_REQUESTED
- **UNDER_REVIEW + Editor approves + planned_release_date exists** → SCHEDULED
- **UNDER_REVIEW + Editor approves + NO planned_release_date** → APPROVED
- **APPROVED + set planned date** → SCHEDULED
- **SCHEDULED + Editor reschedule** → status stays SCHEDULED
- **SCHEDULED + Editor put on hold** → ON_HOLD (preserves planned_release_date)

## Frequency Validation

- Uses `PublicationPeriod` table for WEEKLY/MONTHLY period boundaries
- Previous planned chapter lookup: latest non-cancelled chapter with planned_release_date in same series, ordered by planned_release_date DESC
- Non-first WEEKLY: date must be inside next weekly period after previous chapter's period
- Non-first MONTHLY: date must be inside next monthly period after previous chapter's period
- First WEEKLY: current WEEKLY period OR next WEEKLY period
- First MONTHLY: current MONTHLY period only
- IRREGULAR: no period boundary enforcement
- NULL frequency: allowed without validation

## Page Workflow Locking

Blocked when chapter status is **UNDER_REVIEW, APPROVED, SCHEDULED, ON_HOLD, RELEASED, CANCELLED**:
- Page creation (`POST /api/mangaka/pages/create-with-file`)
- Page deletion (`DELETE /api/mangaka/pages/{pageId}`)
- Page version upload (`POST /api/mangaka/pages/versions/create-with-file`)
- Set current version (`PUT /api/mangaka/pages/{pageId}/versions/set-current`)
- Assistant task submission (`POST /api/assistant/tasks/{taskId}/submit-work`)
- Mangaka chapter edit (already locked via `EnsureCanEditChapter` check for DRAFT/REVISION_REQUESTED only)

## API Endpoints Added

- **PUT** `/api/mangaka/chapters/{chapterId}/planned-release-date` — Mangaka sets planned date
- **PUT** `/api/editor/chapters/{chapterId}/reschedule` — Editor reschedules SCHEDULED chapter
- **POST** `/api/editor/chapters/{chapterId}/hold` — Editor puts SCHEDULED chapter ON_HOLD

## Audit Events

- **CHAPTER_PLANNED_RELEASE_DATE_SET** — When planned date is set on a plannable chapter
- **CHAPTER_SCHEDULED** — When approval + existing planned_release_date results in SCHEDULED
- **CHAPTER_RESCHEDULED** — When Editor reschedules a SCHEDULED chapter
- **CHAPTER_PUT_ON_HOLD** — When Editor puts a SCHEDULED chapter ON_HOLD

Each includes: chapter_id, series_id, old/new status codes, old/new planned release dates, actor user id, reason (where applicable).

## Build Result
**SUCCESS** — 0 errors, pre-existing warnings only (no new warnings in changed files)

## Manual Smoke Test Checklist

- [ ] Mangaka can set planned_release_date on an editable/plannable chapter
- [ ] Editor can set planned_release_date on a plannable chapter
- [ ] Editor approval with no planned_release_date sets status APPROVED
- [ ] Editor approval with planned_release_date sets status SCHEDULED
- [ ] Approved chapter gets planned_release_date and becomes SCHEDULED
- [ ] SCHEDULED chapter blocks Mangaka content/page mutation
- [ ] SCHEDULED chapter allows Editor reschedule
- [ ] SCHEDULED chapter allows Editor put ON_HOLD with required reason
- [ ] ON_HOLD recovery is not available
- [ ] WEEKLY first planned chapter accepts current week and next week
- [ ] WEEKLY first planned chapter rejects dates outside current/next week
- [ ] MONTHLY first planned chapter accepts current month
- [ ] MONTHLY first planned chapter rejects dates outside current month
- [ ] Non-first WEEKLY chapter requires next weekly PublicationPeriod
- [ ] Non-first MONTHLY chapter requires next monthly PublicationPeriod
- [ ] IRREGULAR chapter scheduling does not enforce weekly/monthly boundaries

## Remaining Follow-ups

- ON_HOLD recovery not implemented (resume-from-hold, unschedule)
- Release automation not implemented
- Public release visibility not implemented
- UI wiring (planned release date controls in Mangaka/Editor pages) partially present; full UI integration expected in follow-up
