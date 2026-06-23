# Session Note — Manage Series Contributors

- **Branch:** `feature/Mangaka`
- **Date:** `2026-06-22`
- **Scope:** `Mangaka contributor management`
- **Applies to:** `feature/Mangaka`
- **Related workflow:** `Mangaka series contributor management`

## Task summary

Implemented a dedicated Mangaka page at `/mangaka/contributors` that lets an active Mangaka contributor:
- view contributor history for their own series,
- filter by series, role, status, and search text,
- add active Assistant users who are not already active contributors of the selected series,
- end active Assistant contributors while preserving contributor history.

This follows the dedicated page + sidebar navigation pattern already used by `/mangaka/review-submissions`.

## Important decisions

### Applies to current/main branch
- Reused existing `GET /api/mangaka/series/my-series` / `SeriesDto` for the series selector. No extra series-options endpoint was needed because `SeriesDto` already contains `SeriesId` and `Title`.
- Kept Current Series and Series Proposals dashboard behavior unchanged.
- Added a focused contributor-management repository abstraction in `Application.Interfaces` rather than reusing/refactoring the legacy generic CRUD contributor service.
- Reused existing `manga.usp_SeriesContributor_Add` for add-assistant writes, but only after Application/C# validation.
- Created a new SQL script for `manga.usp_SeriesContributor_EndAssistant` but did **not** apply it automatically to the database.

### Deferred / out of scope
- Mangaka contributor add/remove/self-removal/co-Mangaka removal
- Tantou Editor add/edit/remove
- contributor role editing
- contributor specialization
- `/mangaka/series` route migration
- `/mangaka/proposals` route migration
- Quick Select / batch task creation
- large MangakaDashboard refactor

## Architecture flow

```text
Razor Page
  -> typed Web API client
  -> API controller
  -> IMediator.Send(command/query)
  -> Application handler
  -> Infrastructure repository/SP wrapper
  -> SQL Server stored procedure / EF read query
```

## Files changed

### Web
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Mangaka/ManageContributors.razor` — new dedicated contributor management page using `<MangakaLayout>`.
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Mangaka/MangakaDashboard.razor` — added `Manage Contributors` sidebar item and `HandleNavClick("contributors")` route to `/mangaka/contributors`.
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/IMangakaSeriesContributorApiClient.cs` — new typed client contract.
- `MangaManagementSystem/src/MangaManagementSystem.Web/Services/Api/MangakaSeriesContributorApiClient.cs` — new typed client implementation.
- `MangaManagementSystem/src/MangaManagementSystem.Web/Program.cs` — registered typed contributor API client.

### API
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/Mangaka/MangakaSeriesContributorController.cs` — thin contributor-management controller.

### Application
- `MangaManagementSystem/src/MangaManagementSystem.Application/DTOs/Manga/MangakaContributorDtos.cs` — contributor page DTOs and request contracts.
- `MangaManagementSystem/src/MangaManagementSystem.Application/Interfaces/ISeriesContributorManagementRepository.cs` — focused contributor-management repository abstraction.
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Contributors/Queries/GetSeriesContributors/GetSeriesContributorsQuery.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Contributors/Queries/GetSeriesContributors/GetSeriesContributorsQueryHandler.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Contributors/Queries/SearchEligibleAssistants/SearchEligibleAssistantsQuery.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Contributors/Queries/SearchEligibleAssistants/SearchEligibleAssistantsQueryHandler.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Contributors/Commands/AddAssistantContributor/AddAssistantContributorCommand.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Contributors/Commands/AddAssistantContributor/AddAssistantContributorCommandHandler.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Contributors/Commands/EndAssistantContributor/EndAssistantContributorCommand.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Mangaka/Contributors/Commands/EndAssistantContributor/EndAssistantContributorCommandHandler.cs`

### Infrastructure
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Repositories/SeriesContributorRepository.cs` — EF read queries + SP wrappers for contributor management.
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/DependencyInjection.cs` — registered `ISeriesContributorManagementRepository`.

### Domain
- `MangaManagementSystem/src/MangaManagementSystem.Domain/Interfaces/ISeriesContributorRepository.cs` — reverted to an unused placeholder comment because the focused contributor-management abstraction had to live in Application, not Domain, to avoid Domain -> Application DTO dependency.

### Database / SQL
- `MangaManagementSystem_SeriesContributor_EndAssistant.sql` — new SQL script for `manga.usp_SeriesContributor_EndAssistant`.

### Documentation
- `docs/revision/_CURRENT_SESSION.md` — live session note.
- `docs/revision/Mangaka/2026-06-22-manage-series-contributors.md` — this final handoff.

## CQRS / MediatR changes

- **Commands added:**
  - `AddAssistantContributorCommand`
  - `EndAssistantContributorCommand`
- **Queries added:**
  - `GetSeriesContributorsQuery`
  - `SearchEligibleAssistantsQuery`
- **Handlers added:**
  - `AddAssistantContributorCommandHandler`
  - `EndAssistantContributorCommandHandler`
  - `GetSeriesContributorsQueryHandler`
  - `SearchEligibleAssistantsQueryHandler`
- **Validators added/changed:** explicit validation inside handlers; no separate FluentValidation types added.
- **Controller dispatch pattern:** `IMediator.Send(...)`

## API endpoints added/changed

- `GET /api/mangaka/series/{seriesId}/contributors` — returns all contributor rows for a series where actor is active Mangaka contributor.
- `GET /api/mangaka/series/{seriesId}/contributors/eligible-assistants?search={search}` — returns ACTIVE Assistant users not currently active contributors of the selected series.
- `POST /api/mangaka/series/{seriesId}/contributors/assistants` — adds an Assistant contributor.
- `POST /api/mangaka/series/{seriesId}/contributors/assistants/{assistantUserId}/end` — ends an Assistant contributor row.

## Web API clients added/changed

- `IMangakaSeriesContributorApiClient` / `MangakaSeriesContributorApiClient`
  - `GetContributorsAsync`
  - `SearchEligibleAssistantsAsync`
  - `AddAssistantAsync`
  - `EndAssistantAsync`

## Database / stored procedure behavior

- **Stored procedure used for add:** `manga.usp_SeriesContributor_Add`
- **Stored procedure added as script only for end:** `manga.usp_SeriesContributor_EndAssistant`
- **Was the new EndAssistant SP applied to the database?** No. Script created only. Not run automatically.
- **Transaction owner:** SQL stored procedures
- **Audit behavior:**
  - add uses existing `SERIES_CONTRIBUTOR_ADDED` audit path in `usp_SeriesContributor_Add`
  - end script writes `SERIES_CONTRIBUTOR_ASSISTANT_ENDED`
- **Rollback/compensation behavior:** SQL transaction rollback via `SET XACT_ABORT ON` + `TRY/CATCH` in the new SP script.

## Application validation

### Add Assistant validation
Before calling `manga.usp_SeriesContributor_Add`, Application validates:
- actorUserId required
- seriesId required
- assistantUserId required
- actor is active Mangaka contributor of target series
- target user exists
- target user status is `ACTIVE`
- target user role is `Assistant`
- target user is not already an active contributor of the selected series
- historical ended rows do not block re-adding

### End Assistant validation
Before calling `manga.usp_SeriesContributor_EndAssistant`, Application validates:
- actorUserId required
- seriesId required
- assistantUserId required
- reason required
- reason max length 500
- actor is active Mangaka contributor of target series
- target exists
- target role is `Assistant`
- target is currently active contributor of the selected series
- target has no `ASSIGNED` or `UNDER_REVIEW` tasks for the same series
- user-safe blocking message: `This assistant has active tasks. Reassign or cancel their tasks before removing them from the series.`

## Infrastructure read/write behavior

### Reads
- `GetSeriesContributorsAsync` uses `SeriesContributors + Users + Roles + Series` EF projection with `AsNoTracking()`.
- Active logic: `EndDate == null && User.StatusCode == "ACTIVE"`
- Former logic: `EndDate != null`
- `SearchEligibleAssistantContributorsAsync` excludes only currently active contributor rows (`EndDate == null`), not historical ended rows.
- `HasActiveTasksForSeriesAsync` derives task-series relationship through:
  `Task -> PageRegions -> ChapterPageVersion -> ChapterPage -> Chapter -> Series`

### Writes
- add uses existing `manga.usp_SeriesContributor_Add`
- end uses new script target `manga.usp_SeriesContributor_EndAssistant`

## UI behavior changed

- Added `/mangaka/contributors` page using `<MangakaLayout>`.
- Added `Back to Mangaka Dashboard` button.
- Added stat cards:
  - My Series
  - Active Contributors
  - Active Assistants
  - Former Contributors
- Added filter bar:
  - series selector
  - search box
  - role filter
  - status filter (Active / Former / All)
- Contributor table shows:
  - series title
  - display name
  - username / email
  - role chip
  - status chip
  - start date
  - end date
  - actions
- `Add Assistant` button opens Assistant search dialog.
- `End` button appears only for active Assistant rows.
- Mangaka and Tantou Editor rows are read-only.
- Former rows are read-only.
- Sidebar navigation item `Manage Contributors` added to `/mangaka` using the same route-navigation pattern as `Assistant Review`.

## Validation and build result

- **Command run:** `dotnet build MangaManagementSystem\MangaManagementSystem.sln --no-incremental`
- **Result:** Build succeeded.
- **Error count:** 0
- **Warning count:** 57
- **Changed-file warnings:** 0 new warnings from changed/new contributor-feature files
- **Previous CS8602 warnings still exist?** Yes, pre-existing CS8602 warnings still exist in `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Repositories/ChapterPageTaskRepository.cs` from other methods.
- **Did this feature introduce any new warnings?** No.
- **Runtime tests:** not run

## Add/remove smoke-test readiness

- **Can add assistant be smoke-tested immediately?** Yes, assuming the database already has the existing `manga.usp_SeriesContributor_Add` procedure available.
- **Can remove assistant be smoke-tested immediately?** No, not unless the new `MangaManagementSystem_SeriesContributor_EndAssistant.sql` script is first applied to the target database.
- **Does remove assistant require applying the new SP?** Yes.

## Known issues / problems discovered

- `MangaManagementSystem_SeriesContributor_EndAssistant.sql` was created only as a script and was **not** applied automatically. Runtime remove-assistant flow depends on DB deployment of that procedure.
- `MangaManagementSystem/src/MangaManagementSystem.Domain/Interfaces/ISeriesContributorRepository.cs` was initially created in the wrong layer shape and then neutralized to preserve boundaries. It is not used by the new feature path.
- Runtime smoke was not run.

## Manual test checklist

### Navigation
- [ ] `/mangaka` sidebar shows `Manage Contributors`
- [ ] Clicking it navigates to `/mangaka/contributors`
- [ ] Page uses `MangakaLayout`
- [ ] Back button returns to `/mangaka`
- [ ] Current Series and Series Proposals behavior unchanged
- [ ] Assistant Review navigation still works

### Contributor read/list
- [ ] Page loads only series where actor is active Mangaka contributor
- [ ] Series selector lists actor's series from existing `GetMySeries` API
- [ ] Contributor list loads for selected series
- [ ] Active contributors shown by default
- [ ] Search filters by display name / username / email / role / series title
- [ ] Role filter works
- [ ] Status filter Active / Former / All works
- [ ] Mangaka contributors are read-only
- [ ] Tantou Editor contributors are read-only
- [ ] Former contributors are read-only

### Add Assistant
- [ ] Add Assistant dialog opens for selected series
- [ ] Search returns only ACTIVE Assistant users
- [ ] Search excludes users already active contributors of selected series
- [ ] Search allows users with only historical ended rows
- [ ] Cannot submit without selecting assistant
- [ ] Successful add refreshes contributor list
- [ ] Added assistant no longer appears in eligible search
- [ ] Duplicate active contributor add is rejected with safe error

### Remove Assistant
- [ ] Apply `MangaManagementSystem_SeriesContributor_EndAssistant.sql` to DB first
- [ ] End button appears only for active Assistant contributors
- [ ] End button does not appear for Mangaka contributors
- [ ] End button does not appear for Tantou Editor contributors
- [ ] End button does not appear for former contributors
- [ ] End dialog requires reason
- [ ] Successful end sets `end_date` without deleting row
- [ ] Successful end refreshes contributor list
- [ ] Former contributor visible in Former / All filters
- [ ] Removed assistant can reappear in eligible assistant search
- [ ] End is blocked if assistant has `ASSIGNED` or `UNDER_REVIEW` tasks on the series

### Security / scope
- [ ] Mangaka cannot view contributors for other Mangaka's series
- [ ] Mangaka cannot add assistant to another Mangaka's series
- [ ] Mangaka cannot remove assistant from another Mangaka's series
- [ ] API rejects missing/invalid actor user id
- [ ] API rejects non-Mangaka or inactive actor
- [ ] API rejects adding inactive Assistant
- [ ] API rejects adding non-Assistant user
- [ ] API rejects ending non-Assistant contributors

## Follow-up runtime fix — EF ordering translation

### Root cause

`GET /api/mangaka/series/{seriesId}/contributors` returned HTTP 400 at runtime because EF Core could not translate the LINQ query in `SeriesContributorRepository.GetSeriesContributorsAsync()`. The query projected directly into `SeriesContributorListItemDto` and then ordered by `SeriesContributorListItemDto.IsActive`. Because `IsActive` was a DTO property constructed inside the projection, EF could not translate the `OrderByDescending` expression into SQL.

### File changed

- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Repositories/SeriesContributorRepository.cs`

### Query fix summary

The contributor list query was changed so ordering happens **before** DTO projection using server-translatable scalar expressions:

- old shape:
  - `select new SeriesContributorListItemDto(...)`
  - `.OrderByDescending(x => x.IsActive).ThenBy(x => x.DisplayName)`

- new shape:
  - `orderby (sc.EndDate == null && u.StatusCode == "ACTIVE") descending, u.DisplayName`
  - `select new SeriesContributorListItemDto(...)`

This preserves behavior without using `AsEnumerable()`:
- active contributors first
- active = `EndDate == null && user.StatusCode == "ACTIVE"`
- former contributors still included
- all contributor roles still returned
- query remains EF-translatable and SQL-executed

### Build result

- Command: `dotnet build MangaManagementSystem\MangaManagementSystem.sln --no-incremental`
- Result: Build succeeded
- Errors: 0
- Warnings: 57 (baseline unchanged)

### Manual smoke status

- Runtime browser smoke was **not run in this environment**.
- Requested manual smoke checklist remains:
  - [ ] Open `/mangaka/contributors`
  - [ ] Select a series
  - [ ] Contributor list loads without API 400
  - [ ] Active contributors appear before former contributors
  - [ ] Filters still work

## Remaining follow-up tasks

- Deploy/apply `MangaManagementSystem_SeriesContributor_EndAssistant.sql` to the local/dev database before runtime remove-assistant smoke.
- Optionally clean up the unused placeholder file `Domain/Interfaces/ISeriesContributorRepository.cs` in a separate refactor if desired.
- If runtime UX polish is needed later, improve typed-client error extraction to parse `ApiErrorResponse.Message` instead of surfacing raw JSON strings in snackbar messages.

## Resume prompt for next AI agent

On branch `feature/Mangaka`, the contributor-management feature is implemented and build-clean with the baseline 57 warnings. Before runtime testing remove-assistant flow, apply `MangaManagementSystem_SeriesContributor_EndAssistant.sql` to the database. Inspect these primary files first:
- `Web/Components/Pages/Mangaka/ManageContributors.razor`
- `API/Controllers/Mangaka/MangakaSeriesContributorController.cs`
- `Application/Features/Mangaka/Contributors/...`
- `Infrastructure/Repositories/SeriesContributorRepository.cs`
- `MangaManagementSystem_SeriesContributor_EndAssistant.sql`
Do not refactor the Mangaka dashboard shell or expand scope into Mangaka/Tantou Editor contributor management.
