# Cross-Role SafeReturnUrl Navigation

## Branch

`feature/Mangaka`

## Date

2026-06-22

## Task summary

Implemented reusable cross-role SafeReturnUrl navigation for SeriesPage and Workspace flows. Fixed broken back-navigation for non-Mangaka roles. Added workspace link buttons on ReviewSubmissions task cards with returnUrl support.

## Architecture path

```text
Web: SafeReturnUrl helper + Razor pages
No API/Application/Infrastructure/Domain/DB/SP changes for navigation.
Application DTO + mapper: added 3 read-model fields (SeriesSlug, ChapterId, SourceChapterPageVersionId) from existing EF include path.
```

## Files changed

| Layer | File | Change |
|-------|------|--------|
| Web | `Services/SafeReturnUrl.cs` | Replaced hardcoded allowlist with explicit prefix array; added `/mangaka`, `/assistant`, `/board`, `/board-chief`, `/admin`, `/dashboard` prefixes; added `data:` and `/api/` and `/signout` rejection; added `AppendReturnUrl()` helper |
| Web | `Components/Pages/Series/SeriesPage.razor` | Changed fallback from `/mangaka` to `/dashboard`; "not found" back button uses `BackUrl`; Workspace/chapter links now forward `returnUrl` using `SafeReturnUrl.AppendReturnUrl()` |
| Web | `Components/Pages/Mangaka/MangakaDashboard.razor` | Series card SERIALIZED click now passes `returnUrl=/mangaka` |
| Web | `Components/Pages/Mangaka/ReviewSubmissions.razor` | Added Back button to Mangaka Dashboard; added workspace link buttons on Original Page and Submitted Output; added `BuildWorkspaceLink()` helper; added `@using MangaManagementSystem.Web.Services` |
| Application | `DTOs/Manga/ChapterPageTaskDtos.cs` | Added `SeriesSlug`, `ChapterId`, `SourceChapterPageVersionId` optional fields |
| Application | `Services/ChapterPageTaskService.cs` | Populated new fields in `MapToDtoWithFullContext` and `MapToDtoWithAssistantContext` from existing EF chain |

## SafeReturnUrl before/after

### Before (hardcoded exact matches)

```csharp
return value == "/editor"
    || value.StartsWith("/editor/", ...)
    || value.StartsWith("/series/", ...)
    || value == "/editor/series"
    || value == "/editor/proposals"
    || value == "/editor/annotations"
    || value == "/editor/chapters";
```

### After (prefix-based allowlist)

```csharp
AllowedPrefixes = { "/mangaka", "/assistant", "/editor", "/board-chief", "/board", "/admin", "/series", "/dashboard" }
// Match: exact prefix or prefix + "/"
// Reject: //, ://, backslash, /javascript:, /data:, /api/, /signout
```

## SeriesPage fallback fix

- Before: `SafeReturnUrl.Resolve(returnUrl, "/mangaka")` — broke for Tantou Editor, Assistant, and other roles.
- After: `SafeReturnUrl.Resolve(returnUrl, "/dashboard")` — neutral fallback for all roles.
- "Not found" back button also now uses `BackUrl` instead of hardcoded `/mangaka`.

## Workspace return behavior

- CreatorWorkspace already used `SafeReturnUrl.Resolve(returnUrl, $"/series/{Slug}")` — no code change needed.
- The expanded SafeReturnUrl allowlist now accepts `/mangaka/review-submissions`, `/editor/series`, `/assistant/tasks`, etc.

## Source pages updated to pass returnUrl

| Source page | Destination | returnUrl passed |
|------------|------------|-----------------|
| MangakaDashboard (SERIALIZED click) | `/series/{slug}` | `/mangaka` |
| SeriesPage (Open Workspace button) | `/series/{slug}/workspace` | forwards incoming `returnUrl` |
| SeriesPage (chapter row click) | `/series/{slug}/workspace?chapterId=...` | forwards incoming `returnUrl` |
| ReviewSubmissions (workspace links) | `/series/{slug}/workspace?chapterId=...` | `/mangaka/review-submissions` |
| Editor SeriesList | `/series/{slug}` | `/editor/series` (already worked via SeriesNavigationUrlBuilder) |
| EditorDashboard | `/series/{slug}` | `/editor` (already worked via SeriesNavigationUrlBuilder) |

## ReviewSubmissions workspace links

- "View in Workspace" button appears below Original Page image (when `SeriesSlug` and `ChapterId` are available).
- "View in Workspace" button appears below Submitted Output image (UNDER_REVIEW tasks only).
- Links use `/series/{slug}/workspace?chapterId={id}&returnUrl=/mangaka/review-submissions`.
- Workspace back arrow returns to `/mangaka/review-submissions`.
- Tasks without `SeriesSlug`/`ChapterId` (edge case: 0 regions) gracefully hide the workspace link.

## Backend/API/DB/SP impact

**None for navigation.** Only read-model DTO fields added:
- `ChapterPageTaskDto.SeriesSlug` — populated from `series?.Slug` (existing EF include chain)
- `ChapterPageTaskDto.ChapterId` — populated from `chapter?.ChapterId` (existing EF include chain)
- `ChapterPageTaskDto.SourceChapterPageVersionId` — populated from `firstRegion?.ChapterPageVersionId` (existing EF include chain)

No new API endpoints. No DB schema changes. No stored procedure changes.

## Build result

```
dotnet build MangaManagementSystem\MangaManagementSystem.sln --no-incremental
Build succeeded.
0 Errors
57 Warnings (all pre-existing baseline, none from changed files)
```

## Manual smoke checklist

- [ ] `/mangaka/review-submissions` Back button returns to `/mangaka`
- [ ] Original Page workspace link includes `returnUrl=%2Fmangaka%2Freview-submissions`
- [ ] Submitted Output workspace link includes `returnUrl=%2Fmangaka%2Freview-submissions`
- [ ] Workspace Back arrow returns to `/mangaka/review-submissions`
- [ ] `/editor/series` -> `/series/{slug}` includes `returnUrl=%2Feditor%2Fseries`
- [ ] SeriesPage Back returns to `/editor/series` for editor flow
- [ ] SeriesPage fallback does not send editor to `/mangaka`
- [ ] `/mangaka` -> click SERIALIZED series -> `/series/{slug}?returnUrl=%2Fmangaka`
- [ ] SeriesPage Back returns to `/mangaka` for Mangaka flow
- [ ] Workspace link from ReviewSubmissions task card opens correct workspace
- [ ] Unsafe returnUrls (external, javascript:, /api/) are rejected
- [ ] No DB/SP changes

Runtime smoke not run; user must verify manually.

## Known limitations

- Workspace does not honor `page` or `version` query params — links go to the right chapter but not the exact page/version. Deep-linking is a future follow-up.
- `/dashboard` fallback is a neutral route; if a role-aware dashboard route does not exist at `/dashboard`, this may need adjustment.
- `/board-chief` and `/board` prefixes are added proactively but no source pages currently navigate to SeriesPage/Workspace from board pages.
- Assistant workspace links from `/assistant/tasks` are not implemented yet (no current linking code from assistant pages to workspace with returnUrl).

## Follow-ups

- Add `page` and `version` query parameter support to CreatorWorkspace for deep-linking
- Verify `/dashboard` route exists or update fallback to role-aware logic
- Add returnUrl from Assistant task pages when they link to workspace
