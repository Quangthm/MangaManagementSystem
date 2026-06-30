# Proposal UI Text and Revision State Fix — 2026-06-29

## Branch
`feature/Mangaka`

## Problem
Runtime testing showed:
- Publication Frequency dropdown displayed mojibake text for the optional "Not set" option.
- Proposal submission snackbar displayed mojibake around the dash.
- Proposal detail showed a false warning when proposal status was `REVISION_REQUESTED` and series status was `PROPOSAL_DRAFT`.

## Fix
- Replaced the Publication Frequency optional option text with ASCII-safe `Not set`.
- Replaced the proposal submission snackbar text with ASCII-safe punctuation.
- Updated proposal/series state mismatch logic so `REVISION_REQUESTED` proposal + `PROPOSAL_DRAFT` series is treated as an expected state pair.
- Did not change the backend revision workflow because returning the series to `PROPOSAL_DRAFT` is correct.

## Files Changed
- `src/MangaManagementSystem.Web/Components/Pages/Mangaka/MangakaDashboard.razor`
- `src/MangaManagementSystem.Web/Components/Pages/Editor/ProposalReviewDetail.razor`

## Build Result
```text
dotnet build MangaManagementSystem\MangaManagementSystem.slnx --no-incremental
Build succeeded
0 errors
47 warnings
```

## Manual Test Checklist
- [ ] Open the proposal/series form with Publication Frequency dropdown.
- [ ] Confirm optional value displays as `Not set`.
- [ ] Submit a proposal.
- [ ] Confirm snackbar has no mojibake text.
- [ ] Open a revision-requested proposal whose series is `PROPOSAL_DRAFT`.
- [ ] Confirm the false mismatch warning does not appear.
- [ ] Confirm real unexpected stale/mismatch warnings still appear when applicable.
