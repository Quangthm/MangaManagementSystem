# Chapter-Level Publication Scheduling and ON_HOLD Workflow Implementation

**Date:** 2026-07-03
**Branch:** `feature/Mangaka`
**Build:** SUCCESS (0 errors, pre-existing warnings only)

---

## Summary

Implemented chapter-level publication scheduling workflow including:
- Planned release date setting by Mangaka/Editor on plannable chapters
- Frequency validation using `PublicationPeriod` table (WEEKLY, MONTHLY, IRREGULAR rules)
- SCHEDULED transition when approved chapter has planned_release_date
- ON_HOLD with required reason (Editor-only)
- Page/content mutation locking when chapter is locked
- Audit events for all scheduling state changes
- Thin API endpoints and typed Web API clients

---

## Files Changed

### Modified (20 files)
| File | Purpose |
|---|---|
| `API/Contracts/MangakaChapterRequests.cs` | Added `SetPlannedReleaseDateApiRequest` |
| `API/Controllers/Assistant/AssistantTaskController.cs` | Added mutation guard for task submission |
| `API/Controllers/Editor/EditorChapterReviewsController.cs` | Added reschedule + hold endpoints |
| `API/Controllers/Mangaka/MangakaChaptersController.cs` | Added planned-release-date endpoint |
| `API/Controllers/Mangaka/MangakaPageController.cs` | Added mutation guards (create/delete/version) |
| `Application/DTOs/Manga/MangakaChapterDtos.cs` | Added `SetPlannedReleaseDateRequest` |
| `Application/DependencyInjection.cs` | Registered `ChapterSchedulingValidator` |
| `Application/.../SubmitChapterEditorialReviewCommandHandler.cs` | Routes to scheduling-aware method |
| `Application/Interfaces/IChapterService.cs` | Added `EnsureChapterAllowsContentMutationsAsync` |
| `Application/Interfaces/IMangakaChapterRepository.cs` | Made partial |
| `Application/Services/ChapterService.cs` | Added mutation guard implementation |
| `Domain/Interfaces/IEditorChapterReviewRepository.cs` | Made partial |
| `Infrastructure/DependencyInjection.cs` | Registered new repos |
| `Infrastructure/Persistence/ApplicationDbContext.cs` | Added `PublicationPeriod` DbSet |
| `Infrastructure/Repositories/EditorChapterReviewRepository.cs` | Made partial |
| `Infrastructure/Repositories/MangakaChapterRepository.cs` | Made partial |
| `Web/Services/Api/EditorChapterReviewApiClient.cs` | Added reschedule + hold methods |
| `Web/Services/Api/IEditorChapterReviewApiClient.cs` | Added reschedule + hold interface |
| `Web/Services/Api/IMangakaChapterApiClient.cs` | Added `SetPlannedReleaseDateAsync` |
| `Web/Services/Api/MangakaChapterApiClient.cs` | Added `SetPlannedReleaseDateAsync` impl |

### Created (19 files)
| File | Purpose |
|---|---|
| `API/Contracts/EditorChapterRequests.cs` | API request types |
| `Application/DTOs/.../SetChapterPlannedReleaseDateResponse.cs` | Response DTO |
| `Application/Features/.../SetChapterPlannedReleaseDate/` | Mangaka command + handler |
| `Application/Features/.../EditorSetChapterPlannedReleaseDate/` | Editor command + handler |
| `Application/Features/.../RescheduleChapter/` | Command + handler + response |
| `Application/Features/.../PutScheduledChapterOnHold/` | Command + handler + response |
| `Application/Interfaces/IMangakaChapterRepository.Scheduling.cs` | Partial interface |
| `Application/Services/ChapterSchedulingValidator.cs` | Validation policy |
| `Domain/Entities/PublicationPeriod.cs` | EF entity |
| `Domain/Interfaces/ChapterPlannedDateResult.cs` | Domain result for editor scheduling |
| `Domain/Interfaces/IChapterOnHoldRepository.cs` | Hold repo interface |
| `Domain/Interfaces/IEditorChapterReviewRepository.Scheduling.cs` | Scheduling partial |
| `Domain/Interfaces/IPublicationPeriodRepository.cs` | Period repo interface |
| `Infrastructure/.../PublicationPeriodConfiguration.cs` | EF config |
| `Infrastructure/Repositories/ChapterOnHoldRepository.cs` | ON_HOLD impl |
| `Infrastructure/Repositories/EditorChapterReviewRepository.Scheduling.cs` | Approval+scheduling+editor-set-date impl |
| `Infrastructure/Repositories/MangakaChapterRepository.Scheduling.cs` | Set-date impl |
| `Infrastructure/Repositories/PublicationPeriodRepository.cs` | Period lookup impl |
| `Infrastructure/Repositories/EditorChapterReviewRepository.Scheduling.cs` | Approval+scheduling impl |
| `Infrastructure/Repositories/MangakaChapterRepository.Scheduling.cs` | Set-date impl |
| `Infrastructure/Repositories/PublicationPeriodRepository.cs` | Period lookup impl |

---

## Status Transition Behavior

| Current Status | Action | New Status |
|---|---|---|
| DRAFT | Set planned_release_date by Mangaka/Editor | DRAFT |
| REVISION_REQUESTED | Set planned_release_date by Mangaka/Editor | REVISION_REQUESTED |
| UNDER_REVIEW | Editor approves + has planned_release_date | **SCHEDULED** |
| UNDER_REVIEW | Editor approves + NO planned_release_date | APPROVED |
| APPROVED | Set planned_release_date by Mangaka/Editor | **SCHEDULED** |
| SCHEDULED | Editor reschedules planned_release_date | SCHEDULED |
| SCHEDULED | Editor puts ON_HOLD with reason | **ON_HOLD** |
| SCHEDULED | Mangaka attempts content mutation | **Blocked** |
| ON_HOLD | Any recovery action | **Not implemented** |

---

## Frequency Validation Behavior

### Previous Planned Chapter Definition
Latest non-cancelled chapter in the same series where:
- `planned_release_date IS NOT NULL`
- `chapter_id != current chapter id`
- `status_code != 'CANCELLED'`
- Ordered by `planned_release_date DESC`

### Rules

| Scenario | Frequency | Rule |
|---|---|---|
| Previous chapter exists | WEEKLY | Next weekly PublicationPeriod after previous chapter's period |
| Previous chapter exists | MONTHLY | Next monthly PublicationPeriod after previous chapter's period |
| Previous chapter exists | IRREGULAR | No boundary enforcement |
| First chapter | WEEKLY | Current weekly PublicationPeriod **OR** next weekly PublicationPeriod |
| First chapter | MONTHLY | Current monthly PublicationPeriod **only** |
| First chapter | IRREGULAR | No boundary enforcement |
| Any | NULL | Allowed without strict validation |

---

## First Planned Chapter Behavior

- **WEEKLY:** planned_release_date may be inside current WEEKLY period or next WEEKLY period
- **MONTHLY:** planned_release_date must be inside current MONTHLY period only

---

## Page Workflow Locking

Content mutations **blocked** when chapter status is: UNDER_REVIEW, APPROVED, SCHEDULED, ON_HOLD, RELEASED, CANCELLED

Guards added for:
- Page creation (`MangakaPageController.CreatePageWithVersionAsync`)
- Page deletion (`MangakaPageController.DeleteAsync`)
- Page version upload (`MangakaPageController.CreateVersionWithFileAndRegionsAsync`)
- Set current version (`MangakaPageController.SetCurrentVersionAsync`)
- Assistant task submission (`AssistantTaskController.SubmitWorkAsync`)
- Chapter metadata editing (`MangakaChapterRepository.EnsureCanEditChapter` — already locked for non-DRAFT/REVISION_REQUESTED)

---

## API Endpoints

| Method | Route | Description | Role |
|---|---|---|---|
| PUT | `/api/mangaka/chapters/{id}/planned-release-date` | Set planned date on plannable chapter | Mangaka |
| PUT | `/api/editor/chapters/{id}/planned-release-date` | Set planned date on plannable chapter | Editor |
| PUT | `/api/editor/chapters/{id}/reschedule` | Reschedule SCHEDULED chapter | Editor |
| POST | `/api/editor/chapters/{id}/hold` | Put SCHEDULED chapter ON_HOLD | Editor |

---

## Audit Events

| Code | Trigger |
|---|---|
| `CHAPTER_PLANNED_RELEASE_DATE_SET` | Planned date set on plannable chapter |
| `CHAPTER_SCHEDULED` | Approval decision with existing planned_release_date |
| `CHAPTER_RESCHEDULED` | Editor reschedules a SCHEDULED chapter |
| `CHAPTER_PUT_ON_HOLD` | Editor puts SCHEDULED chapter ON_HOLD |

Each includes: chapter_id, series_id, old/new status codes, old/new planned release dates, actor user id, reason (where applicable).

---

## UI/API Behavior

- **Set Planned Date** (`PUT /api/mangaka/chapters/{id}/planned-release-date`) — accepts `{ plannedReleaseDate }`, returns validation info including allowed period range and frequency code
- **Reschedule** (`PUT /api/editor/chapters/{id}/reschedule`) — accepts `{ newPlannedReleaseDate, reason }`, Editor-only
- **Hold** (`POST /api/editor/chapters/{id}/hold`) — accepts `{ reason }`, Editor-only, requires non-empty reason

---

## Build Result

**SUCCESS** — 0 errors. All warnings are pre-existing (none in changed files).

---

## Manual Smoke Test Checklist

- [ ] Mangaka can set planned_release_date on DRAFT/REVISION_REQUESTED/APPROVED chapter
- [ ] Editor can set planned_release_date on plannable chapter
- [ ] Editor approval with no planned_release_date → APPROVED
- [ ] Editor approval with planned_release_date → SCHEDULED
- [ ] APPROVED + set planned_release_date → SCHEDULED
- [ ] SCHEDULED chapter blocks page creation
- [ ] SCHEDULED chapter blocks page deletion
- [ ] SCHEDULED chapter blocks version upload
- [ ] SCHEDULED chapter blocks assistant task submission
- [ ] Editor can reschedule SCHEDULED chapter
- [ ] Editor can put SCHEDULED chapter ON_HOLD with required reason
- [ ] ON_HOLD preservation: planned_release_date retained after on-hold
- [ ] ON_HOLD recovery not available
- [ ] WEEKLY first chapter: accepts current week and next week
- [ ] WEEKLY first chapter: rejects dates outside current/next week
- [ ] MONTHLY first chapter: accepts current month
- [ ] MONTHLY first chapter: rejects dates outside current month
- [ ] Non-first WEEKLY: requires next weekly PublicationPeriod
- [ ] Non-first MONTHLY: requires next monthly PublicationPeriod
- [ ] IRREGULAR: no weekly/monthly boundary enforcement

---

## Remaining Follow-ups

- **ON_HOLD recovery** not implemented (resume-from-hold, unschedule)
- **Release automation** not implemented (auto-release at planned date)
- **Public release visibility** not implemented (reader-facing release calendar)
- **Full UI integration** for planned release date controls in existing Mangaka/Editor pages pending follow-up

---

## Verification Follow-up (2026-07-03)

### Editor initial scheduled-date endpoint added

The business rule requires both Mangaka **and** Tantou Editor to set an initial `planned_release_date` on plannable chapters. The Editor controller was missing this endpoint. Added:

- **PUT** `/api/editor/chapters/{id}/planned-release-date` → dispatches `EditorSetChapterPlannedReleaseDateCommand` → calls `IEditorChapterReviewRepository.SetPlannedReleaseDateAsync` (new partial) → implemented in `EditorChapterReviewRepository.Scheduling.cs` with Tantou Editor authorization, status guard, EF transaction, and `CHAPTER_PLANNED_RELEASE_DATE_SET` audit event.

### Verification Summary

| Check | Result |
|---|---|
| Editor can set initial planned date (DRAFT/REVISION_REQUESTED/APPROVED) | **Added** |
| Mangaka can set initial planned date | **Existing** |
| SCHEDULED approval validates planned_release_date exists | **Confirmed** (`.HasValue` check) |
| NULL frequency allows scheduling | **Confirmed** (returns `Success()`) |
| Audit events in same EF transaction | **Confirmed** (same `SaveChangesAsync` + `BeginTransactionAsync`) |
| Status strings include SCHEDULED and ON_HOLD | **Confirmed** (both repos have constants) |
| Mutation guards cover all blocked statuses | **Confirmed** (UNDER_REVIEW, APPROVED, SCHEDULED, ON_HOLD, RELEASED, CANCELLED) |
| PublicationPeriod table migration status | **No migration needed** — mapped to existing `manga.PublicationPeriod` table |
| Build | **SUCCESS** |

### Files Changed in Follow-up

| File | Change |
|---|---|
| `Domain/Interfaces/ChapterPlannedDateResult.cs` | New domain result record |
| `Domain/Interfaces/IEditorChapterReviewRepository.Scheduling.cs` | Added `SetPlannedReleaseDateAsync` |
| `Infrastructure/Repositories/EditorChapterReviewRepository.Scheduling.cs` | Added implementation |
| `Application/Features/.../SetChapterPlannedReleaseDate/EditorSetChapterPlannedReleaseDateCommand.cs` | New Editor command |
| `Application/Features/.../SetChapterPlannedReleaseDate/EditorSetChapterPlannedReleaseDateCommandHandler.cs` | New Editor handler |
| `API/Controllers/Editor/EditorChapterReviewsController.cs` | Added `PUT .../planned-release-date` endpoint |
| `Web/Services/Api/IEditorChapterReviewApiClient.cs` | Added `SetPlannedReleaseDateAsync` |
| `Web/Services/Api/EditorChapterReviewApiClient.cs` | Added implementation |
