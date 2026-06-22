# Editor Workspace Return Navigation

## Branch
`feature/Mangaka`

## Date
2026-06-19

## Task Summary
Fixed the workspace back-arrow behavior when a Tantou Editor opens a chapter via the Editor
review flow. The workspace now supports an optional `returnUrl` query parameter that controls
where the back arrow navigates. When present and safe, it is used; otherwise the back arrow
falls back to `/series/{slug}`.

## Issue Fixed
When a Tantou Editor clicked "Review in Workspace" from the Editor chapter review detail or
list page, the workspace opened correctly at:

`/series/{slug}/workspace?chapterId={chapterId}`

But inside the workspace, the back arrow always returned to `/series/{slug}`, which is the
series landing page — wrong for the Editor review flow. The editor expected to return to the
Editor chapter review page.

## Files Changed

| File | Change |
|------|--------|
| `src/MangaManagementSystem.Web/Components/Pages/Mangaka/CreatorWorkspace.razor` | Added `[SupplyParameterFromQuery] public string? returnUrl` parameter. Added `BackHref` computed property, `BuildSafeReturnUrl()`, and `IsSafeLocalReturnUrl()` safety validator. Updated both back-navigation targets (access-denied button on line 27, header back arrow on line 39) from hard-coded `/series/{Slug}` to `BackHref`. |
| `src/MangaManagementSystem.Application/Features/Editor/ChapterReviews/Queries/GetEditorChapterReviewDetail/GetEditorChapterReviewDetailQueryHandler.cs` | Workspace URL now includes `&returnUrl=%2Feditor%2Fchapters%2F{chapterId}` (URL-encoded) |
| `src/MangaManagementSystem.Application/Features/Editor/ChapterReviews/Queries/GetEditorChapterReviewQueue/GetEditorChapterReviewQueueQueryHandler.cs` | Workspace URL now includes `&returnUrl=%2Feditor%2Fchapters` (URL-encoded) |

## Old Behavior
- Workspace back arrow always navigated to `/series/{slug}` regardless of entry context.
- Editor review flow: "Review in Workspace" → workspace → back arrow → series page (wrong).

## New Behavior
- **From Editor detail page**: "Review in Workspace" opens
  `/series/{slug}/workspace?chapterId={id}&returnUrl=%2Feditor%2Fchapters%2F{id}`.
  Back arrow returns to `/editor/chapters/{id}`.
- **From Editor list page**: "Review Chapter" opens
  `/series/{slug}/workspace?chapterId={id}&returnUrl=%2Feditor%2Fchapters`.
  Back arrow returns to `/editor/chapters`.
- **From series page or direct URL without returnUrl**: Back arrow still returns to
  `/series/{slug}` (unchanged fallback).

## returnUrl Safety Rule
The `IsSafeLocalReturnUrl` method enforces:
1. Must not be null or whitespace.
2. Must start with `/` (local path only).
3. Must not start with `//` (prevents protocol-relative URLs like `//evil.com`).
4. Must match a known internal route prefix:
   - `/editor/chapters`
   - `/editor/proposals`
   - `/series/`

Any value failing these checks is silently ignored and the fallback `/series/{slug}` is used.

## Routes Affected
- `/series/{slug}/workspace` — workspace page (reads `returnUrl` query param)
- No routes were added, removed, or renamed.
- `/mangaka/workspace/{SeriesId}` was NOT restored.
- `AnnotationWorkspace.razor` was NOT used.

## Build Result
```
dotnet build MangaManagementSystem\MangaManagementSystem.sln
```
- **Build succeeded**
- **0 errors**
- **66 warnings** (all pre-existing; filtered check for changed files produced no output)

## Manual Test Checklist
1. [ ] Login as active Tantou Editor contributor.
2. [ ] Open `/editor/chapters`.
3. [ ] Open a scoped chapter detail.
4. [ ] Click "Review in Workspace".
5. [ ] Workspace opens at `/series/{slug}/workspace?chapterId={id}&returnUrl=%2Feditor%2Fchapters%2F{id}`.
6. [ ] Click the workspace back arrow.
7. [ ] It returns to `/editor/chapters/{id}`, not `/series/{slug}`.
8. [ ] From the chapter list, click "Review Chapter" directly.
9. [ ] Back arrow returns to `/editor/chapters`.
10. [ ] Open workspace directly without `returnUrl` (e.g. `/series/{slug}/workspace?chapterId={id}`).
11. [ ] Back arrow returns to `/series/{slug}` (fallback).
12. [ ] Try unsafe returnUrl values:
    - `returnUrl=https://evil.com` — ignored, falls back to `/series/{slug}`.
    - `returnUrl=//evil.com` — ignored, falls back to `/series/{slug}`.
    - `returnUrl=javascript:alert(1)` — ignored, falls back to `/series/{slug}`.
    - Empty `returnUrl=` — ignored, falls back to `/series/{slug}`.
13. [ ] Confirm `/mangaka/workspace/{SeriesId}` is not restored.
14. [ ] Confirm `AnnotationWorkspace.razor` is not used.
15. [ ] Build succeeds with 0 errors.

## Remaining Tasks
- No remaining tasks for this fix.
- If future navigation contexts (e.g. board review, admin) need workspace return support,
  add their route prefixes to `IsSafeLocalReturnUrl`.
