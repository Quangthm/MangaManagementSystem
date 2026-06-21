# Session Note — Create Series Draft: MediatR/CQRS Migration

- **Branch:** `feature/Mangaka`
- **Date:** 2026-06-17

---

## Task Summary

Migrated Create Series Draft from the transitional `ISeriesService.CreateSeriesDraftAsync` path
to the established MediatR/CQRS architecture. All four Mangaka series workflows in
`MangakaSeriesController` now use `IMediator.Send(command)`. `ISeriesService` has been removed
from the controller but remains in the solution for other consumers.

Added backend cover file validation to the new handler (type, extension, and size) before any
Cloudinary upload is attempted.

---

## Old Flow

```
MangakaDashboard.razor
→ IMangakaSeriesApiClient.CreateDraftAsync
→ POST /api/mangaka/series/drafts
→ MangakaSeriesController.CreateDraftAsync
→ ISeriesService.CreateSeriesDraftAsync       ← transitional path
→ _unitOfWork.Series.CreateSeriesDraftViaProcAsync
→ manga.usp_Series_Create
```

---

## New CQRS/MediatR Flow

```
MangakaDashboard.razor
→ IMangakaSeriesApiClient.CreateDraftAsync    (unchanged)
→ POST /api/mangaka/series/drafts             (unchanged)
→ MangakaSeriesController.CreateDraftAsync
→ IMediator.Send(CreateSeriesDraftCommand)    ← MediatR dispatch
→ CreateSeriesDraftCommandHandler
→ ISeriesRepository.CreateSeriesDraftViaProcAsync
→ manga.usp_Series_Create                    (unchanged)
```

---

## Files Changed

| File | Change |
|---|---|
| `Application/Features/Mangaka/Series/Commands/CreateSeriesDraft/CreateSeriesDraftCommand.cs` | **New** — sealed record `IRequest<SeriesDraftCreatedDto>` |
| `Application/Features/Mangaka/Series/Commands/CreateSeriesDraft/CreateSeriesDraftCommandHandler.cs` | **New** — handler with cover validation, lifted logic, SHA-256 guard, Cloudinary cleanup |
| `API/Controllers/MangakaSeriesController.cs` | Removed `ISeriesService` field/constructor param; `CreateDraftAsync` now builds `CreateSeriesDraftCommand` and calls `_mediator.Send`; updated class doc comment |

**Unchanged:**
- `IMangakaSeriesApiClient` / `MangakaSeriesApiClient`
- `MangakaDashboard.razor`
- `API/Contracts/CreateSeriesDraftForm.cs`
- `Application/DTOs/Manga/SeriesDraftCreatedDto.cs`
- `Domain/Interfaces/ISeriesRepository.cs`
- `Infrastructure/Repositories/SeriesRepository.cs` (CreateSeriesDraftViaProcAsync)
- `Application/Interfaces/ISeriesService.cs` (still registered, still used by other pages)
- `Application/Services/SeriesService.cs` (CreateSeriesDraftAsync still exists but no longer called by the API)
- Submit Proposal, Edit Draft, Cancel Draft workflows

---

## Validation Behavior

The handler validates inputs in this order before any Cloudinary upload:

1. `ActorUserId` != `Guid.Empty`.
2. `Title` non-empty after trim.
3. `Genre` non-empty after trim.
4. `Slug` derivable from `SlugGenerator.Normalize(Slug, title)`.
5. If cover bytes are provided:
   - `CoverFileName` and `CoverContentType` must be present.
   - File size ≤ 5 MB.
   - Extension must be `.jpg`, `.jpeg`, `.png`, or `.webp`.
   - Content type must be `image/jpeg`, `image/png`, or `image/webp`.
6. After Cloudinary upload: `Sha256Hash` must not be null/empty.

All failures throw `InvalidOperationException` with a user-safe message → controller maps to `400 BadRequest`.

---

## Cloudinary Cleanup Behavior

- Cover images uploaded to Cloudinary use `"image"` resource type.
- If the SHA-256 null guard triggers: `DeleteFileAsync(publicId, "image")` is called before throwing.
- If SQL fails after a successful upload: `TryCleanupCoverAsync` is called (best-effort, logged if it fails, original exception still rethrown).
- No Cloudinary involvement if no cover was provided.

---

## Build Result

`dotnet build MangaManagementSystem.slnx` — **Build succeeded, 0 errors.**

All warnings are pre-existing.

---

## Manual Test Checklist

**Not run by OpenCode.** Developer manual testing required.

```
1. Login as active Mangaka.
2. Open New Series modal on dashboard.
3. Fill in title + genre only → click Create Draft.
4. Confirm SeriesDraftCreatedDto returned: seriesId, title, slug, PROPOSAL_DRAFT.
5. DB: Series row with status_code = PROPOSAL_DRAFT.
6. DB: SeriesContributor row for creator.
7. DB: No SeriesProposal row created.
8. Repeat with an optional cover image (PNG/JPG/WEBP, ≤ 5 MB).
9. DB: SERIES_COVER FileResource row with sha256_hash populated.
10. Try uploading a cover > 5 MB → friendly 400 error, no Cloudinary upload.
11. Try uploading a .pdf as cover → friendly 400 error.
12. Try uploading an image with wrong content type → friendly 400 error.
13. Confirm existing Submit Proposal, Edit Draft, Cancel Draft still work.
14. Confirm ISeriesService is not invoked from MangakaSeriesController (check logs).
15. Confirm no raw SQL/stack trace in UI or API responses.
```

---

## Remaining Tasks

- `SeriesService.CreateSeriesDraftAsync` still exists and is registered but is no longer called
  from the API. It can be removed from `ISeriesService` and `SeriesService` in a future
  cleanup task once confirmed safe. Other `ISeriesService` methods (`GetAllSeriesAsync`,
  `GetSeriesByIdAsync`) remain in use by `MangakaDashboard`, `SeriesPage`, `CreatorWorkspace`,
  `BoardPolls`, `SeriesList`.
- `/series/{slug}` full page — stub only.
- Create Draft modal in dashboard does not yet show a `PublicationFrequencyCode` dropdown
  (new drafts default to null; user can set it via Edit Draft after creation).
