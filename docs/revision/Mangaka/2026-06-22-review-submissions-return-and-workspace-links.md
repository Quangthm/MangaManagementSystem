# Review Submissions — Return Navigation and Workspace Links

## Branch

`feature/Mangaka`

## Date

2026-06-22

## Task summary

Added Back button and workspace link buttons to the `/mangaka/review-submissions` page. Back button returns to `/mangaka`. Workspace links open the task's series/chapter workspace with `returnUrl=/mangaka/review-submissions` so workspace back arrow returns to this page.

## Files changed

| Layer | File | Change |
|-------|------|--------|
| Web | `Components/Pages/Mangaka/ReviewSubmissions.razor` | Added Back button; added "View in Workspace" buttons below Original Page and Submitted Output images; added `BuildWorkspaceLink()` helper; added `@using MangaManagementSystem.Web.Services` |
| Application | `DTOs/Manga/ChapterPageTaskDtos.cs` | Added `SeriesSlug?`, `ChapterId?`, `SourceChapterPageVersionId?` |
| Application | `Services/ChapterPageTaskService.cs` | Populated new fields from existing EF include chain in both mappers |

## Backend/API/DB/SP impact

None. Read-model DTO fields only.

## UI behavior

- "Back to Mangaka Dashboard" button above page title, navigates to `/mangaka`.
- "View in Workspace" link under Submitted Output image (UNDER_REVIEW tasks with SeriesSlug/ChapterId).
- "View in Workspace" link under Original Page image (all tasks with SeriesSlug/ChapterId).
- Links use `SafeReturnUrl.AppendReturnUrl()` for safe URL construction.
- Tasks without series/chapter context gracefully hide workspace links.

## Build result

```
Build succeeded. 0 errors, 57 warnings (baseline).
```

## Manual smoke

Runtime smoke not run; user must verify manually.

- [ ] Back button visible above "Review Assistant Submissions" heading
- [ ] Back button navigates to `/mangaka`
- [ ] "View in Workspace" appears below Original Page image when data available
- [ ] "View in Workspace" appears below Submitted Output image when data available
- [ ] Workspace opens with correct series and chapter context
- [ ] Workspace back arrow returns to `/mangaka/review-submissions`
