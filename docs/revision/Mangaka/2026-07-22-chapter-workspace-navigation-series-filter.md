# Mangaka Chapter Workspace Navigation and Series Filter Fix

## Branch

`feature/Mangaka`

## Date

2026-07-22

## Task summary

Updated Chapter Submission Management so the Series filter visibly defaults to `All Series` and eligible chapter cards can open the exact chapter in the existing creator workspace with safe return navigation.

## Architecture path

Web-only UI/navigation state change. Existing typed-client and backend authorization paths are unchanged.

## Files changed

| Layer | File | Change |
| --- | --- | --- |
| Web | `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Mangaka/Chapters.razor` | Added explicit All-Series filter sentinel and guarded chapter-scoped workspace action. |
| Docs | `docs/revision/Mangaka/2026-07-22-chapter-workspace-navigation-series-filter.md` | Recorded implementation and verification evidence. |

## DB/SP impact

None. No API, typed-client, Application, Infrastructure, Domain, DTO, SQL, stored-procedure, or migration changes.

## Behavior changed

- The Series filter uses `Guid.Empty` as the explicit `All Series` value and is no longer clearable to a blank normal state.
- Series and Status filter changes continue to reset chapter-card pagination to page 1.
- `DRAFT`, `REVISION_REQUESTED`, `UNDER_REVIEW`, `APPROVED`, `SCHEDULED`, `ON_HOLD`, and `RELEASED` cards show `View Workspace` when `SeriesSlug` is present.
- Workspace links use `/series/{slug}/workspace?chapterId={chapterId}` and `SafeReturnUrl.AppendReturnUrl` with the current page URL.
- `CANCELLED` cards and chapters with a missing/blank slug do not show the workspace action.

## Verification

### Build

Command:

```text
dotnet build MangaManagementSystem/src/MangaManagementSystem.Web/MangaManagementSystem.Web.csproj
```

Result: blocked by environment NuGet connectivity. Restore/build reported `NU1301` because `https://api.nuget.org/v3/index.json` could not be reached. An offline-cache attempt with `RestoreIgnoreFailedSources=true` did not produce a successful build. The earlier restore attempt compiled Domain, Application, and Infrastructure and reported existing nullable warnings, but did not provide a successful final Web build result.

### Static checks

- Inspected the targeted Razor diff.
- Confirmed the select, items, backing field, handler, and filtering condition consistently use `Guid`.
- Confirmed both filter handlers retain `ResetChapterCardsPage()`.
- Confirmed workspace URLs use the card's `ChapterId`, require a nonblank slug, and append `CurrentUrl` through `SafeReturnUrl`.
- Confirmed existing Edit, Submit for Review, calendar, and Review History blocks are unchanged.

### Manual smoke

Not run; the application was not started.

## Known issues

- A successful Web project build remains required once NuGet connectivity is available.
- Runtime UI/navigation checks remain required.

## Follow-ups

- Re-run the Web project build in an environment that can reach NuGet.
- Smoke-test the Series/Status filter combinations and workspace/back-navigation behavior.
