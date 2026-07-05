# Action Drawer Editor Endpoint Integration

**Date:** 2026-07-05
**Branch:** `feature/Mangaka`
**Build:** SUCCESS (0 errors)

---

## Summary

Updated `PublicationScheduleActionDrawer.razor` to load Editor actionable chapters from the new `GET /api/editor/series-chapters` endpoint instead of the old `GetReviewQueueAsync` fallback. The Editor drawer now properly shows all actionable statuses (DRAFT, REVISION_REQUESTED, UNDER_REVIEW, APPROVED, SCHEDULED, ON_HOLD) with `SeriesCoverUrl` mapped correctly.

---

## Files Changed

### Modified (3)
| File | Change |
|---|---|
| `Web/Services/Api/IEditorChapterReviewApiClient.cs` | Added `GetActionableChaptersAsync` method + `System.Collections.Generic` using |
| `Web/Services/Api/EditorChapterReviewApiClient.cs` | Added `GetActionableChaptersAsync` implementation + `StringBuilder`/`List` usings |
| `Web/Components/Pages/Publication/PublicationScheduleActionDrawer.razor` | Replaced `GetReviewQueueAsync` fallback with `GetActionableChaptersAsync`; mapped `SeriesCoverUrl` |

---

## API Client Method Added

**Interface:** `IEditorChapterReviewApiClient`
```csharp
Task<IReadOnlyList<EditorActionableChapterDto>> GetActionableChaptersAsync(
    Guid actorUserId,
    Guid? seriesId = null,
    string? searchText = null,
    string? statusCode = null,
    int? maxResults = null,
    CancellationToken cancellationToken = default);
```

**Endpoint:** `GET /api/editor/series-chapters`
**Query params:** `seriesId`, `searchText`, `statusCode`, `maxResults` (all optional)
**Pattern:** Builds query string with `StringBuilder`, sends `X-Actor-User-Id` header, deserializes `List<EditorActionableChapterDto>`

---

## DTO

No additional Web DTO needed. The existing `Application/DTOs/Editor/EditorActionableChapterDto` (from the Application layer) is used directly:
- Guid ChapterId, Guid SeriesId, string SeriesTitle, string? SeriesSlug
- string? SeriesCoverUrl, string ChapterNumberLabel, string? ChapterTitle
- string StatusCode, DateTime? PlannedReleaseDate, DateTime? ReleasedAtUtc
- string? PublicationFrequencyCode, DateTime? UpdatedAtUtc
- bool CanSchedule, bool CanPutOnHold, bool CanRelease

---

## Drawer Data-Source Change

**Before (partial — only UNDER_REVIEW chapters):**
```csharp
var queue = await EditorApi.GetReviewQueueAsync(_currentUserId, null);
items = queue.Chapters.Select(c => new ActionableChapterItem(
    c.ChapterId, c.SeriesId, c.SeriesTitle,
    c.ChapterNumberLabel, c.ChapterTitle,
    c.StatusCode, null, null, null)).ToList();
```

**After (all actionable chapters + cover):**
```csharp
var chapters = await EditorApi.GetActionableChaptersAsync(
    _currentUserId, SelectedSeriesId);
items = chapters.Select(c => new ActionableChapterItem(
    c.ChapterId, c.SeriesId, c.SeriesTitle,
    c.ChapterNumberLabel, c.ChapterTitle,
    c.StatusCode, c.PlannedReleaseDate,
    c.ReleasedAtUtc, c.SeriesCoverUrl)).ToList();
```

Key improvements:
- `SelectedSeriesId` passed as `seriesId` for server-side filtering
- `PlannedReleaseDate` and `ReleasedAtUtc` now populated (was null before)
- `SeriesCoverUrl` now mapped (was "N/C" placeholder before)
- Client-side `ExcludedStatuses` filter no longer needed (backend already excludes RELEASED/CANCELLED)

---

## SeriesCoverUrl Mapping

`EditorActionableChapterDto.SeriesCoverUrl` → `ActionableChapterItem.SeriesCoverUrl`

The drawer card cover rendering (lines 88-96) already handles this correctly:
```razor
@if (!string.IsNullOrWhiteSpace(chapter.SeriesCoverUrl))
{
    <img src="@chapter.SeriesCoverUrl" alt="cover" loading="lazy" />
}
else
{
    <span>...placeholder...</span>
}
```

---

## Editor Action Behavior

All actions preserved from Phase 1:

| Status | Visible Actions | Endpoint Used |
|---|---|---|
| DRAFT | Schedule | `PUT .../planned-release-date` |
| REVISION_REQUESTED | Schedule | `PUT .../planned-release-date` |
| UNDER_REVIEW | Schedule | `PUT .../planned-release-date` |
| APPROVED | Schedule, Release | `PUT .../planned-release-date`, `POST .../release` |
| SCHEDULED | Reschedule, Hold, Release | `PUT .../planned-release-date`, `POST .../hold`, `POST .../release` |
| ON_HOLD | Return to Schedule | `PUT .../planned-release-date` |
| RELEASED/CANCELLED | Not shown | Backend excludes these |

---

## Mangaka Behavior Impact

**None.** Mangaka path unchanged — still uses `IMangakaChapterApiClient.GetMyChaptersAsync`.

---

## Build Result

**SUCCESS** — 0 errors

---

## Runtime Smoke Test Status

Not run (build-only verification).

Editor smoke checklist:
- [ ] Log in as Tantou Editor with data containing APPROVED/SCHEDULED/ON_HOLD chapters
- [ ] Open `/publication/schedule`, click "Manage Schedule"
- [ ] Confirm APPROVED chapters appear with cover images
- [ ] Confirm SCHEDULED chapters appear
- [ ] Confirm ON_HOLD chapters appear
- [ ] Confirm UNDER_REVIEW chapters appear (schedule action available)
- [ ] Confirm no RELEASED/CANCELLED chapters appear
- [ ] APPROVED: click Release → confirm → chapter disappears from drawer
- [ ] SCHEDULED: click Hold → enter reason → confirm → status badge shows ON_HOLD
- [ ] SCHEDULED: click Release → confirm → chapter disappears
- [ ] ON_HOLD: click "Return to Schedule" → date picker → confirm → status becomes SCHEDULED
- [ ] Calendar refreshes after each action
- [ ] Series filter works (select series in main calendar → drawer filters)
- [ ] Cover images display for chapters with covers, placeholder for those without

---

## Remaining Gaps

1. **Mangaka cover images still "N/C"**: `MangakaChapterListItemDto` does not include `SeriesCoverUrl`. A future DTO update could add this field.
2. **Server-side searchText not wired**: The drawer uses local chapter typeahead; `searchText` parameter is available but not passed currently. Can be added later.
3. **CanSchedule/CanPutOnHold/CanRelease booleans**: The backend DTO includes these, but the drawer uses local status checks (`CanSchedule()` method, inline checks for hold/release). Switching to the booleans is a minor future cleanup.

---

## Phase 2 Reminder

Drag-and-drop integration from drawer cards to calendar day columns. Not part of this task.

---

## Docs/Revision Handoff Path

`docs/revision/PublicationScheduling/2026-07-05-action-drawer-editor-endpoint-integration.md`
