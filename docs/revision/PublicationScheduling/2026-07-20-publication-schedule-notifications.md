# Publication Schedule Notifications

## Branch

`feature/Mangaka`

## Date

2026-07-20

## Task summary

Added atomic `PUBLICATION_SCHEDULE` notifications for chapters that enter `SCHEDULED` or move to a different normalized planned release date while already scheduled. Removed the unused explicit Mangaka schedule and dedicated Editor reschedule vertical slices.

## Architecture path

```text
Schedule UI
-> typed API client
-> API controller
-> IMediator.Send(command)
-> Application handler
-> EF Core scheduling repository
-> Chapter + AuditEvent + Notification in one transaction
-> SQL Server
```

Editorial approval continues through its existing command/repository path and may atomically create both `CHAPTER_DECISION` and `PUBLICATION_SCHEDULE` notifications.

## Behavior changed

- `APPROVED -> SCHEDULED`, `ON_HOLD -> SCHEDULED`, and approval-driven `UNDER_REVIEW -> SCHEDULED` create publication schedule notifications.
- `SCHEDULED -> SCHEDULED` creates notifications only when the normalized planned date changes.
- Date-only changes that remain `DRAFT`, `REVISION_REQUESTED`, or `UNDER_REVIEW` do not notify.
- Recipients are distinct active contributors of the exact series, excluding the initiating actor, ended memberships, and inactive users.
- Notification content includes the series, chapter label/title, and normalized release date.
- Notification Bell entries navigate to `/publication/schedule`.

## Files changed by layer

### Application

- `Application/Common/NotificationConstants.cs` — registered `PUBLICATION_SCHEDULE`.
- `Application/Features/Publication/Schedule/PublicationScheduleNotificationSupport.cs` — centralized trigger classification and notification content.
- Removed the unused Mangaka schedule and Editor reschedule commands/handlers/contracts.

### Infrastructure

- `Infrastructure/Repositories/PublicationScheduleNotificationPersistence.cs` — centralized active-contributor resolution and notification creation.
- `Infrastructure/Repositories/MangakaChapterRepository.Scheduling.cs` — added atomic notification creation to the canonical Mangaka workflow.
- `Infrastructure/Repositories/EditorChapterReviewRepository.Scheduling.cs` — added atomic notification creation to canonical Editor scheduling and editorial approval.
- Removed the unused repository methods for the legacy endpoints.

### API and Web

- Removed unused `/api/mangaka/chapters/{chapterId}/schedule` and `/api/editor/chapters/{chapterId}/reschedule` endpoint slices and typed-client methods.
- `Web/Components/Shared/NotificationBell.razor` — added publication schedule navigation.

## DB/SP impact

None. Existing EF entities, `manga.Notification`, and repository-owned transactions are reused. No migration or stored procedure change was required.

## Verification

### Static checks

- Repository-wide search found zero production references to all removed legacy symbols and request/result types.
- `git diff --check` passed; only working-copy LF/CRLF notices were emitted.
- Static transaction inspection confirms notifications are added before the existing `SaveChangesAsync` and commit in all three active mutation paths.
- The shared predicate normalizes nullable dates with `.Date` before comparing them.
- Recipient query filters exact `SeriesId`, `EndDate == null`, `UserStatusCode == ACTIVE`, excludes the actor, and applies `Distinct()`.

### Build

```text
dotnet build .\MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
Build succeeded: 0 errors, 68 warnings.
```

Warnings match existing nullable, generated Razor, and MudBlazor analyzer categories. No warning originates from the new notification helper, persistence helper, scheduling partials, constants, Bell change, or removed contracts. The warning in `MangakaChapterRepository.cs` is pre-existing and its line moved because the dead method was deleted.

### Manual smoke

Not run. Build and static verification only; no database-backed runtime environment was started.

## Known issues and follow-ups

- Runtime recipient and Bell behavior should be smoke-tested with representative active, inactive, ended, duplicate, actor, and other-series contributor data.
- Transaction rollback on notification insertion failure was verified statically; no artificial production failure hook was added.
- No commit or push was performed.
