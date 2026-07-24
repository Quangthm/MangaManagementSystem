# Workspace Canvas Initialization Lifecycle Regression

Date: 2026-07-24  
Branch: `feature/Mangaka`

## Runtime failure

A Tantou Editor opening Creator Workspace remained on the initialization spinner. The fatal browser error was:

`TypeError: Cannot read properties of null (reading 'getContext')`

The exact JavaScript failure occurred in `mangaAiCanvas.js` when `initCanvas` called `canvas.getContext("2d")` after `document.getElementById(canvasId)` returned `null`.

The calling C# lifecycle path was `CreatorWorkspace.OnAfterRenderAsync(bool firstRender)`, which initialized both canvas instances whenever `firstRender` was true.

## Root cause

The workspace initialization UI changed the component's first rendered output. While `_isWorkspaceInitializing` is true, Razor renders only the spinner; the `ai-canvas-left`, `ai-canvas-right`, and their containers are absent.

`firstRender` identifies the component's first output, not the first output containing canvas markup. Calling `initCanvas` during that spinner-only render threw an unhandled `JSException`, terminated the Blazor Server circuit, and left the browser displaying the last successfully rendered spinner DOM.

## Correction

Canvas setup is no longer controlled by `firstRender`. `OnAfterRenderAsync` now requires all of the following:

- workspace initialization has completed;
- access was not denied;
- no workspace load error is active;
- `_canvasInitialized` is false.

That condition corresponds to the Razor branch containing both canvas elements. Initialization imports the module, creates both instances and .NET references, and initializes both DOM canvases. `_canvasInitialized` becomes true only after both `initCanvas` calls succeed.

If pages were loaded during `OnInitializedAsync`, the selected page is loaded immediately after successful delayed initialization. Mangaka, Tantou Editor, and Assistant continue to use the same canvas path.

## Defensive JavaScript validation

`mangaAiCanvas.js` now verifies both the requested canvas and container elements before calling `getContext`. A missing element throws a controlled message identifying the canvas/container IDs instead of causing a null-property exception.

## Circuit-safe failure handling and retry

`OnAfterRenderAsync` catches JavaScript interop and invalid-operation failures at the canvas initialization boundary. It:

- leaves `_canvasInitialized` false;
- records a descriptive message in the existing workspace load-error state;
- reports the exception to stderr;
- requests a render so the existing Retry/Back panel is displayed.

Retry already performs a forced navigation to the current route, reconstructing the component and naturally retrying initialization.

## `/pages/counts` 401 observation

Static inspection found no unique page-client authorization defect:

- `IMangakaChapterApiClient`: one `ApiAuthorizationMessageHandler`
- `IMangakaPageApiClient`: one `ApiAuthorizationMessageHandler`
- `IMangakaPageRegionApiClient`: one `ApiAuthorizationMessageHandler`
- `IMangakaTaskApiClient`: one `ApiAuthorizationMessageHandler`
- `IMangakaAnnotationApiClient`: one `ApiAuthorizationMessageHandler`

The Page counts action retains the approved shared roles, JWT actor resolution, and resource guard. No authentication or authorization changes were made for the observed 401. It should be retested after the fatal circuit regression is removed.

## Scope preservation

- Shared workspace authorization matrix: unchanged.
- `IWorkspaceResourceAuthorizationService`: unchanged.
- Deferred `SeriesController.GetWorkspaceEntryAsync` and `SeriesApiClient.GetWorkspaceEntryAsync`: unchanged.
- `X-Actor-User-Id`: not restored or migrated.
- Schema, SQL, migrations, and stored procedures: unchanged.

## Files changed

- `MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Workspace/CreatorWorkspace.razor.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Web/wwwroot/js/mangaAiCanvas.js`
- `docs/revision/Editor/2026-07-24-workspace-canvas-initialization-regression.md`

## Verification

- Targeted lifecycle, DOM-ID, initialization-flag, JavaScript, and HTTP-handler searches: performed.
- `git diff --check`: passed; only line-ending conversion warnings were reported.
- Build: **NOT RUN — user will build**.
- Tests/application/browser automation: **NOT RUN — per instruction**.
- Manual black-box testing: **NOT RUN — user will test**.
- Commit/push: none.
