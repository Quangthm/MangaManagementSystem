# Session Note — Create Series Draft (Mangaka)

- **Branch:** `feature/Mangaka`
- **Date:** 2026-06-16
- **Stored procedure used:** `manga.usp_Series_Create`

## Task summary

Implement the Mangaka "create a new series draft" use case through the dedicated API
project, and correct the dashboard create modal so it creates **only** a series draft.

Previously the Mangaka dashboard mixed draft creation with proposal submission: it
uploaded a `SERIES_PROPOSAL` document, created a `Series` via EF, and then created a
`SeriesProposal` row — all client-orchestrated from the Razor page through
`ISeriesService` / `ISeriesProposalService` directly (bypassing `manga.usp_Series_Create`
and its permission/contributor/audit logic).

The corrected behavior:

- Creating a draft creates only `manga.Series` with status `PROPOSAL_DRAFT`.
- The optional file upload during draft creation is a **series cover** (`SERIES_COVER`).
- No proposal document is uploaded here, and no `SeriesProposal` row is created.
- Proposal document upload (`SERIES_PROPOSAL`) stays a separate future "Submit Proposal"
  workflow on an existing draft.

## Architecture flow (now matches the skills guide)

```
MangakaDashboard.razor
  -> IMangakaSeriesApiClient / MangakaSeriesApiClient (Web typed client)
  -> POST /api/mangaka/series/drafts (MangakaSeriesController, thin)
  -> ISeriesService.CreateSeriesDraftAsync (Application)
  -> ISeriesRepository.CreateSeriesDraftViaProcAsync (Infrastructure SP wrapper)
  -> manga.usp_Series_Create (SQL Server)
```

The Web project no longer calls `ISeriesService` directly for the create-draft workflow.
(`ISeriesService` is still injected for the unrelated existing read/list, status-change,
and cover-update flows, which are out of scope for this task.)

## Files changed

### Application
- `Interfaces/ISeriesService.cs` — added `CreateSeriesDraftAsync(Guid actorUserId, CreateSeriesDraftDto dto, CancellationToken)`.
- `Services/SeriesService.cs` — implemented `CreateSeriesDraftAsync`; injected `IFileStorageService` + `ILogger`. Validates/normalizes input, generates slug, performs optional cover upload, calls the SP wrapper, and compensates (Cloudinary cleanup) if the SQL workflow fails.
- `DTOs/Manga/CreateSeriesDraftDto.cs` *(new)* — draft input: Title, Synopsis, Genre, optional ContentLanguageCode, optional Slug, optional PublicationFrequencyCode, optional SourceSeriesId, optional cover bytes/name/content-type. No proposal document field.
- `DTOs/Manga/SeriesDraftCreatedDto.cs` *(new)* — result: `SeriesId`, `Title`, `Slug`, `StatusCode`, `CoverFileId`.
- `Common/SlugGenerator.cs` *(new)* — URL-safe slug generation from title + normalization of a user-supplied slug. Strips diacritics, collapses non-alphanumerics to single hyphens, lowercases, trims, caps at 220 chars. No `series_code` reintroduced.

### Domain
- `Interfaces/ISeriesRepository.cs` — added `CreateSeriesDraftViaProcAsync(...)` returning `(Guid newSeriesId, Guid? coverFileResourceId)`.

### Infrastructure
- `Repositories/SeriesRepository.cs` — implemented `CreateSeriesDraftViaProcAsync` using `CommandType.StoredProcedure` against `manga.usp_Series_Create`, with strongly typed `SqlParameter`s (GUIDs as `SqlDbType.UniqueIdentifier`), `DBNull.Value` for optional cover metadata, and captured output parameters `@new_series_id` and `@cover_file_resource_id`. Added `MapSqlException` to translate known SQL errors into friendly `InvalidOperationException` messages:
  - `57301` -> "Only an active Mangaka can create a series draft."
  - `57302` -> incomplete cover metadata message
  - `2627` / `2601` -> duplicate slug/title message
  - `547` -> invalid code/check-constraint message
  - default -> generic safe message
  Raw SQL text is never surfaced.

### API
- `Contracts/CreateSeriesDraftForm.cs` *(new)* — `multipart/form-data` payload (draft fields + optional `IFormFile CoverFile`). No proposal document field.
- `Controllers/MangakaSeriesController.cs` *(new)* — `[Route("api/mangaka/series")]`, thin `POST drafts` endpoint, `[Consumes("multipart/form-data")]`. Reads form fields + optional cover bytes, resolves the actor user id from the transitional `X-Actor-User-Id` header, calls `ISeriesService.CreateSeriesDraftAsync`, returns `201 Created` with `SeriesDraftCreatedDto`. Maps `InvalidOperationException` -> `400` with `ApiErrorResponse`; unexpected -> generic `500`. No Cloudinary/SQL/repository/business logic in the controller.

### Web
- `Services/Api/IMangakaSeriesApiClient.cs` *(new)* — typed client contract.
- `Services/Api/MangakaSeriesApiClient.cs` *(new)* — builds multipart request, sets the `X-Actor-User-Id` header from the passed actor id, parses `ApiErrorResponse` / `ProblemDetails` / `ValidationProblemDetails` into friendly messages (mirrors `RegistrationApiClient`).
- `Program.cs` — registered `AddHttpClient<IMangakaSeriesApiClient, MangakaSeriesApiClient>` using `ApiSettings:BaseUrl`.
- `Components/Pages/Mangaka/MangakaDashboard.razor` — converted the create modal from "New Series Proposal" to "New Series Draft"; removed the proposal document upload and the `ISeriesProposalService` injection; added optional cover upload + content-language select; create now calls `IMangakaSeriesApiClient.CreateDraftAsync`. On success: shows success snackbar with the `PROPOSAL_DRAFT` status, adds the new card to the list, and closes the modal.

## Application method added

`SeriesService.CreateSeriesDraftAsync(Guid actorUserId, CreateSeriesDraftDto dto, CancellationToken)`
-> returns `SeriesDraftCreatedDto`.

## Infrastructure stored procedure wrapper added

`SeriesRepository.CreateSeriesDraftViaProcAsync(...)` -> calls `manga.usp_Series_Create`,
returns `(Guid newSeriesId, Guid? coverFileResourceId)`.

## API endpoint added

`POST /api/mangaka/series/drafts` (multipart/form-data).

## Web API client added

`IMangakaSeriesApiClient` / `MangakaSeriesApiClient`, registered in Web `Program.cs`.

## How the optional cover file is handled

- The cover is optional. If no cover is selected, the Application passes `null` for all six
  cover metadata parameters and the SP creates no `FileResource`.
- If a cover is selected, the Application uploads it via the existing
  `IFileStorageService` (`CloudinaryFileStorageService`), which **uploads to Cloudinary and
  computes the SHA-256 hash but does NOT create a `FileResource` row**. The returned
  metadata (public id, secure url, content type, size, original name, sha256) is then passed
  to `manga.usp_Series_Create`, which creates the `SERIES_COVER` `FileResource` itself
  inside the SQL transaction. This avoids duplicate `FileResource` creation.
- Compensation: if the SQL workflow fails after a successful Cloudinary upload, the
  Application attempts to delete the uploaded Cloudinary asset (`DeleteFileAsync(publicId, "image")`).

## Confirmation: proposal upload removed from draft creation

- The dashboard create modal no longer has a proposal document upload control.
- No `SeriesProposal` is created during draft creation.
- `SERIES_PROPOSAL` purpose is not used anywhere in this workflow.
- `ISeriesProposalService` is no longer injected into the dashboard.

## Transitional auth note

The API does not yet own authentication. The Web host owns the Blazor cookie/session and
reads the logged-in user's id from the `NameIdentifier` claim (existing dashboard pattern).
The typed Web client forwards that id to the API via the `X-Actor-User-Id` request header,
and the controller resolves the actor from that header. This is a documented temporary
server-to-server Web->API pattern, not a final auth design. The user cannot type the actor
id in the UI. A full JWT/shared-cookie auth system is intentionally out of scope here.

## Build result

`dotnet build` — **Build succeeded, 0 errors.** (Pre-existing warnings remain; no new
errors introduced by this task.)

## Remaining follow-up

- Implement a separate **Submit Proposal** workflow for an existing draft:
  upload a `SERIES_PROPOSAL` document, call `manga.usp_SeriesProposal_Submit` (which
  snapshots title/synopsis/genre from the Series row server-side, versions the proposal,
  and transitions the series to `UNDER_EDITORIAL_REVIEW`), surfaced as an action on a
  draft series card/detail rather than on the create modal.
- When API authentication is implemented, replace the transitional `X-Actor-User-Id`
  header with proper authenticated identity.
