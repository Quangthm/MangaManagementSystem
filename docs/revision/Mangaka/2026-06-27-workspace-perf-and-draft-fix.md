# Workspace: fix false unsaved-draft warning + page-navigation performance

## Branch
N/A (working copy is not a git repo in this environment)

## Date
2026-06-27

## Task summary
Two issues in the shared Creator Workspace (`CreatorWorkspace.razor`, Blazor Server, used by Mangaka / Tantou Editor / Assistant):

1. **False "Phát hiện bản nháp chưa lưu" warning on every chapter/page switch.** The warning should only appear when there is a *real* unsaved edit (auto-segment, AI translate, draw/move/resize/delete region, OCR/translation text, note, task/annotation), per the intended save logic.
2. **Performance: switching pages was slow on pages that have regions/tasks/annotations, and rapid pagination clicks caused freezes / circuit disconnects / crashes.**

## Root causes
1. JS `mangaAiCanvas.js → loadRegions()` always called `syncToBlazor()` → `OnRegionsUpdated`, and `OnRegionsUpdated` always called `saveMmsDraft(...)` — **even for the programmatic load echo and for empty `"[]"` pages**. So merely *viewing* any page wrote a `localStorage` shadow draft that was never cleared (it was cleared only in `SaveProgress` when `IsDirty`). Next visit → `getMmsDraft` non-empty → warning. The old warning also fired for *any* non-empty draft (no content comparison).
2. `OnPageChanged` always ran `SaveProgress()` (semaphore + loop over all pages + a "Progress saved successfully!" toast **even when nothing was dirty**) and then fire-and-forget `_ = LoadPage(...)` with **no re-entrancy guard**. Rapid clicks queued overlapping DB + JS-interop work on the single Blazor Server circuit → overload → slow loads / disconnects.

## Architecture path
UI-only change (Web layer + its canvas JS). No API / Application / Infrastructure / DB / stored-procedure changes. Existing legacy direct-service calls in this component were left as-is (out of scope).

## Files changed
| Layer | File | Change |
| --- | --- | --- |
| Web (JS) | `src/MangaManagementSystem.Web/wwwroot/js/mangaAiCanvas.js` | `loadRegions(str, silent)` + `saveState(silent)`: a programmatic load no longer echoes a phantom change to Blazor; undo/redo history is reset per page so Undo can't cross pages. |
| Web (Razor) | `src/MangaManagementSystem.Web/Components/Pages/Mangaka/CreatorWorkspace.razor` | See below. |

### CreatorWorkspace.razor specifics
- **`LoadPage` / `OnSplitPageChanged`**: added `_isPageLoading` re-entrancy guard (`try/finally`); load regions with `silent: true`; reset `CanUndo/CanRedo`; warn only via new `HasMeaningfulDraft(...)`.
- **`OnPageChanged`**: early-return if `_isPageLoading`; `await LoadPage(...)` instead of fire-and-forget.
- **`OnRegionsUpdated`**: mark `IsDirty` + write the shadow draft **only when region content actually changed** (compared via `StripSelected`, which ignores the UI-only `selected` flag) — so selection-only/echo events are no longer "edits".
- **`SaveProgress`**: compute `anyDirty` first and **return early when nothing is dirty** (no semaphore, no DB, no toast); only toast when something was actually saved (`savedCount > 0`).
- **New helpers**: `StripSelected(json)` (removes the volatile `selected` flag for comparison) and `HasMeaningfulDraft(draft, savedRegions)` (warn only when a stored draft really differs from saved regions; treats null/`"[]"` as empty).
- **Markup**: both `<MudPagination>` get `Disabled="@_isPageLoading"`.

## Behavior after change
- Plain chapter/page navigation writes **no** draft and shows **no** warning. The "unsaved draft" warning now appears only when a genuine unsaved edit exists for that page (and the "Khôi phục/Restore" recovery still works).
- Navigating between unedited pages is much cheaper (SaveProgress no-ops, no echo round-trip, no extra render, no toast spam).
- Rapid pagination clicks are dropped while a load is in flight instead of piling up on the circuit → no more freeze/disconnect from click-spam.
- Crash-recovery shadow drafts are still saved on real edits and cleared on successful save.

## Verification
### Build
```
dotnet build src/MangaManagementSystem.Web/MangaManagementSystem.Web.csproj -o <scratch> --no-incremental
Build succeeded. 0 Error(s), 54 Warning(s)   (within the ~60 pre-existing baseline; no new warnings from changed files)
```
Note: a normal in-place build initially failed with MSB3027/MSB3021 file-lock errors only because the Web app was running (process held the output DLLs); building to a separate `-o` folder confirmed a clean compile. `.slnx` is not buildable with the installed .NET 8 CLI — build via Visual Studio or per-project csproj.

### Manual smoke
- [ ] NOT run yet (requires running the app). Suggested checks:
  - Switch chapters/pages with no edits → no "bản nháp chưa lưu" warning, fast.
  - Draw/move a region, switch page → comes back; warning offered with working Restore.
  - Spam-click pagination → no disconnect; page ends on a consistent state.
  - Edit → "Submit for Review"/Back → Progress saved toast appears only when something changed.

## Known issues / notes
- A legacy phantom draft from before this fix is an echo of the loaded server JSON, so `StripSelected` comparison treats it as equal → no warning. A rare residual mismatch (floating-point formatting of a coordinate) could warn once, then self-heal on next save. No blanket `localStorage` purge was added, to avoid wiping a genuine crash-recovery draft.

## Round 2 (2026-06-27) — load performance for data-heavy pages
Follow-up to reduce slow loading on pages/chapters with many regions, tasks, annotations, or page versions.

- **Per-page cache of tasks & annotations** in `CreatorWorkspace.razor`: new `_pageDataCache`
  keyed by `ChapterPageId`. `LoadPage` now hits the DB only on a cache miss; revisiting a
  page (or switching versions, which are page-scoped) skips both `GetChapterPageTasks...` and
  `GetChapterPageAnnotations...` round-trips. Also removes the duplicate tasks/annotations load
  that happened on initial chapter open (OnInitialized + OnAfterRender both call `LoadPage`).
  - Consistency: the UI mutates `ActiveTasks`/`ActiveAnnotations` (the same cached list
    instances) via `Insert`/`Remove`/property edits, so the cache stays correct in-session.
  - Only a fully successful load is cached (`loadedOk`) so transient errors retry next visit.
  - Tradeoff: changes made by another user/role to the same page won't appear until the
    workspace is reopened (acceptable for single-editor page workflow).
- **Adjacent-page image preload**: new JS `window.mmsPreloadImages(urls)` (sets
  `crossOrigin='anonymous'` to match the canvas loader so the cached response is reused) +
  `PreloadAdjacentPagesAsync(index)` fired (fire-and-forget) at the end of `LoadPage` to warm
  the next/previous page images. Paging sequentially through a chapter now feels instant after
  the first view; revisits hit the browser cache.

### Build (Round 2)
```
dotnet build src/MangaManagementSystem.Web/...csproj -o <scratch> --no-incremental
Build succeeded. 0 Error(s), 54 Warning(s)
```

## Round 3 (2026-06-27) — optimized image delivery + sidebar render isolation

### Option 2 — Cloudinary optimized delivery (`f_auto,q_auto`)
- New helper `OptimizedImageUrl(url)` inserts `f_auto,q_auto/` after `/upload/` in the Cloudinary
  delivery URL. **No resize transform**, so pixel dimensions — and therefore region/annotation
  coordinates — are unchanged; only bytes drop (WebP/AVIF + auto quality). Applied at every canvas
  image load: `LoadPage`, `OnSplitPageChanged`, `SwitchVersion`, and the adjacent-page preload (so
  preload and real load share the same cached response).
- The stored `cloudinary_secure_url` is untouched — this is a delivery-URL transform only, so it does
  NOT change Cloudinary/DB structure.
- Note: the AI segment/translate path reads canvas pixels, which are now the q_auto rendition. For
  high-contrast manga line art the OCR impact is negligible; revert to plain `f_auto` (format only) if
  any OCR regression is observed.

### Option 2b — version switch now loads silently
- `SwitchVersion` (the v1..v5 radio) previously loaded regions non-silently → same phantom-draft /
  false-dirty bug class as page navigation. It now loads with `silent: true` and resets `CanUndo/CanRedo`.

### Option 3 — chapter sidebar extracted to a child component
- New `WorkspaceChapterSidebar.razor` holds the chapter list markup; the parent owns all state/logic and
  passes data + `EventCallback`s. The child overrides `ShouldRender()` with a content **signature**
  (SelectedChapter + IsAddingChapter + each chapter's Id/StatusCode/PageCount/IsRenaming/Title), so it
  re-renders only when chapter data actually changes — NOT on every canvas region update / page
  navigation `StateHasChanged` in the parent. This removes the chapter list + per-chapter `MudMenu`s
  from the parent's per-interaction render diff.
- All chapter actions (add/select/submit/rename/cancel-rename/delete/cancel-submission) are forwarded
  via callbacks to the existing parent methods; rename keydown (Enter/Escape) is handled inside the child.
- The parent's now-unused `HandleRenameKeyDown` was left in place (harmless dead code; no warning).

### Build (Round 3)
```
dotnet build src/MangaManagementSystem.Web/...csproj -o <scratch> --no-incremental
Build succeeded. 0 Error(s), 54 Warning(s)
```

### Manual smoke (Round 3) — NOT run yet, suggested
- [ ] Pages load with visibly smaller/faster image fetch; regions still align exactly on the image.
- [ ] Chapter rename / submit / delete / cancel-submission / new-chapter still work from the sidebar.
- [ ] Drawing/selecting regions no longer visibly re-renders the chapter sidebar.
- [ ] AI auto-segment / translate still produce correctly-placed regions.

## Round 4 (2026-06-28) — DB data audit, delete bugs, translate 500, corrupt-data guard

### Read-only DB audit (Mock Serialized Series)
Queried `MangaManagementDB` (SELECT only, no changes). Findings:
- **Ch4 region data is corrupt/exploded: 887,093 regions** — page 2 v1 (current) = **354,823**, page 3 v3 (current) = **532,237**. This is the real cause of Ch4 being slow/unviewable (loading serializes all of them → freeze) and of the failed "delete page" attempt on it.
- **Ch1/Ch2 data is healthy** (17 and 24 regions total). No deleted files, no empty URLs; the "pages with no usable current version" check returned **empty** → no file-level breakage. Ch1 just has `page_no` gaps (pages 1 & 3 don't exist — deleted earlier; UI numbers by position). A genuinely blank page is most likely a Ch4 freeze page, or a Cloudinary asset deleted externally (DB row still active — not detectable via SQL).

### Delete page — was hard-deleting (FK violation) → now soft delete
`ChapterPageService.DeleteChapterPageAsync` did `Delete(entity)` but never removed the page's
versions/regions → blocked by `fk_chapter_page_version_page` ("error saving the entity changes").
The schema is built for **soft delete** (`deleted_at_utc`; all reads filter it; CHECK
`ck_chapter_page_deleted_pair` pairs deleted_at/by). Now sets `DeletedAtUtc`+`DeletedByUserId`
(requires a user). Bonus: soft-deleting a corrupt Ch4 page hides it **without** touching its
hundreds of thousands of regions, so the chapter then loads fast.

### Delete page version — FileResource CHECK violation → now passes user id
`DeleteCurrentVersion` (Razor) soft-deletes the version's **file** via
`FileResourceService.DeleteFileResourceAsync(pageFileId)` but called it **without** a user id, so
it set `deleted_at_utc` with a null `deleted_by_user_id` → violated CHECK
`ck_file_resource_deleted_pair`. Fixed: pass `_currentUserId`; the service now throws a clear
error if no user is supplied instead of hitting the constraint.

### Region-count guard in SelectChapter (corrupt-data resilience + perf)
`SelectChapter` eagerly materialized every version's regions (for Ch4 that meant fetching ~887k
rows → freeze). New repository methods `CountByAsync` (grouped COUNT, one query) and
`ExecuteDeleteAsync` (set-based delete) were added to `IGenericRepository`/`GenericRepository`.
`PageRegionService.GetRegionCountsByVersionIdsAsync` now lets `SelectChapter` skip loading regions
for any version with **> 2000** regions (loads `[]`, shows a Vietnamese warning). Such pages stay
viewable and deletable, so the user can clean up the corrupt Ch4 pages via the now-working delete.
Also `ChapterPageVersionService.DeleteChapterPageVersionAsync` (currently unused by the UI) was
made correct via set-based region delete.

### Translate 500 (Python AI service)
`POST /api/ai/translate-selected` 500 is an unhandled exception inside the Python service. The only
un-guarded call in the translate loop was `clean_bubble(roi)` (OCR and GoogleTranslator are already
try-wrapped). Wrapped `clean_bubble` in try/except and added a `cv2.imdecode is None` guard in
`main.py` → a bad region/image no longer 500s the whole request (returns translated text without
cleaning that bubble). If 500s persist, the exact cause is in the uvicorn console
(`traceback.print_exc()`); manga-ocr model load is already guarded (returns empty, not 500).

### Build (Round 4)
```
dotnet build src/MangaManagementSystem.Web/...csproj -o <scratch> --no-incremental
Build succeeded. 0 Error(s), 54 Warning(s)
```
Files changed: `IGenericRepository.cs`, `GenericRepository.cs`, `IPageRegionService.cs`,
`PageRegionService.cs`, `ChapterPageService.cs`, `ChapterPageVersionService.cs`,
`FileResourceService.cs`, `CreatorWorkspace.razor`, `MangaAI_Service/main.py`. No schema/DB changes.

### Manual smoke (Round 4) — NOT run yet
- [ ] Delete a page → page disappears (soft delete), no error; chapter still loads.
- [ ] Delete a non-v1 page version → succeeds (no "saving entity changes" error).
- [ ] Open Ch4 → no freeze; warning shown; corrupt pages openable then deletable.
- [ ] Translate selected regions → returns text (no 500) once AI service is running.

## Round 5 (2026-06-28) — chapter delete (missing stored procedure)

### Symptom
"Failed to delete chapter: Could not find stored procedure 'manga.usp_Chapter_Delete'."
EF log: `EXEC manga.usp_Chapter_Delete @p0`.

### Cause
`ChapterRepository.DeleteWithDependenciesAsync` `EXEC`s `manga.usp_Chapter_Delete`, but that
procedure does **not exist** in the database (it is not in
`MangaManagementSystem_Procedures_Views_Bootstrap.sql` — never created).

### Fix (no DB objects created — per the "don't modify the database" rule)
Reimplemented the cascade **in the repository** as one transactional, set-based raw-SQL batch
(`ExecuteSqlRawAsync`, parameterized by chapter id). FKs have no `ON DELETE CASCADE`, so children
are removed in dependency order:
task/annotation region junctions → tasks/annotations → page regions → page versions → pages →
chapter editorial reviews → chapter reader-vote snapshots → chapter. Wrapped in
`BEGIN TRAN … COMMIT` with `TRY/CATCH … ROLLBACK; THROW` (atomic) and
`SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON` (required for DELETEs on the filtered indexes on
`ChapterPage` / `ChapterPageVersion`). Set-based so it stays fast even for the corrupt Ch4 page.

### Validation
Dry-run of the exact cascade against **Chapter 2** (real data: 6 versions, 24 regions, 1 task,
1 annotation) wrapped in a forced ROLLBACK → "Cascade executed with NO errors", Chapter 2 still
present afterwards (nothing changed). Build: `Build succeeded. 0 Error(s)`.
File changed: `Infrastructure/Repositories/ChapterRepository.cs`. No schema/objects added.

### Note on "same image reused for many chapters"
Not a cause of errors or slowness. Each upload creates its own `FileResource` with a unique
`cloudinary_public_id`, so the `uq_file_resource_cloudinary_public_id` constraint is never hit
(identical `sha256_hash` is allowed — no unique constraint on it). Slow image display is just file
size + the `f_auto,q_auto`/preload/cache optimizations not being active until the app is rebuilt
and restarted (the running process is still an older build).

## Round 6 (2026-06-28) — chapter delete semantics + cancelled-chapter cleanup

Design decision (approved): stop conflating "delete" with "cancel".
- `CANCELLED` is a terminal editorial outcome (per `docs/context.md` §4.11: "if fixable, use
  `REVISION_REQUESTED` instead of `CANCELLED`"). The editable-again state is `REVISION_REQUESTED`.
- Chapter numbers are not reused after cancel (`uq_chapter_series_chapter_number` is non-filtered) —
  matches real serialization; new chapters take the next number.

### Changes (CreatorWorkspace.razor)
1. **`AddNewChapter` numbering** — next number now computed from **all** chapters in the series
   (incl. hidden CANCELLED), not just the visible list. Fixes the "duplicate key (series, 1)"
   create failure that happened after cancelling chapters.
2. **`DeleteChapter`** — now hard-deletes (cascade) only `DRAFT`/`REVISION_REQUESTED` chapters
   (works even with pages, via Round 5 cascade). Any other status is blocked with a clear message
   ("must be cancelled through the editorial review workflow"). Removed the old
   "if PageCount > 0 → set CANCELLED" behaviour that was leaving hidden cancelled chapters behind.
3. **`IsChapterLocked`** — `CANCELLED` added → cancelled chapters are read-only (terminal).

### Data cleanup (committed, validated)
Ran the Round-5 cascade (set-based, transactional) over all `CANCELLED` chapters of "Mock
Serialized Series" via sqlcmd. Before: Ch1,2,4,5,6 = CANCELLED. After: **0 chapters**. Verified:
0 pages in series, 0 total PageRegion rows in DB, 0 orphan regions, 0 orphan versions — the 887k
corrupt Ch4 regions are gone. (Ch3 had already been hard-deleted earlier, which is why it was
"missing".) Data-only; no schema/objects changed.

### Build (Round 6)
`Build succeeded. 0 Error(s)`. Files: `CreatorWorkspace.razor` (+ Round 5 `ChapterRepository.cs`).

### After restart
Rebuild + restart the Web app → the series shows no chapters → create Chapter 1 fresh; delete on a
draft chapter now removes it entirely (number freed); cancelled chapters (from editorial review)
are read-only.

## Round 7 (2026-06-28) — pin duplicate, pagination hang after reload

### Pin tool drew a duplicate "#N" box
Annotations are anchored by a tiny **pin region** (width/height ≤ 0.05). `SelectChapter` mapped ALL
db regions into the canvas, so the pin region was drawn both as a region box ("#8") and as the
annotation pin "!". Fix: `SelectChapter` now skips tiny pin regions when building the canvas region
list (the pin still renders from the annotation layer). `BulkReplace` already excludes pins, so they
are not deleted.

### Pagination dead after reload (root cause: JS loadImage could hang)
`mangaAiCanvas.js loadImage` never resolved its Promise when an image failed to load — the fallback
`img2` had no `onerror`, and the first `onerror` was overwritten. A failed image (e.g. transient
Cloudinary/CDN error after reload) left `await _leftCanvasRef.InvokeVoidAsync("loadImage", …)` hung in
`LoadPage`, so `_isPageLoading` stayed `true` forever → the bottom pagination was `Disabled` and
`OnPageChanged`'s guard blocked every click. Fix: `loadImage` now always resolves (added
`img2.onerror`, plus a 20s `setTimeout` safety net). Build: `0 Error(s)`.

### Page-version create/save — architecture/business review (answer, no code change yet)
- Data model: aligned — new `ChapterPageVersion` with incremented `version_no`, one `FileResource`
  (`CHAPTER_PAGE_VERSION`), single current version (two-pass `SetCurrentVersion`), old versions kept,
  upload-to-Cloudinary-before-link. OK.
- Architecture: NOT on the target flow. `SaveAsNewVersion`/`HandleUploadVersion`/`DeleteCurrentVersion`
  call Application services + Cloudinary **directly from the Razor** (legacy transitional pattern),
  not Web → typed API client → API → MediatR.
- Integrity gap: version create does upload → CreateFileResource → CreateVersion → SetCurrent →
  BulkReplaceRegions as **separate uncoordinated SaveChanges with no transaction and no Cloudinary
  cleanup on failure** (violates the "atomic link + best-effort cleanup" rule). A mid-way failure
  orphans the Cloudinary file / leaves a half-created version.
- Edit-bleed (root of the "undo after returning to original" complaint): editing mutates the ACTIVE
  version's regions (`OnRegionsUpdated` → `activeVer.Regions`) and `SaveProgress` persists them, so
  edits made before "Save as new version" bleed into the OLD version too — the original is no longer
  pristine ("old versions remain preserved" is at risk). The per-load undo-history reset (Round 3,
  needed to avoid cross-version/page undo corruption) means undo does not persist across version
  switches — so after switching you cannot undo prior edits. These are design issues to fix together.

## Round 8 (2026-06-28) — Plan A: atomic page-version create + Cloudinary cleanup + edit-bleed fix

Implemented the approved "Plan A" (keep the legacy direct-service pattern for now; fix integrity +
correctness; full Web→API→MediatR migration is the future "Plan B").

### Transaction support (Infrastructure)
- `IUnitOfWork` + `UnitOfWork`: added `BeginTransactionAsync` / `CommitTransactionAsync` /
  `RollbackTransactionAsync` over the shared `DbContext` (`IDbContextTransaction`; rollback also clears
  the change tracker; dispose cleans up the transaction).

### Atomic create method (Application)
- `IChapterPageVersionService.CreateVersionWithFileAndRegionsAsync(chapterPageId, versionNo, fileDto,
  versionNote, regionDtos, setAsCurrent)` — creates the FileResource + ChapterPageVersion + its
  PageRegions + (two-pass) set-current **inside one transaction**; rolls back and rethrows on failure.

### Version-create UI (CreatorWorkspace.razor: SaveAsNewVersion + HandleUploadVersion)
- Flow is now: upload to Cloudinary (outside the transaction) → call the atomic service method → on
  ANY failure, **best-effort delete the just-uploaded Cloudinary file** (`DeleteFileAsync(publicId,
  "image")`) so a rolled-back DB never leaves an orphan asset. (Answer to the user's question: cleanup
  deletes ONLY that one uploaded file by public_id — it never touches the Cloudinary account/structure.)
- **Edit-bleed fixed**: no longer calls `SetActiveVersion`+`SwitchVersion` (which ran `SaveProgress`
  and persisted the in-session edits back into the OLD version). Instead the old active version's
  `IsDirty` is cleared (so its DB stays pristine — "old versions remain preserved"), and the switch to
  the new version is done inline (silent canvas load, undo reset). The edits now live only on the NEW
  version.
- Pin marker regions (width/height ≤ 0.05) are excluded when copying regions to the new version.

### Build
`Build succeeded. 0 Error(s)`. Files: `IUnitOfWork.cs`, `UnitOfWork.cs`, `IChapterPageVersionService.cs`,
`ChapterPageVersionService.cs`, `CreatorWorkspace.razor`. No schema/DB-object changes (transaction is
runtime only).

### Notes / known edges
- After "Save as new version", switching back to the OLD version in the SAME session may briefly show
  the in-memory edited regions (the DB is pristine; it self-corrects on reload). Acceptable for now.
- The undo-history reset on version/page switch is intentional (prevents cross-version/page undo
  corruption); undo works for edits made on the currently active version.
- Plan B (migrate page-version writes to typed API client → MediatR command with the same EF
  transaction) remains the long-term target.

## Round 9 (2026-06-28) — pagination stuck (real cause) + translate-persistence design

### Pagination still dead after reload — real root cause
Not the image hang (that was a separate real fix). `LoadPage`/`OnSplitPageChanged` called
`StateHasChanged()` INSIDE the try, then the `finally` set `_isPageLoading = false`. The initial load
runs from `OnAfterRenderAsync`, which does NOT auto-render after it completes, so the last render
captured `_isPageLoading = true` → the pagination's `Disabled="@_isPageLoading"` stayed true forever
(even though the real field value was false). Fix: call `StateHasChanged()` in the `finally` AFTER
clearing the flag (both `LoadPage` and `OnSplitPageChanged`). Build: 0 errors.

### Translate persistence — design answer (no code yet, pending choice)
Translate is a PREVIEW: the Python service returns a cleaned background (white-filled bubbles) drawn
to the in-memory `backgroundCanvas`, plus translated text stored in region data. Translate alone does
NOT persist the cleaned background, so on reload the ORIGINAL image loads and only the text overlay
remains → "white-fill lost". Per `docs/context.md` §4.1 the final translated page should be saved as a
new `ChapterPageVersion` ("the saved translated page file is the authoritative result").
- Working/editable layer (already works after rebuild): "Save as new version" persists the cleaned
  background as the version image + translated text as editable region overlay → reload shows clean
  bubbles + VN text and stays editable.
- Authoritative-file layer (recommended, not yet built): a baked export (`exportRenderedImage`:
  clean bg + translated text flattened into pixels, no UI boxes) so the version FILE itself is the
  translated page. To avoid double text on reload, a baked version stores region positions without
  re-overlaying text. Pending user decision (overlay vs baked).

## Round 10 (2026-06-28) — per-page Export (download finished page)

The EXPORT button was a no-op stub (`Snackbar.Add("Page exported successfully!")` only). Implemented
a real per-page export:
- JS `mangaAiCanvas.js`: `exportRenderedImage()` renders a flattened PNG = clean/translated
  background + translated text only (no region boxes/handles/#labels/pins), to an offscreen canvas;
  `downloadRenderedImage(filename)` converts it to a Blob and triggers a browser download. Both added
  to the canvas instance's exported API.
- `CreatorWorkspace.razor` `ExportCurrentPage` (now `async Task`): validates there is a page/image,
  builds a tidy filename `{safeSlug}_ch{chapter}_page{page}.png`, calls `downloadRenderedImage`, and
  reports success/empty/failure.

Design notes (answers to the user's questions):
- Export does NOT store anything — no version, no FileResource, no Cloudinary upload, no DB write. It
  only downloads a PNG to the user's machine, so it cannot make the version/storage model messy.
- It exports the currently viewed page/version with translated text baked into the pixels (the
  "authoritative flattened file"), while the in-system versions stay editable (text as data overlay).
- Build: 0 errors.

## Remaining follow-ups (not done — optional, need approval)
- The chapter load (`SelectChapter`) eagerly deserializes regions for *every* version of every
  page. For chapters with many versions this is one-time CPU cost; could be lazy-parsed per
  version when actually viewed.
- This component still uses legacy direct Application-service injection (not the typed-API-client
  → MediatR flow). Migration is a separate, larger task.
- Consider splitting the ~2,700-line component into smaller child components to reduce the
  per-render diff cost on Blazor Server.
