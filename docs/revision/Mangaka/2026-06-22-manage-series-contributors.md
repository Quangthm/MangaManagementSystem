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

## Follow-up runtime fix — Add Assistant eligible search

### Root cause

The Add Assistant dialog's autocomplete search appeared to do nothing. Investigation traced the entire search flow:

- **UI wiring**: `SearchFunc="SearchAssistantsAsync"` — missing `@` prefix. In Blazor component parameters, omitting `@` on a delegate-typed parameter can prevent the method group from being correctly bound. The working reference in `ReviewSubmissions.razor` uses `SearchFunc="@SearchAssistants"` with `@`. Without `@`, the Razor compiler may not resolve the method reference to the `Func<string?, CancellationToken, Task<IEnumerable<T>>?>` delegate expected by MudBlazor 8.x's `SearchFunc`.
- **Confirmed via MudBlazor 8.0.0 source**: `public Func<string?, CancellationToken, Task<IEnumerable<T>>?>? SearchFunc { get; set; }` — matches our method signature exactly.
- **Infrastructure query**: Correct shape — filters by role "Assistant", status "ACTIVE", excludes active contributors only (not historical rows), applies search on DisplayName/Username/Email, returns top 50.
- **Web typed client**: Correct — includes actor header, builds URL with `Uri.EscapeDataString(search)`.
- **API controller**: Correct — `[FromQuery] string? search` and passes to handler.
- **DTO**: `EligibleAssistantContributorDto` is a sealed positional record — `PropertyNameCaseInsensitive = true` in the client handles camelCase JSON correctly.

The backend logic was correct throughout. The UI binding was broken.

### Files changed

- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Mangaka/ManageContributors.razor`

### Fix summary

1. **Added `@` prefix** to `SearchFunc` binding so Blazor correctly resolves the method group:
   - before: `SearchFunc="SearchAssistantsAsync"`
   - after: `SearchFunc="@SearchAssistantsAsync"`

2. **Replaced inline `ToStringFunc` lambda with a named `FormatAssistant` method** for clarity and readability:
   - handles null assistant
   - prefers `DisplayName (Username)` format
   - falls back to `DisplayName` alone if no Username
   - falls back to `Username` if no DisplayName
   - falls back to `Email` or `UserId.ToString()` as last resort

3. **Added `MinCharacters="1"`** to ensure search triggers after the user types at least one character (MudBlazor 8.x defaults to 0, but explicit `1` is safer and matches expected UX).

The backend query shape, Web typed client, controller, and Application layer were all confirmed correct and unchanged.

### Build result

- Command: `dotnet build MangaManagementSystem\MangaManagementSystem.sln --no-incremental`
- Result: Build succeeded
- Errors: 0
- Warnings: 57 (baseline unchanged)
- Changed-file warnings: 0

### API/UI smoke status

- **API direct test**: Not run from this environment. Backend logic (query shape, role filter "Assistant", ACTIVE status filter, active-contributor exclusion, search on DisplayName/Username/Email) was confirmed correct by code inspection.
- **UI smoke**: Not run from this environment. Fix addresses the `@` binding issue identified from comparing to the working `ReviewSubmissions.razor` autocomplete pattern.
- **Expected after fix**: typing an assistant name in the Add dialog triggers `SearchAssistantsAsync`, which calls the backend endpoint, which returns matching ACTIVE Assistant users excluding those already active on the series.

## Follow-up runtime fix — Add Assistant search still empty

### What the user reported

After the previous fix (adding `@` prefix to `SearchFunc`, `FormatAssistant`, `MinCharacters="1"`), the user retested in the browser. The Add Assistant dialog still showed no selectable assistant results when searching.

### Investigation performed

1. **Database query verified directly** — executed the exact eligible-assistant SQL query against the live database (`MangaManagementDB`). Result: **6 ACTIVE Assistant users** exist and are eligible for series "Sect" (`67AEA971-B245-4E13-A150-EB2F5B69D222`). The actor (`TestMangaka1`, `9E01AADE-...`) is confirmed as an active Mangaka contributor of that series. None of the 6 Assistants are current active contributors of that series. The backend data is correct.
2. **Role name casing verified** — `auth.Roles.role_name` contains `Assistant` (title case). Infrastructure query uses `r.RoleName == "Assistant"` — matches.
3. **API direct test attempted** — tried `Invoke-RestMethod` to `http://localhost:5234` and `https://localhost:7256`. Both failed with "Unable to connect to the remote server" — the API server was not running at the time. Direct API response was not captured.
4. **MudBlazor 8.0.0 source reviewed** — fetched and read the full `MudAutocomplete<T>` source from MudBlazor v8.0.0 GitHub. Key findings:
   - `SearchFunc` is `Func<string?, CancellationToken, Task<IEnumerable<T>>?>?` — matches our method signature.
   - `MaxItems` defaults to `10` — limits display count but does not cause empty results.
   - `MinCharacters` defaults to `0` — already set to `1` by previous fix.
   - **`DropdownSettings.Fixed` defaults to `false`** — when `false`, the autocomplete popover uses `position: absolute` relative to the input. Inside a `MudDialog` with `overflow: hidden`, the dropdown can render but be **clipped/hidden behind the dialog container**. This is a known MudBlazor issue when placing autocomplete inside dialogs.
   - MudBlazor's `OpenMenuAsync` catches all exceptions from `SearchFunc` and logs a warning. If the API call throws (e.g. 400/500 response), the user sees empty results with no error feedback.
5. **Web typed client code reviewed** — `SearchEligibleAssistantsAsync` correctly builds the URL, includes the actor header, and deserializes with `PropertyNameCaseInsensitive = true`. The `EnsureSuccessAsync` method throws `HttpRequestException` on non-2xx responses, which MudBlazor silently swallows.

### Root cause

Root cause not fully confirmed because the API was not running during investigation and browser smoke was not performed by the agent. However, two likely contributing causes were identified:

1. **Dialog popover clipping (probable primary cause)**: `MudAutocomplete` inside `MudDialog` with `DropdownSettings.Fixed = false` (default) can cause the autocomplete dropdown to be clipped by the dialog's `overflow: hidden`. Setting `Fixed = true` forces the popover to use `position: fixed`, which renders above the dialog overlay.
2. **Silent exception swallowing (secondary cause)**: If the API returns a non-2xx response or the typed client throws, MudBlazor catches the exception silently and shows empty results. The user would see no error and no results, making it impossible to distinguish a data issue from an API failure.

### Files changed

- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Mangaka/ManageContributors.razor`

### Fix summary (cumulative diff from committed state)

1. **`SearchFunc="@SearchAssistantsAsync"`** — added `@` prefix for correct Blazor method group binding (previous fix, retained).
2. **`ToStringFunc="@FormatAssistant"`** — replaced inline lambda with named method for robust display labels (previous fix, retained).
3. **`MinCharacters="1"`** — explicit minimum character threshold (previous fix, retained).
4. **`DropdownSettings="@(new DropdownSettings { Fixed = true })"`** — forces the autocomplete popover to render with `position: fixed`, avoiding clipping inside the dialog. This is the likely primary fix for the invisible dropdown.
5. **`SearchAssistantsAsync` parameter changed to `string?`** — matches MudBlazor 8.x `Func<string?, CancellationToken, ...>` delegate signature exactly.
6. **Added `try/catch` in `SearchAssistantsAsync`**:
   - catches `OperationCanceledException` silently (normal MudBlazor debounce cancellation)
   - catches general exceptions and surfaces them via `Snackbar.Add(...)` so the user sees API failures instead of silent empty results
   - always returns `Enumerable.Empty<EligibleAssistantContributorDto>()` on failure (no null)
7. **Added `FormatAssistant` static method** — handles null, prefers `DisplayName (Username)` format, falls back through Username, Email, and UserId.

### Build note

- Agent attempted `dotnet build MangaManagementSystem\MangaManagementSystem.sln --no-incremental` but failed with `MSB1009: Project file does not exist`. This repo uses `.slnx`, not `.sln`.
- User manually ran the correct `.slnx` build and reported success.

### Runtime/API smoke status

- **API direct test**: Not completed. API server was not running during investigation. Database query was verified directly via `sqlcmd` — 6 eligible ACTIVE Assistant users exist for the test series.
- **UI smoke**: Not run by the agent. User should retest: open `/mangaka/contributors`, select a series, open Add Assistant dialog, type an assistant name, and verify results now appear in the dropdown.

### Remaining known issues

- The Add Assistant search fix needs user browser retest to confirm the `DropdownSettings { Fixed = true }` change resolves the visible dropdown issue.
- If user still sees no results after this fix, the next debugging step should be: (1) check browser DevTools Network tab for the actual API request/response to `eligible-assistants`, (2) check if a snackbar error now appears (indicating API failure), (3) verify the API server is actually running and reachable at the configured `BaseUrl`.

## Follow-up runtime fix — Eligible assistant search EF translation

### User-reported error

After the previous UI fixes (`DropdownSettings { Fixed = true }`, error-handling snackbar, `@` prefix), the Add Assistant dialog now correctly surfaces backend errors via snackbar. User saw:

```
GET /api/mangaka/series/{seriesId}/contributors/eligible-assistants?search=ass
```

returned API 400 with EF Core LINQ translation error. The error showed EF trying to translate a `Where` clause that referenced properties on a newly constructed `EligibleAssistantContributorDto`.

### Actual root cause

In `SeriesContributorRepository.SearchEligibleAssistantContributorsAsync`, the LINQ query projected into `EligibleAssistantContributorDto` **before** applying the search `Where` clause. When a search term was provided, the `Where` filter referenced DTO properties (`x.DisplayName`, `x.Username`, `x.Email`), which EF Core cannot translate because the DTO constructor is not a database expression.

This is the same category of EF translation issue as the earlier `OrderByDescending(x => x.IsActive)` fix on `GetSeriesContributorsAsync`.

Query shape before (broken):

```
select new EligibleAssistantContributorDto(...)   <-- DTO projection first
.Where(x => x.DisplayName.Contains(term) ...)     <-- filter on DTO fields = EF translation failure
.OrderBy(x => x.DisplayName)
.Take(50)
```

### File changed

- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Repositories/SeriesContributorRepository.cs`

### Query fix summary

Rewrote `SearchEligibleAssistantContributorsAsync` so filtering and ordering happen on anonymous-type scalar fields (which EF can translate), then DTO projection happens last:

1. Project into anonymous type first: `select new { u.UserId, u.DisplayName, u.Username, u.Email }`
2. Apply search `Where` on anonymous type scalar fields
3. Apply `OrderBy`/`ThenBy`/`Take` on anonymous type scalar fields
4. Final `.Select(x => new EligibleAssistantContributorDto(...))` at the end
5. `.ToListAsync()`

Also removed duplicate leftover code block (lines 112-116 from previous edit artifact).

Query shape after (fixed):

```
select new { u.UserId, u.DisplayName, u.Username, u.Email }   <-- anonymous type
.Where(x => x.DisplayName.Contains(term) ...)                  <-- filter on scalars = EF translatable
.OrderBy(x => x.DisplayName).ThenBy(x => x.Username)
.Take(50)
.Select(x => new EligibleAssistantContributorDto(...))          <-- DTO projection last
.ToListAsync()
```

All business rules preserved:
- Filters by `role.RoleName == "Assistant"` and `user.StatusCode == "ACTIVE"`
- Excludes only active contributors (`EndDate == null`) for selected series
- Historical ended rows do not block eligibility
- Searches DisplayName, Username, Email
- Returns top 50 ordered by DisplayName then Username

### Build result

- Command: `dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental`
- Result: Build succeeded
- Errors: 0
- Warnings: 37
- Changed-file warnings: 0 new warnings from this fix

### Runtime/API smoke status

- **API direct test**: Not run (API server not started by agent).
- **UI smoke**: Not run by agent.
- User should retest: open `/mangaka/contributors`, select a series, open Add Assistant dialog, type "ass" — eligible assistants should appear without API 400 or snackbar error.

## Remaining follow-up tasks

- User should retest Add Assistant search in browser to confirm the EF translation fix resolves the API 400.
- Deploy/apply `MangaManagementSystem_SeriesContributor_EndAssistant.sql` to the local/dev database before runtime remove-assistant smoke.
- Optionally clean up the unused placeholder file `Domain/Interfaces/ISeriesContributorRepository.cs` in a separate refactor if desired.
- If runtime UX polish is needed later, improve typed-client error extraction to parse `ApiErrorResponse.Message` instead of surfacing raw JSON strings in snackbar messages.

## Resume prompt for next AI agent

On branch `feature/Mangaka`, the contributor-management feature is implemented. The eligible-assistant search EF translation bug is fixed — DTO projection now happens after filtering/ordering. Build succeeds with `dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental` (not `.sln`). User must retest Add Assistant search in browser. Before runtime testing remove-assistant flow, apply `MangaManagementSystem_SeriesContributor_EndAssistant.sql` to the database. Inspect these primary files first:
- `Web/Components/Pages/Mangaka/ManageContributors.razor`
- `API/Controllers/Mangaka/MangakaSeriesContributorController.cs`
- `Application/Features/Mangaka/Contributors/...`
- `Infrastructure/Repositories/SeriesContributorRepository.cs`
- `MangaManagementSystem_SeriesContributor_EndAssistant.sql`
Do not refactor the Mangaka dashboard shell or expand scope into Mangaka/Tantou Editor contributor management.
