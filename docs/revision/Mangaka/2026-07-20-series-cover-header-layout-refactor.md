# Series Cover / Header Layout Refactor

**Date:** 2026-07-20
**Branch:** `feature/Mangaka`
**Status:** Implemented; build verified

## Goal

Detach the series cover from the white metadata card on the `/series/{slug}` page. The cover should be an independent visual element that can be larger on desktop, is not controlled by metadata height, and shows the full cover artwork without cropping.

## Layout change

### Before

```
.series-page-grid
  └── White card (cover + metadata together)
        ├── Cover (220×310px, object-fit:cover, may crop)
        └── Metadata
  └── Chapter list
  └── Contributor sidebar
```

### After

```
.series-page-grid
  └── .series-header-row (grid-column:1, inner grid)
        ├── .series-cover-column (independent, no card)
        │     ├── img (width:100%; height:auto; no crop)
        │     └── fallback (aspect-ratio:2/3 placeholder)
        │
        └── .series-metadata-card (independent white card)
              └── (all existing metadata: title, badges, genres, tags, etc.)
  └── Chapter list
  └── Contributor sidebar
```

## CSS additions

All added to the existing `<style>` block in `SeriesPage.razor`:

- `.series-header-row` — inner grid, `minmax(200px, 280px) minmax(0, 1fr)`, gap 24px, breakpoint 680px
- `.series-cover-column` — flex column container
- `.series-cover-image` — `width:100%; height:auto; display:block; border-radius:14px`
- `.series-cover-placeholder` — `aspect-ratio:2/3`, flex-centered fallback
- `.series-metadata-card` — white card: `background:#fff; border; border-radius; padding:28px`

## Removed

- Old white card wrapper (`background:#fff; border; border-radius`) that enclosed both cover + metadata
- Old cover wrapper fixed sizing (`width:220px; height:310px`)
- Old image fixed sizing (`width:220px; height:310px; object-fit:cover`)
- Old flex row wrapper (`display:flex; gap:0; align-items:flex-start`)

## Responsive

- Desktop: cover left, metadata right
- At `max-width: 680px`: cover stacks above metadata, single column
- Cover max-width constrained to 280px on stacked layout

## Files changed

| File | Status |
|------|--------|
| `Components/Pages/Series/SeriesPage.razor` | Modified |

## Protected scope

No changes to: ExpandableChipList, chapter list, chapter pagination, contributor panel, contributor filters, lifecycle buttons, lifecycle alerts, synopsis, Mangaka display, backend files, DTOs, database.

## Build

```
0 Error(s)
68 Warning(s) — all pre-existing
git diff --check → clean (only trailing-whitespace in unrelated docker-compose.yml)
```
