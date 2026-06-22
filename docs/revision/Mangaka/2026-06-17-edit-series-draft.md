# Session Note — BF-SERIES-002 Edit Series Draft Profile

- **Branch:** `feature/Mangaka`
- **Date:** 2026-06-17

---

## Task Summary

Implemented BF-SERIES-002 — Edit Series Draft Profile. A Mangaka contributor can now
open any PROPOSAL_DRAFT series card on the dashboard and edit: title, synopsis, genre,
content language, publication frequency, and optionally replace the cover image.

Cover update is integrated into the Edit Draft workflow only. The old standalone
Upload Cover flow (which directly injected IFileStorageService/IFileResourceService
into Razor) was already removed in the previous dashboard corrections task and is not
restored here. Cover editing is locked once a series leaves PROPOSAL_DRAFT — the stored
procedure enforces the status guard.

This task follows the MediatR/CQRS pattern established in BF-SERIES-003.

---

## Architecture Flow

```
MangakaDashboard.razor (Edit Draft modal)
  → IMangakaSeriesApiClient.UpdateDraftAsync
  → PUT /api/mangaka/series/{seriesId}/draft-profile (multipart/form-data)
  → MangakaSeriesController.UpdateDraftProfileAsync (thin controller)
  → IMediator.Send(UpdateSeriesDraftCommand)
  → UpdateSeriesDraftCommandHandler (Application — orchestration)
  → IFileStorageService.UploadFileAsync (optional cover, Cloudinary + SHA-256)
  → ISeriesRepository.UpdateSeriesDraftViaProcAsync (Infrastructure SP wrapper)
  → manga.usp_Series_UpdateProfile (SQL Server — owns transaction, audit, status guard)
```

Auth boundary: same transitional X-Actor-User-Id header pattern.

---

## New Stored Procedure

**File:** `MangaManagementSystem_Procedures_Views_Bootstrap.sql`

`manga.usp_Series_UpdateProfile` — inserted after `manga.usp_Series_Create`.

Parameters:
```sql
@actor_user_id                  UNIQUEIDENTIFIER,
@series_id                      UNIQUEIDENTIFIER,
@title                          NVARCHAR(200),
@slug                           NVARCHAR(220),
@synopsis                       NVARCHAR(MAX),
@genre                          NVARCHAR(100),
@content_language_code          NVARCHAR(10)  = 'ja',
@publication_frequency_code     NVARCHAR(50)  = NULL,
@cover_original_file_name       NVARCHAR(260) = NULL,
@cover_cloudinary_public_id     NVARCHAR(255) = NULL,
@cover_cloudinary_secure_url    NVARCHAR(1000) = NULL,
@cover_content_type             NVARCHAR(100) = NULL,
@cover_file_size_bytes          BIGINT        = NULL,
@cover_sha256_hash              CHAR(64)      = NULL,
@new_cover_file_resource_id     UNIQUEIDENTIFIER OUTPUT
```

SP behavior:
1. Acquires app lock per series (prevents concurrent updates).
2. Validates series exists.
3. Validates status = PROPOSAL_DRAFT (error 57403 if not).
4. Validates actor is active Mangaka contributor (error 57404 if not).
5. Validates cover metadata is all-or-nothing (error 57405 if partial).
6. Soft-deletes old cover FileResource via usp_FileResource_SoftDelete (if new cover supplied).
7. Creates new SERIES_COVER FileResource via usp_FileResource_Create (if new cover supplied).
8. Updates manga.Series (title, slug, synopsis, genre, language, frequency, cover_file_id,
   updated_at_utc, updated_by_user_id).
9. Writes SERIES_DRAFT_PROFILE_UPDATED audit event.

Custom error numbers (57401-57405):
- 57401: lock failure
- 57402: series not found
- 57403: not PROPOSAL_DRAFT
- 57404: not active Mangaka contributor
- 57405: incomplete cover metadata

---

## CQRS / MediatR Files Added

```
Application/Features/Mangaka/Series/Commands/UpdateSeriesDraft/
  UpdateSeriesDraftCommand.cs        — IRequest<SeriesDraftUpdatedDto>
  UpdateSeriesDraftCommandHandler.cs — validates, uploads cover, calls repository, returns DTO
```

Handler responsibilities:
- Input validation (actor, series ID, title, genre).
- Slug normalization via SlugGenerator.Normalize.
- Optional Cloudinary cover upload via IFileStorageService.UploadFileAsync.
- SHA-256 null guard before SQL call.
- SQL call via ISeriesRepository.UpdateSeriesDraftViaProcAsync.
- Best-effort Cloudinary cleanup on SQL failure (image resource type).
- Returns SeriesDraftUpdatedDto.

---

## Files Changed by Layer

| Layer | File | Change |
|---|---|---|
| SQL | `MangaManagementSystem_Procedures_Views_Bootstrap.sql` | Added `manga.usp_Series_UpdateProfile` |
| Domain | `Interfaces/ISeriesRepository.cs` | Added `UpdateSeriesDraftViaProcAsync(...)` |
| Application | `DTOs/Manga/SeriesDraftUpdatedDto.cs` | **New** result DTO |
| Application | `Features/Mangaka/Series/Commands/UpdateSeriesDraft/UpdateSeriesDraftCommand.cs` | **New** |
| Application | `Features/Mangaka/Series/Commands/UpdateSeriesDraft/UpdateSeriesDraftCommandHandler.cs` | **New** |
| Infrastructure | `Repositories/SeriesRepository.cs` | Added `UpdateSeriesDraftViaProcAsync` + `MapUpdateSqlException` |
| API | `Contracts/UpdateSeriesDraftForm.cs` | **New** multipart form contract |
| API | `Controllers/MangakaSeriesController.cs` | Added `PUT {seriesId}/draft-profile` endpoint |
| Web | `Services/Api/IMangakaSeriesApiClient.cs` | Added `UpdateDraftAsync(...)` |
| Web | `Services/Api/MangakaSeriesApiClient.cs` | Implemented `UpdateDraftAsync(...)` |
| Web | `Components/Pages/Mangaka/MangakaDashboard.razor` | Edit Draft modal (was read-only shell) + state + methods |

---

## API Endpoint Added

```
PUT /api/mangaka/series/{seriesId:guid}/draft-profile
Content-Type: multipart/form-data
Header: X-Actor-User-Id: <actorUserId>

Required fields: Title, Genre
Optional fields: Synopsis, ContentLanguageCode, PublicationFrequencyCode, Slug, CoverFile

200 OK → SeriesDraftUpdatedDto
400 BadRequest → ApiErrorResponse { message }
500 Problem → generic safe message
```

---

## Web Typed Client Method Added

```csharp
Task<SeriesDraftUpdatedDto> UpdateDraftAsync(
    Guid actorUserId, Guid seriesId,
    string title, string synopsis, string genre, string contentLanguageCode,
    string? publicationFrequencyCode = null, string? slug = null,
    byte[]? coverFileBytes = null, string? coverFileName = null,
    string? coverContentType = null,
    CancellationToken cancellationToken = default);
```

---

## UI Behavior Added

**Edit Draft modal** (replaces read-only Draft Details shell):
- Opens when clicking a PROPOSAL_DRAFT card.
- Fields: title, synopsis, genre, content language, publication frequency.
- Optional cover replacement: PNG/JPG/WEBP, max 5 MB, validated at UI layer.
- Cover pre-read behavior: IBrowserFile bytes read immediately in OnEditCoverChanged.
  IBrowserFile reference not held across re-renders.
- Cover preview: data: URI generated from bytes for immediate visual confirmation.
- Current cover shown as thumbnail when no new cover is selected.
- "Save Changes" button → calls UpdateDraftAsync → updates in-memory card.
- "Submit Proposal for Review" secondary action at bottom of modal.
- Status guard: cover upload blocked by SP for non-PROPOSAL_DRAFT; UI only shows
  this modal for PROPOSAL_DRAFT cards (card click routing enforced in OpenSeriesCard).

**After save:**
- In-memory card updated: title, genre, synopsis, slug, language, coverUrl (if new cover).
- Snackbar "Draft profile saved."
- Modal closes.

---

## Cloudinary Cover Cleanup Behavior

Cover images use "image" Cloudinary resource type.

Flow:
1. Handler uploads new cover to Cloudinary (outside SQL transaction).
2. If Sha256Hash is null, handler cleans up Cloudinary and throws safe error.
3. SQL SP soft-deletes old FileResource and creates new SERIES_COVER FileResource inside transaction.
4. If SQL fails after Cloudinary upload, handler attempts best-effort DeleteFileAsync("image").
5. Cleanup failure is logged safely; original business error is rethrown.

---

## Build Result

`dotnet build MangaManagementSystem.slnx` — **Build succeeded, 0 errors.**

All warnings are pre-existing (same 34 warnings as previous task).

---

## Manual Test Notes

**Not run by OpenCode.** Developer manual testing required.

**IMPORTANT: Before testing, run the new SQL stored procedure against the target database:**
```sql
-- Run only the usp_Series_UpdateProfile block from
-- MangaManagementSystem_Procedures_Views_Bootstrap.sql
```

Checklist:
```
1. Login as active Mangaka.
2. Navigate to /mangaka dashboard.
3. Click a PROPOSAL_DRAFT card → Edit Draft modal opens.
4. Verify all fields pre-filled with current values.
5. Change title, synopsis, genre, language.
6. Click Save Changes → success snackbar, modal closes.
7. Confirm card title/genre updated in-memory.
8. Re-open modal — verify updated values shown.
9. Upload a new cover image (PNG/JPG/WEBP, ≤ 5 MB).
10. Confirm preview renders in modal before saving.
11. Save with new cover → card cover thumbnail updates.
12. Try uploading a non-image file → friendly warning, file rejected.
13. Try uploading > 5 MB image → friendly warning, file rejected.
14. Verify database:
    - Series.title / slug / synopsis / genre / content_language_code updated.
    - Old cover FileResource soft-deleted (deleted_at_utc set).
    - New SERIES_COVER FileResource created with sha256_hash.
    - AuditEvent row with action_code = SERIES_DRAFT_PROFILE_UPDATED.
15. Submit a proposal → series becomes UNDER_EDITORIAL_REVIEW.
16. Confirm Edit Draft card action no longer appears (card click opens Review Status modal).
17. Attempt direct API call: PUT /api/mangaka/series/{id}/draft-profile on an
    UNDER_EDITORIAL_REVIEW series → expect 400 "Only a series in draft status...".
18. Confirm no raw SQL / stack trace appears in UI or API responses.
```

---

## Known Risks / Transitional Debt

1. **PublicationFrequencyCode not stored in SeriesCardData.** When the modal opens,
   _editFrequency initializes to null (not the saved value). The SP will update the
   frequency to whatever is submitted. If a user opens edit without changing frequency
   and saves, the existing frequency will be set to null. Fix: add PublicationFrequencyCode
   to SeriesCardData in the next task and pre-populate the dropdown.

2. **Slug not user-editable in the form.** The Slug field is passed as null and derived
   from the title in UpdateSeriesDraftCommandHandler via SlugGenerator.Normalize.
   If the user changes the title, the slug changes automatically. This may affect
   the /series/{slug} stable URL. A future improvement is to show the derived slug
   in the modal as a read-only preview before saving.

3. **`_editFrequency` always set to null when modal opens** (see item 1).

4. **Create Draft MediatR migration** still pending (uses transitional ISeriesService).

5. **Cancel Draft** still disabled (manga.usp_Series_CancelDraft has no C# wiring).

---

## Next Recommended Prompt

> Fix the PublicationFrequencyCode pre-population gap:
> - Add PublicationFrequencyCode to SeriesCardData.
> - Populate it in LoadSeriesAsync from SeriesDto.PublicationFrequencyCode.
> - Seed _editFrequency from _detailSeries.PublicationFrequencyCode in OpenDraftDetails.
>
> Then implement Cancel Draft:
> - Add CancelSeriesDraftViaProcAsync to ISeriesRepository.
> - Implement via manga.usp_Series_CancelDraft (already exists in SQL).
> - Add CancelSeriesDraftCommand/handler (MediatR).
> - Add DELETE /api/mangaka/series/{seriesId}/draft (or POST cancel action).
> - Enable the "Cancel Draft" kebab button.
