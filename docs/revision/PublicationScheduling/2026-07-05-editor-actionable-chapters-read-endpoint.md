# Editor Actionable Chapters Read Endpoint

**Date:** 2026-07-05
**Branch:** `feature/Mangaka`
**Build:** SUCCESS (0 errors)

---

## Summary

Added backend read endpoint `GET /api/editor/series-chapters` that returns all actionable chapters (non-RELEASED, non-CANCELLED) from series where the actor is an active Tantou Editor contributor. Includes `SeriesCoverUrl` in the DTO so the publication schedule action drawer can reuse compact schedule card visuals. Provides optional filters for `seriesId`, `searchText`, and `statusCode`.

---

## Files Changed

### Created (4)
| Layer | File | Purpose |
|---|---|---|
| Application | `DTOs/Editor/EditorActionableChapterDto.cs` | Public DTO with ChapterId, SeriesId, SeriesTitle, SeriesSlug, SeriesCoverUrl, ChapterNumberLabel, ChapterTitle, StatusCode, PlannedReleaseDate, ReleasedAtUtc, PublicationFrequencyCode, UpdatedAtUtc, CanSchedule, CanPutOnHold, CanRelease |
| Application | `Features/Editor/ChapterReviews/Queries/GetEditorActionableChapters/GetEditorActionableChaptersQuery.cs` | CQRS query with ActorUserId, SeriesId?, SearchText?, StatusCode?, MaxResults |
| Application | `Features/Editor/ChapterReviews/Queries/GetEditorActionableChapters/GetEditorActionableChaptersQueryHandler.cs` | Handler: validates params, calls repository, maps records to DTOs |
| Infrastructure | `Repositories/EditorChapterReviewRepository.ActionableChapters.cs` | EF Core query: scoped by contributor, direct projection, no Include() |

### Modified (2)
| Layer | File | Change |
|---|---|---|
| Domain | `Interfaces/IEditorChapterReviewRepository.cs` | Added `GetActionableChaptersAsync` method + `EditorActionableChapterData` record |
| API | `Controllers/Editor/EditorChapterReviewsController.cs` | Added `GET /api/editor/series-chapters` endpoint |

---

## Endpoint Route

```
GET /api/editor/series-chapters
```

---

## Query Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `seriesId` | `Guid?` | null | Filter to a specific series |
| `searchText` | `string?` | null | Search series title, chapter number label, or chapter title (case-insensitive) |
| `statusCode` | `string?` | null | Filter to a specific status (must be one of the included statuses) |
| `maxResults` | `int?` | 100 | Max results (clamped to 1-200) |

All parameters are optional. No required parameters.

---

## Authorization / Scoping Behavior

- Actor identity resolved via `X-Actor-User-Id` header (existing pattern)
- Query scoped to series where actor is an active Tantou Editor contributor:
  ```
  ActiveSeriesContributors
    .Where(user_id == actorUserId && role_name == "Tantou Editor" && end_date IS NULL)
    .Select(series_id)
  ```
- Only chapters from scoped series are returned
- No global/public chapters visible
- Returns empty list for unauthenticated or non-contributor users (no error for auth failure at read level)

---

## DTO Fields

`EditorActionableChapterDto`:

| Field | Type | Source |
|---|---|---|
| `ChapterId` | `Guid` | Chapter.ChapterId |
| `SeriesId` | `Guid` | Chapter.SeriesId |
| `SeriesTitle` | `string` | Chapter.Series.Title |
| `SeriesSlug` | `string?` | Chapter.Series.Slug |
| `SeriesCoverUrl` | `string?` | Chapter.Series.CoverFile.CloudinarySecureUrl |
| `ChapterNumberLabel` | `string` | Chapter.ChapterNumberLabel |
| `ChapterTitle` | `string?` | Chapter.ChapterTitle |
| `StatusCode` | `string` | Chapter.StatusCode |
| `PlannedReleaseDate` | `DateTime?` | Chapter.PlannedReleaseDate |
| `ReleasedAtUtc` | `DateTime?` | Chapter.ReleasedAtUtc |
| `PublicationFrequencyCode` | `string?` | Chapter.Series.PublicationFrequencyCode |
| `UpdatedAtUtc` | `DateTime?` | Chapter.UpdatedAtUtc |
| `CanSchedule` | `bool` | Computed: true for DRAFT, REVISION_REQUESTED, UNDER_REVIEW, APPROVED, SCHEDULED, ON_HOLD |
| `CanPutOnHold` | `bool` | Computed: true for SCHEDULED |
| `CanRelease` | `bool` | Computed: true for APPROVED or SCHEDULED |

---

## Series Cover URL Behavior

Uses the same projection pattern as `PublicationScheduleRepository`:
```csharp
c.Series != null && c.Series.CoverFile != null
    ? c.Series.CoverFile.CloudinarySecureUrl
    : null
```

- EF translates `Series.CoverFile.CloudinarySecureUrl` to a SQL JOIN on `cover_file_id`
- No `.Include()` needed — direct projection in `Select()`
- Returns `null` when no cover is set
- No `DeletedAtUtc` check (consistent with existing `PublicationScheduleRepository` pattern)

---

## Status Inclusion / Exclusion Rules

**Included (returned by default):**
- DRAFT
- REVISION_REQUESTED
- UNDER_REVIEW
- APPROVED
- SCHEDULED
- ON_HOLD

**Excluded (always filtered out):**
- RELEASED
- CANCELLED

If `statusCode` filter is provided and matches an included status, only that status is returned.

---

## Repository Query Details

- `AsNoTracking()` — read-only query
- Direct projection via `Select()` — no eager loading of navigation graphs
- Sorted by: SeriesTitle ASC, ChapterNumberLabel ASC, PlannedReleaseDate (nulls last) ASC
- Limited: `Take(maxResults)` after filter + sort
- All filtering happens in SQL — no in-memory filtering of large result sets

---

## Build Result

**SUCCESS** — 0 errors

---

## API Smoke Test Status

Not run (build-only verification).

Manual API smoke checklist:
- [ ] Call as Tantou Editor: `GET /api/editor/series-chapters`
- [ ] Confirm only chapters from active editor contributor series are returned
- [ ] Confirm RELEASED/CANCELLED chapters excluded
- [ ] Confirm APPROVED, SCHEDULED, ON_HOLD can appear
- [ ] Confirm UNDER_REVIEW can appear
- [ ] Call `GET /api/editor/series-chapters?seriesId={guid}` — confirm filtering works
- [ ] Call `GET /api/editor/series-chapters?searchText=chapter` — confirm search works
- [ ] Call `GET /api/editor/series-chapters?statusCode=SCHEDULED` — confirm status filter works
- [ ] Call `GET /api/editor/series-chapters?maxResults=5` — confirm limit works
- [ ] Call as non-Editor user — confirm empty list or 400
- [ ] Call without X-Actor-User-Id header — confirm 400
- [ ] Confirm `SeriesCoverUrl` is returned when cover exists
- [ ] Confirm `CanSchedule`/`CanPutOnHold`/`CanRelease` booleans are correct per status

---

## Remaining Frontend Follow-up

1. Update `PublicationScheduleActionDrawer.razor` to use `GET /api/editor/series-chapters` instead of `GetReviewQueueAsync` for loadChaptersAsync
2. Add typed API client method `GetActionableChaptersAsync` to `IEditorChapterReviewApiClient` / `EditorChapterReviewApiClient`
3. Remove the fallback error message about "editor chapter list endpoint not available"
4. Covers will now render properly (no more "N/C" placeholders for Editor chapters)
5. The `_drawerLoading` cleanup and `_error` state will be simplified

---

## Docs/Revision Handoff Path

`docs/revision/PublicationScheduling/2026-07-05-editor-actionable-chapters-read-endpoint.md`
