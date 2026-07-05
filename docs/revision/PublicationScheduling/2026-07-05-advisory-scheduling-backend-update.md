# Advisory Scheduling Backend Update

**Date:** 2026-07-05
**Branch:** `feature/Mangaka`
**Build:** SUCCESS (0 errors, pre-existing warnings only)

---

## Summary

Removed strict `PublicationPeriod` enforcement from chapter scheduling. Publication frequency is now advisory: the system may suggest dates but never hard-blocks scheduling. Mangaka and Editor can both schedule/reschedule chapters when status/permissions allow. Added chapter release function. Revised ON_HOLD. Updated audit events.

---

## Files Changed

### Created (5)
| Layer | File | Purpose |
|---|---|---|
| Domain | `Domain/Interfaces/IChapterReleaseRepository.cs` | Release repo interface + `ChapterReleaseResult` record |
| Infrastructure | `Infrastructure/Repositories/ChapterReleaseRepository.cs` | Release implementation: validates Editor permission, status, confirm flag, sets RELEASED + released_at_utc, writes CHAPTER_RELEASED audit |
| Application | `Features/Editor/ChapterReviews/Commands/ReleaseChapter/ReleaseChapterCommand.cs` | CQRS command |
| Application | `Features/Editor/ChapterReviews/Commands/ReleaseChapter/ReleaseChapterCommandHandler.cs` | Handler delegates to `IChapterReleaseRepository` |
| Application | `Features/Editor/ChapterReviews/Commands/ReleaseChapter/ReleaseChapterResponse.cs` | Response DTO with ChapterId, StatusCode, ReleasedAtUtc, PlannedReleaseDate, Message |

### Modified (13)
| Layer | File | Change |
|---|---|---|
| Application | `Services/ChapterSchedulingValidator.cs` | **Refactored**: removed `ValidateAsync` with blocking PublicationPeriod rules. Replaced with `GetAdvisoryAsync` — returns `AdvisoryResult` with `SuggestedDate` and `WarningMessage` for WEEKLY/MONTHLY patterns. Never blocks. |
| Application | `DTOs/Manga/SetChapterPlannedReleaseDateResponse.cs` | Added optional `SuggestedReleaseDate` and `WarningMessage` fields for advisory warnings |
| Application | `Interfaces/IMangakaChapterRepository.Scheduling.cs` | Removed `ChapterSchedulingValidator` parameter from `SetPlannedReleaseDateAsync` |
| Infrastructure | `Repositories/MangakaChapterRepository.Scheduling.cs` | Removed PublicationPeriod validation. `IsPlannableOrReschedulableChapter` now allows DRAFT, REVISION_REQUESTED, APPROVED, SCHEDULED, ON_HOLD. Uses `CHAPTER_RESCHEDULED` audit code when rescheduling. |
| Application | `Features/Mangaka/.../SetChapterPlannedReleaseDateCommandHandler.cs` | Removed `ChapterSchedulingValidator` dependency |
| Domain | `Interfaces/IEditorChapterReviewRepository.Scheduling.cs` | No signature change (already didn't have validator param) |
| Infrastructure | `Repositories/EditorChapterReviewRepository.Scheduling.cs` | `SetPlannedReleaseDateAsync` and `ReschedulePlannedReleaseDateAsync` now accept SCHEDULED + ON_HOLD. ON_HOLD → SCHEDULED transition on reschedule. Removed duplicate `StatusOnHold` constant (uses existing from parent partial). |
| Application | `Features/Editor/.../EditorSetChapterPlannedReleaseDateCommandHandler.cs` | Removed `ChapterSchedulingValidator` dependency |
| Infrastructure | `Repositories/ChapterOnHoldRepository.cs` | Added `actor_user_id` to audit detail JSON |
| API | `Contracts/EditorChapterRequests.cs` | Added `EditorReleaseChapterRequest` with `ConfirmRelease` field |
| API | `Controllers/Editor/EditorChapterReviewsController.cs` | Added `POST /api/editor/chapters/{id}/release` endpoint |
| Infrastructure | `DependencyInjection.cs` | Registered `IChapterReleaseRepository` → `ChapterReleaseRepository` |

---

## Removed Legacy Strict Enforcement

| Old Rule | New Behavior |
|---|---|
| CHAPTER_PLANNED_RELEASE_DATE_SET blocked if date outside PublicationPeriod | Date accepted if future + status allows; warning returned if pattern mismatch |
| WEEKLY first chapter required current/next weekly period | Not enforced |
| MONTHLY first chapter required current monthly period | Not enforced |
| Non-first chapters required next PublicationPeriod | Not enforced |
| `ChapterSchedulingValidator.ValidateAsync` returned blocking `ValidationResult` | Replaced with `GetAdvisoryAsync` returning `AdvisoryResult` |
| Only Editor could reschedule | Both Mangaka (via set-planned-date) and Editor can reschedule |

---

## New Advisory Scheduling Behavior

### ChapterSchedulingValidator now provides:
- `GetAdvisoryAsync(frequencyCode, selectedDate)` → `AdvisoryResult`
- `SuggestedDate`: the date matching the frequency pattern (same weekday next week for WEEKLY, same day next month for MONTHLY)
- `WarningMessage`: non-null when selected date differs from suggested date
- Returns `NoSuggestion()` for IRREGULAR, NULL, or empty frequency

### Response DTO carries:
- `SuggestedReleaseDate` (DateTime?) — pattern-based suggestion if applicable
- `WarningMessage` (string?) — advisory warning when date doesn't match frequency pattern

Note: The current repository methods do not yet call `GetAdvisoryAsync` to populate these fields — this is a TODO for the UI task when calendar/scheduling date-picker integration happens. The fields exist in the response contract for the UI to consume.

---

## Hard Validations (Still Enforced)

| Check | Error |
|---|---|
| Planned date in past | "Planned release date cannot be in the past." |
| Invalid status (UNDER_REVIEW, RELEASED, CANCELLED) | "This chapter's status does not allow..." |
| Missing permission (not active contributor) | "You are not authorized..." |
| ON_HOLD with empty reason | "A reason is required..." |
| Release without ConfirmRelease=true | "Release confirmation is required." |
| Release on CANCELLED | "A cancelled chapter cannot be released." |
| Release on non-SCHEDULED/APPROVED | "Only SCHEDULED or APPROVED chapters can be released." |

---

## Status Transition Behavior

| Current Status | Action | Actor | New Status |
|---|---|---|---|
| DRAFT | Set planned date | Mangaka/Editor | DRAFT |
| REVISION_REQUESTED | Set planned date | Mangaka/Editor | REVISION_REQUESTED |
| UNDER_REVIEW + Editor approve + planned date exists | Review decision | Editor | SCHEDULED |
| UNDER_REVIEW + Editor approve + no planned date | Review decision | Editor | APPROVED |
| APPROVED | Set planned date | Mangaka/Editor | SCHEDULED |
| SCHEDULED | Set planned date (reschedule) | Mangaka/Editor | SCHEDULED |
| SCHEDULED | Put ON_HOLD with reason | Editor | ON_HOLD |
| ON_HOLD | Set planned date with new future date | Mangaka/Editor | SCHEDULED |
| SCHEDULED/APPROVED | Release with ConfirmRelease=true | Editor | RELEASED |
| RELEASED | Any schedule/reschedule/hold/release | — | Blocked |
| CANCELLED | Any schedule/reschedule/hold/release | — | Blocked |

---

## Put ON_HOLD Behavior

- Only when current status = SCHEDULED
- Requires non-empty reason (max 1000 chars)
- **Preserves** `planned_release_date` (not cleared)
- Sets `status_code = ON_HOLD`
- Audit includes: chapter_id, series_id, old_status_code, new_status_code, old_planned_release_date, actor_user_id, reason

---

## Release Function

### Command
`ReleaseChapterCommand { ActorUserId, ChapterId, ConfirmRelease }`

### Endpoint
`POST /api/editor/chapters/{id}/release`
Request: `{ "confirmRelease": true }`

### Behavior
- Actor: Tantou Editor (validated via active series contributor)
- Allowed statuses: SCHEDULED, APPROVED
- Requires `ConfirmRelease = true`
- Sets `status_code = RELEASED`
- Sets `released_at_utc = DateTime.UtcNow`
- If `planned_release_date` null → set to `DateTime.UtcNow.Date` (UTC date as MVP publication business date)
- If `planned_release_date` exists → preserved for planned-vs-actual comparison
- Writes `CHAPTER_RELEASED` audit event
- Single EF transaction

---

## Audit Events

| Code | Trigger | Includes |
|---|---|---|
| `CHAPTER_PLANNED_RELEASE_DATE_SET` | Initial planned date set on DRAFT/REVISION_REQUESTED/APPROVED | chapter_id, series_id, old/new status, old/new planned date, actor_user_id, frequency_code |
| `CHAPTER_RESCHEDULED` | Planned date changed on SCHEDULED or ON_HOLD → SCHEDULED | chapter_id, series_id, old/new status, old/new planned date, reason, actor_user_id |
| `CHAPTER_SCHEDULED` | Editor approves UNDER_REVIEW chapter with planned date | chapter_id, series_id, review_id, decision, planned_release_date |
| `CHAPTER_PUT_ON_HOLD` | Editor puts SCHEDULED chapter ON_HOLD | chapter_id, series_id, old/new status, old planned date, reason, actor_user_id |
| `CHAPTER_RELEASED` | Editor releases SCHEDULED/APPROVED chapter | chapter_id, series_id, old/new status, old/new planned date, released_at_utc, actor_user_id, confirm_release |

---

## API Endpoints / Contracts

| Method | Route | Role | Description |
|---|---|---|---|
| PUT | `/api/mangaka/chapters/{id}/planned-release-date` | Mangaka | Set/reschedule planned date (including SCHEDULED/ON_HOLD) |
| PUT | `/api/editor/chapters/{id}/planned-release-date` | Editor | Set/reschedule planned date (including SCHEDULED/ON_HOLD) |
| PUT | `/api/editor/chapters/{id}/reschedule` | Editor | Reschedule SCHEDULED/ON_HOLD with reason |
| POST | `/api/editor/chapters/{id}/hold` | Editor | Put SCHEDULED chapter ON_HOLD |
| **POST** | **`/api/editor/chapters/{id}/release`** | **Editor** | **Release chapter (NEW)** |

---

## Build Result

**SUCCESS** — 0 errors, 0 warnings from changed files. All warnings are pre-existing (ChapterPageTaskRepository.cs, SeriesProposalRepository.cs, Razor generated code, etc.).

---

## Backend Smoke Checklist

- [ ] Mangaka can schedule future date on DRAFT chapter
- [ ] Mangaka can schedule future date on REVISION_REQUESTED chapter
- [ ] Mangaka can schedule future date on APPROVED → becomes SCHEDULED
- [ ] Editor can schedule future date on DRAFT, REVISION_REQUESTED, APPROVED
- [ ] Mangaka can reschedule SCHEDULED chapter (via set-planned-date)
- [ ] Editor can reschedule SCHEDULED chapter (via set-planned-date or dedicated reschedule endpoint)
- [ ] Mangaka can reschedule ON_HOLD chapter → becomes SCHEDULED
- [ ] Editor can reschedule ON_HOLD chapter → becomes SCHEDULED
- [ ] Past planned release date is rejected
- [ ] Weekly/monthly mismatch no longer blocks scheduling
- [ ] APPROVED + planned date moves to SCHEDULED
- [ ] ON_HOLD + schedule with new future date moves to SCHEDULED
- [ ] Put ON_HOLD keeps old planned_release_date
- [ ] Release with existing planned date preserves planned date and sets released_at_utc
- [ ] Release with null planned date sets planned_release_date to current UTC date and sets released_at_utc
- [ ] Release moves status to RELEASED
- [ ] Release without ConfirmRelease=true is rejected
- [ ] Release on CANCELLED is blocked
- [ ] Release on already RELEASED is blocked
- [ ] CANCELLED/RELEASED schedule actions are blocked
- [ ] Content/page mutation locking still works for SCHEDULED/ON_HOLD/RELEASED/CANCELLED
- [ ] CHAPTER_RESCHEDULED audit event on SCHEDULED reschedule
- [ ] CHAPTER_RESCHEDULED audit event on ON_HOLD → SCHEDULED
- [ ] CHAPTER_RELEASED audit event is written with correct data

---

## Remaining Follow-ups

1. **UI integration** — Front-end date pickers, calendar scheduling actions, release button, hold dialog, warning display for frequency mismatch. Next task.
2. **Populate advisory fields** — Repository methods do not yet call `ChapterSchedulingValidator.GetAdvisoryAsync` to fill `SuggestedReleaseDate`/`WarningMessage` in the response. The response contract already supports these fields. Wire in the next UI task.
3. **ON_HOLD recovery dedicated endpoint** — Currently handled via set-planned-date/reschedule on ON_HOLD status. May want a dedicated endpoint later.
4. **Auto-hold overdue chapters** — Background job / hosted service to detect overdue SCHEDULED chapters (deferred).
5. **Publication timezone** — Currently uses `DateTime.UtcNow.Date` as MVP publication business date. May need a Vietnam timezone (UTC+7) helper later.
6. **Mangaka release permission** — Currently only Tantou Editor can release. If project rules change, Mangaka release may be added.
