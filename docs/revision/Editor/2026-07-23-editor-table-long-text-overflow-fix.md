# Editor Table Long-Text Overflow Fix

## Problem and root cause

- `/editor/chapters`: the MudTable used automatic table layout and the Title cell had no overflow boundary, so long unbroken chapter titles could increase the table's intrinsic width.
- `/editor/proposals`: submitter and reviewer grid items retained intrinsic minimum sizing and had no overflow containment, allowing long names to leak into neighboring columns.

## Files changed

- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Editor/ChapterReviewList.razor`
- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Shared/SeriesProposalTable.razor`
- `docs/revision/Editor/2026-07-23-editor-table-long-text-overflow-fix.md`

## Solution

- Chapter table: applied a component-specific fixed table layout, stable column widths, and a shrinkable single-line Title element with ellipsis and an HTML `title` tooltip containing the full title.
- Proposal table: made Submitted By shrinkable and added overflow containment, single-line ellipsis, and a full-name `title` tooltip.
- Reviewer: applied the same defensive treatment because it had the identical unbounded person-name behavior.
- Underlying title/name values and all search, filter, sort, pagination, navigation, and action behavior remain unchanged.

## Impact and verification

- Backend/API/authentication impact: none.
- Static verification: scoped diff reviewed; `git diff --check` and final status recorded in the completion report.
- Build: NOT RUN — user will build.
- Manual black-box functional testing: NOT RUN — user will test.
- Commit/push: not performed.
