# Mangaka Series Card — Left-Side Portrait Cover Layout

## Branch

`feature/Mangaka`

## Date

2026-06-22

## Task summary

Changed the Mangaka current series cards from a top-banner cover layout to a left-side portrait cover layout. Added tag chips to cards mirroring the genre chip pattern.

## Files changed

| Layer | File | Change |
|-------|------|--------|
| Web | `Components/Pages/Mangaka/MangakaDashboard.razor` | Redesigned the series card template: left-side portrait cover column (120px wide, 2:3 object-fit), right-side content column (title, genres, tags, status, updated time, action buttons). Added tag chip display with +N more behavior. |

## Backend/API/DB/SP impact

None. `Tags` already present on `SeriesCardData` (IReadOnlyList<TagDto>). No DTO or contract changes needed.

## Old card layout (top banner)

```
┌──────────────────────────┐
│  [wide banner cover]     │ ← 130px height, cover cropped to banner
├──────────────────────────┤
│  accent bar              │
│  title                   │
│  genre chips + status    │
│  Updated N mins ago      │
│  View Draft | Submit     │
└──────────────────────────┘
```

## New card layout (left portrait cover)

```
┌──────┬─────────────────────────────┐
│      │ accent bar                  │
│ cover│ title                       │
│ 120× │ genre chips +N more         │
│ 180  │ tag chips +N more           │
│      │ status ● Updated N mins ago │
│      │ View Draft | Submit         │
└──────┴─────────────────────────────┘
```

- Cover column: 120px wide, full card height, object-fit: cover, 2:3 portrait preserved
- No-cover placeholder: MenuBook icon centered
- Card min-height: 210px
- Existing grid layout unchanged: `grid-template-columns: repeat(auto-fill, minmax(280px, 1fr))`

## Genre +N more preservation

- Same `Take(3)` visible limit
- Same `+N more` chip with tooltip showing remaining genre names
- Same genre chip styling (indigo background)
- Same overflow count calculation

## Tag display

- Mirrors genre display pattern: `Take(3)` visible, `+N more` overflow
- Green-tinted chip style (`#f0fdf4` background, `#166534` text)
- Hidden entirely when no tags exist
- Same tooltip behavior on `+N more` chip
- Uses existing `series.Tags` from `SeriesCardData`

## Build result

```
dotnet build MangaManagementSystem/MangaManagementSystem.sln --no-incremental
0 errors, 57 warnings (all pre-existing baseline)
0 new changed-file warnings
```

## Manual smoke

Runtime smoke not run; user must verify manually.

```
[ ] Mangaka current series cards show left-side portrait cover
[ ] Cover is not displayed as a top landscape banner anymore
[ ] Cover keeps 2:3 ratio
[ ] No-cover placeholder still works
[ ] Genre chips still show "+N more" exactly as before
[ ] Tag chips show with "+N more" behavior
[ ] No tags → tag row hidden
[ ] Cards remain responsive on narrow screen
[ ] VIEW DRAFT still works
[ ] SUBMIT still works
[ ] More menu still works
[ ] Proposal filters / genre-tag filters still work
[ ] Create/Edit Draft modal still works
[ ] Cover crop dialog still works
```
