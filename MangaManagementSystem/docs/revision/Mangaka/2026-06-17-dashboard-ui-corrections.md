# Session Note — Dashboard UI Corrections + Series Stub Page

- **Branch:** `feature/Mangaka`
- **Date:** 2026-06-17

---

## Task Summary

Fixed the MangakaDashboard UI and lifecycle navigation rules after BF-SERIES-003 manual testing.
Corrected card click routing, removed incorrect actions (Favorite, standalone Upload Cover),
replaced Delete with scoped Cancel Draft, surfaced Submit Proposal as a visible card action,
added cover thumbnail rendering, introduced two new modal shells (Draft Details, Review Status),
and created the minimal `/series/{slug}` stub page to establish the stable series URL.

---

## Cover Thumbnail Read-Model Change

**Problem:** Cards only showed a placeholder book icon even when a cover had been uploaded.
`SeriesService.GetAllSeriesAsync` called `GetAllAsync()` which had no `Include(s => s.CoverFile)`,
and `SeriesDto` had no `CoverUrl` field.

**Fix (display-only, no upload logic changed):**

1. **`ISeriesRepository`** — added `GetAllWithCoverAsync()` interface method (doc comment).
2. **`SeriesRepository`** — implemented `GetAllWithCoverAsync()`:
   `_context.Series.Include(s => s.CoverFile).AsNoTracking().ToListAsync()`.
3. **`SeriesDto`** — added `string? CoverUrl = null` (optional positional param, non-breaking).
4. **`SeriesService.MapToDto`** — populates `CoverUrl` from
   `s.CoverFile?.CloudinarySecureUrl` when `s.CoverFile?.DeletedAtUtc == null`.
5. **`SeriesService.GetAllSeriesAsync`** — switched from `GetAllAsync()` to `GetAllWithCoverAsync()`.
6. **`SeriesCardData`** — added `CoverUrl (string?)`, `Slug (string)`, `Synopsis (string?)`,
   `ContentLanguageCode (string?)` fields (needed for Draft Details modal and routing).
7. **`MangakaDashboard.razor`** — `LoadSeriesAsync` maps new fields; cards render `<img>` when
   `CoverUrl` is non-empty, styled placeholder otherwise.

---

## Dashboard Action Changes

| Before | After |
|---|---|
| All cards → navigate to `/mangaka/workspace/{seriesId}` | Status-based routing (see below) |
| Favorite / Unfavorite in kebab | **Removed entirely** |
| Standalone Upload Cover in kebab (all statuses) | **Removed entirely** |
| Delete in kebab (all statuses) | **Replaced:** Cancel Draft (PROPOSAL_DRAFT only, disabled, "Coming soon" tooltip) |
| Submit Proposal in kebab only | **Kept in kebab + added visible card action row for PROPOSAL_DRAFT** |
| Edit Cover modal (`SaveCoverUpload`) | **Removed entirely** (pre-existing tech debt; cover change belongs in BF-SERIES-002 Edit Draft) |
| `IFileStorageService`, `IFileResourceService`, `ChangeSeriesStatus` | **Removed** (unused after Edit Cover removal) |
| `_loading`, `_favoriteIds`, `IsFavorite`, `ToggleFavorite` | **Removed** |

Cover update is now intentionally only available as part of the future BF-SERIES-002 Edit Draft workflow.

---

## Navigation Guard Behavior

`OpenSeriesCard(series)` dispatches card clicks by `StatusCode`:

| Status | Behavior |
|---|---|
| `PROPOSAL_DRAFT` | Opens Draft Details modal shell (read-only) |
| `SERIALIZED` | `Nav.NavigateTo($"/series/{series.Slug}")` |
| `UNDER_EDITORIAL_REVIEW` | Opens Review Status modal shell |
| `UNDER_BOARD_REVIEW` | Opens Review Status modal shell |
| `HIATUS` / `CANCELLED` / `COMPLETED` | Opens Review Status modal shell |

No card click navigates directly to `/mangaka/workspace/...` from the dashboard.

---

## Modal Shells Added

### Draft Details Modal
- Opens when a `PROPOSAL_DRAFT` card is clicked.
- Shows: cover thumbnail (if available), title, synopsis, genre, language, slug.
- Read-only info note: "Full draft editing will be available in BF-SERIES-002."
- Action buttons: Close, Submit Proposal (opens Submit Proposal modal).

### Review Status Modal
- Opens for `UNDER_EDITORIAL_REVIEW`, `UNDER_BOARD_REVIEW`, `HIATUS`, `CANCELLED`, `COMPLETED`.
- Shows: cover thumbnail (if available), status badge, status-specific message.
- Read-only; no action buttons except Close.

### Submit Proposal Modal
- Unchanged from BF-SERIES-003.
- Now reachable from: (1) Draft Details modal, (2) card action row "Submit" button, (3) kebab menu item.

---

## `/series/{slug}` Stub Page

**New file:** `src/MangaManagementSystem.Web/Components/Pages/Series/SeriesPage.razor`

Route: `@page "/series/{Slug}"` — `[Authorize]` (all authenticated roles).

Content (minimal stub only):
- Back to Dashboard button.
- Cover image or placeholder.
- Title, status badge, genre, language, publication frequency chips.
- Synopsis if available.
- Placeholder alert: "Chapter and workspace management will be added later."
- Stable URL display.

Implementation note: uses `SeriesService.GetAllSeriesAsync()` filtered by slug (case-insensitive).
A dedicated `GetBySlugAsync` is a future optimisation; the series count is small for MVP.

Slug is sourced from `SeriesDto.Slug` (already populated by `SeriesService.MapToDto`).

---

## Files Changed

| Layer | File | Change |
|---|---|---|
| Domain | `Interfaces/ISeriesRepository.cs` | Added `GetAllWithCoverAsync()` |
| Infrastructure | `Repositories/SeriesRepository.cs` | Implemented `GetAllWithCoverAsync()` with Include + AsNoTracking |
| Application | `DTOs/Manga/SeriesDtos.cs` | Added `string? CoverUrl = null` to `SeriesDto` |
| Application | `Services/SeriesService.cs` | `GetAllSeriesAsync` uses `GetAllWithCoverAsync`; `MapToDto` populates `CoverUrl` |
| Web | `Components/Pages/Mangaka/MangakaDashboard.razor` | Full rewrite of card layout, modals, navigation guards, action cleanup |
| Web | `Components/Pages/Series/SeriesPage.razor` | **New** — minimal `/series/{slug}` stub |

---

## Build Result

`dotnet build MangaManagementSystem.slnx` — **Build succeeded, 0 errors.**

34 warnings — all pre-existing (15 from Domain/Application/Infrastructure, 19 from Web MUD0002/CS0649).
No new warnings introduced.

---

## Manual Test Notes

**Not run by OpenCode.** Developer manual testing required.

Checklist:
```
1. Login as active Mangaka.
2. Navigate to /mangaka.
3. Series with a cover → thumbnail renders in card.
4. Series without a cover → placeholder icon renders.
5. Click a PROPOSAL_DRAFT card → Draft Details modal opens (not workspace).
6. Draft Details modal shows title, slug, genre, synopsis, language.
7. "Submit Proposal" in Draft Details modal opens Submit Proposal modal.
8. Submit Proposal end-to-end still works (existing BF-SERIES-003 flow unchanged).
9. After submit: card flips to UNDER_EDITORIAL_REVIEW.
10. Click UNDER_EDITORIAL_REVIEW card → Review Status modal opens (not workspace).
11. Review Status modal shows correct status-specific message.
12. No "Upload Cover" in any kebab menu.
13. No "Favorite" in any kebab menu.
14. "Cancel Draft" appears in kebab for PROPOSAL_DRAFT only, disabled.
15. "Submit" button visible directly on PROPOSAL_DRAFT card (action row).
16. Click a SERIALIZED card → navigates to /series/{slug}.
17. /series/{slug} page shows title, cover, status, genre, synopsis, placeholder notice.
18. /series/{unknown-slug} shows "Series Not Found" state.
19. Back button on /series/{slug} returns to /mangaka.
```

---

## Remaining Tasks / Technical Debt

1. **BF-SERIES-002 — Edit Series Draft Profile:** `manga.usp_Series_UpdateProfile` does not exist
   yet. Draft Details modal is read-only. Full edit (title, synopsis, genre, cover, language)
   requires: SQL SP creation, command handler (MediatR), API endpoint, form in Draft Details modal.

2. **Cancel Draft workflow:** `manga.usp_Series_CancelDraft` exists in SQL but has no C# wrapper,
   Domain interface method, Application handler, or API endpoint. The "Cancel Draft" kebab item
   is disabled with a tooltip until a dedicated task implements it.

3. **`/series/{slug}` full page:** Stub only. Chapter list, workspace entry, role-based actions,
   and public-reader view will be added when the chapter/serialization workflow is implemented.
   A dedicated `GetBySlugAsync` repository method should replace the current
   `GetAllSeriesAsync().FirstOrDefault` approach at that point.

4. **Create Draft MediatR migration:** Still uses transitional `ISeriesService` path. Migrate
   to `CreateSeriesDraftCommand`/handler in a later dedicated task.

5. **Cover URL on newly-created drafts:** After `CreateSeriesDraft` adds a card to `_seriesData`
   in-memory, the card's `CoverUrl` is `null` even if a cover was uploaded (the Cloudinary URL
   requires a DB round-trip that bypasses the current in-memory add). The cover will appear
   correctly on next full page load (`LoadSeriesAsync`). Document as acceptable MVP behaviour.

---

## Next Recommended Prompt

> Implement BF-SERIES-002 — Edit Series Draft Profile. Prerequisites:
> 1. Create `manga.usp_Series_UpdateProfile` SQL stored procedure (does not exist).
> 2. Add `UpdateSeriesDraftViaProcAsync` to `ISeriesRepository`.
> 3. Add `UpdateSeriesDraftCommand`/handler (MediatR, following BF-SERIES-003 pattern).
> 4. Add `PUT /api/mangaka/series/{seriesId}/draft-profile` multipart endpoint.
> 5. Add `UpdateDraftAsync` to `IMangakaSeriesApiClient`.
> 6. Convert the Draft Details modal from read-only to an edit form
>    (title, synopsis, genre, language, cover upload for PROPOSAL_DRAFT only).
> 7. Verify cover upload is locked for non-PROPOSAL_DRAFT series.
>
> Separately:
> - Wire `manga.usp_Series_CancelDraft` as `CancelSeriesDraftCommand`/handler for the Cancel Draft button.
