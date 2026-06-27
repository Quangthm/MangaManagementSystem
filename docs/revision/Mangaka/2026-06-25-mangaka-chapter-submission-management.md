# Mangaka Chapter Submission Management

## Summary
Implemented Mangaka-side chapter draft and submission management for `manga.Chapter` and `manga.ChapterEditorialReview`.

This session continued from an existing partial state and completed the Mangaka-side workflow for:
- listing chapters for the logged-in Mangaka's series
- creating chapter drafts
- editing draft/revision-requested chapter metadata
- submitting draft/revision-requested chapters for Tantou Editor review
- viewing latest editorial review feedback
- scheduling approved chapters with a planned release date

## Scope
Implemented in scope:
- Mangaka chapter list
- Create chapter draft metadata
- Edit chapter draft metadata
- Submit DRAFT / REVISION_REQUESTED chapter for editorial review
- Track latest editor review status/feedback
- Schedule APPROVED chapter with `planned_release_date`

Out of scope:
- Editor review action UI
- Editor approval / revision / cancel implementation
- Chapter page upload/workspace flow
- Release publishing job
- Strict publication frequency enforcement
- Double-page spread splitting/cropping
- Mangaka dashboard/navigation refactor

## Business rules implemented
- Only active Mangaka contributors can manage chapter drafts for their series.
- Create chapter creates status `DRAFT`.
- Edit allowed only when status is `DRAFT` or `REVISION_REQUESTED`.
- `chapter_number_label` is required.
- Duplicate `chapter_number_label` per series is rejected safely.
- Submit for review allowed only when status is `DRAFT` or `REVISION_REQUESTED`.
- Submit sets status to `UNDER_REVIEW`.
- `UNDER_REVIEW`, `APPROVED`, `SCHEDULED`, `RELEASED`, `CANCELLED`, `ON_HOLD` cannot be edited by Mangaka.
- `REVISION_REQUESTED` chapters can be edited and resubmitted.
- Latest review feedback can be viewed from `manga.ChapterEditorialReview`.
- `APPROVED` chapters can be scheduled.
- Scheduling requires `planned_release_date` and it must not be in the past.
- Scheduling sets status to `SCHEDULED`.
- Strict publication frequency validation is deferred.

## Architecture path
Blazor Web
→ typed API client
→ API controller
→ MediatR Application command/query handlers
→ Infrastructure repository with EF Core transaction
→ SQL Server

## Stored procedure decision
No stored procedures were added.

Reason:
- there is currently no stored procedure for this workflow
- project architecture already allows EF transaction-backed workflow implementation for new areas
- this session used Application handlers + Infrastructure EF transaction as the preferred path

Future hardening may move chapter submit/review/schedule transitions into stored procedures if the team later wants SP-owned workflow transitions.

## Files changed
### Domain
- `MangaManagementSystem/src/MangaManagementSystem.Domain/Entities/Chapter.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Domain/Entities/ChapterEditorialReview.cs`

### Application DTOs / interfaces / handlers
- `MangaManagementSystem/src/MangaManagementSystem.Application/DTOs/Manga/MangakaChapterDtos.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Interfaces/IMangakaChapterRepository.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Chapters/Queries/GetMyMangakaChapters/GetMyMangakaChaptersQuery.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Chapters/Queries/GetMyMangakaChapters/GetMyMangakaChaptersQueryHandler.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Chapters/Queries/GetMangakaSeriesChapters/GetMangakaSeriesChaptersQuery.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Chapters/Queries/GetMangakaSeriesChapters/GetMangakaSeriesChaptersQueryHandler.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Chapters/Commands/CreateChapterDraft/CreateChapterDraftCommand.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Chapters/Commands/CreateChapterDraft/CreateChapterDraftCommandHandler.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Chapters/Commands/UpdateChapterDraft/UpdateChapterDraftCommand.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Chapters/Commands/UpdateChapterDraft/UpdateChapterDraftCommandHandler.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Chapters/Commands/SubmitChapterForReview/SubmitChapterForReviewCommand.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Chapters/Commands/SubmitChapterForReview/SubmitChapterForReviewCommandHandler.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Chapters/Commands/ScheduleApprovedChapter/ScheduleApprovedChapterCommand.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Chapters/Commands/ScheduleApprovedChapter/ScheduleApprovedChapterCommandHandler.cs`

### Infrastructure
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/DependencyInjection.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Persistence/Configurations/ChapterConfiguration.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Persistence/Configurations/ChapterEditorialReviewConfiguration.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Repositories/MangakaChapterRepository.cs`

### API
- `MangaManagementSystem/src/MangaManagementSystem.API/Contracts/MangakaChapterRequests.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/MangakaChaptersController.cs`

### Web
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/IMangakaChapterApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/MangakaChapterApiClient.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Mangaka/Chapters.razor`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Program.cs`

## Endpoints added
- `GET /api/mangaka/chapters`
- `GET /api/mangaka/series/{seriesId}/chapters`
- `POST /api/mangaka/chapters`
- `PUT /api/mangaka/chapters/{chapterId}`
- `POST /api/mangaka/chapters/{chapterId}/submit-review`
- `POST /api/mangaka/chapters/{chapterId}/schedule`

## DTOs added
- `MangakaChapterListItemDto`
- `ChapterEditorialReviewSummaryDto`
- `CreateChapterDraftRequest`
- `UpdateChapterDraftRequest`
- `ScheduleApprovedChapterRequest`

## Application handlers
Added MediatR handlers for:
- `GetMyMangakaChaptersQuery`
- `GetMangakaSeriesChaptersQuery`
- `CreateChapterDraftCommand`
- `UpdateChapterDraftCommand`
- `SubmitChapterForReviewCommand`
- `ScheduleApprovedChapterCommand`

Validation added in handlers/includes:
- `ActorUserId` must not be empty
- `SeriesId` / `ChapterId` must not be empty
- `ChapterNumberLabel` must be provided
- `PlannedReleaseDate` must be provided and not in the past

## Infrastructure persistence approach
Used EF Core repository + transaction for writes.

Write operations re-check inside the repository:
- actor is an active Mangaka contributor of the series
- chapter exists
- chapter status is eligible for the requested action
- duplicate chapter number label per series is blocked

Write operations use one `SaveChangesAsync` per operation.

Unique constraint violations are mapped to a safe message:
- `A chapter with this number already exists for this series.`

Latest review summary is loaded from `manga.ChapterEditorialReview` including reviewer display name, comments, markup file URL, and reviewed time.

## UI route/page
Added new route:
- `/mangaka/chapters`

UI supports:
- series filter
- status filter
- create draft dialog
- edit draft dialog
- submit for review action
- latest review feedback display
- schedule approved chapter dialog
- status chips for all chapter statuses
- read-only `UNDER_REVIEW` state message

## Build result
Command run:
- `dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental`

Result:
- Build succeeded
- `0` errors
- `43` warnings

This matches the existing warning baseline.

## Runtime smoke result
Not fully runtime-tested in this session.

Build and code-path review support these expected flows, but the following remain manual smoke items:
- [ ] Mangaka can open `/mangaka/chapters`
- [ ] Mangaka can create chapter draft
- [ ] Duplicate `chapter_number_label` in same series is rejected safely
- [ ] Mangaka can edit DRAFT chapter metadata
- [ ] Mangaka can submit DRAFT chapter for review
- [ ] Submitted chapter becomes `UNDER_REVIEW`
- [ ] `UNDER_REVIEW` chapter cannot be edited
- [ ] `REVISION_REQUESTED` chapter shows latest feedback and can be edited/resubmitted
- [ ] `APPROVED` chapter can select `planned_release_date`
- [ ] Scheduling sets status to `SCHEDULED`
- [ ] Existing `ReviewSubmissions`, `ImagePreview`, and `Quick Select` still work

## Known limitations
- Strict publication frequency validation for `planned_release_date` is not enforced yet.
- Editor-side chapter review actions are not implemented in this session.
- Page workflow remains workspace/team responsibility.
- Mangaka dashboard/navigation refactor remains separate.
- Double-page spread splitting/cropping remains workspace/page-processing responsibility.
- Audit action codes were not implemented/enforced for this workflow because action-code constraints/support were not confirmed safe for this session.
- The new route was added without broader Mangaka dashboard refactor.

## Follow-ups
- Add strict publication frequency validation for `planned_release_date`.
- Implement editor-side chapter review actions if not already present elsewhere.
- Keep chapter page workflow with the responsible workspace/page team.
- Consider a future Mangaka dashboard/navigation refactor separately.
- Keep double-page spread splitting/cropping with workspace/page-processing flows.
- If the team wants stronger DB-owned workflow transitions, consider moving submit/review/schedule transitions into stored procedures.
- Confirm audit action code support and add workflow audit events safely if desired.
