# Phase 6 - Admin File Management SQL and Backend Checkpoints

**Date:** 2026-06-20
**Branch:** `feature/phase5-admin-account-management`
**Current HEAD:** `32b6de1ad94a5e2061bf54a68be6b3365bc1e6cc`
**Pull Request:** PR #64
**Status:** SQL and backend checkpoints completed and runtime smoke-tested; not staged, committed, or pushed.

## Context

`origin/main` was force-updated during Phase 6 while the database was being transitioned to add genre and tag tables and related procedures.

The feature branch was synchronized in the required direction:

```text
origin/main
-> feature/phase5-admin-account-management
```

A local merge commit was created:

```text
32b6de1 merge(main): sync latest database transition before Phase 6
```

The merge commit has two parents:

- Previous feature HEAD: `bde99f8d5d7c6f26a43b921e6f31658feef2ba2e`
- Synchronized `origin/main`: `9a103a02af282a6a7e6465143a10a5e0f3e3cd25`

No merge or push was performed directly into `main`.

## Scope Completed

This checkpoint implements the database and C# backend foundation for Phase 6 Admin File Management:

```text
Database / Stored Procedures
-> Repository
-> Application / CQRS
-> API Controller
-> Controlled Preview and Download
-> Backend Authorization
-> Soft Delete
-> Audit
```

The Admin Web UI and typed Web API client are not included yet.

## Database Changes

Updated:

- `MangaManagementSystem_Procedures_Views_Bootstrap.sql`

Added:

- `manga.usp_Admin_FileResource_Search`
- `manga.usp_Admin_FileResource_GetById`
- `manga.usp_Admin_FileResource_SoftDelete`

### Database Behavior

`manga.usp_Admin_FileResource_Search`:

- Requires an `ACTIVE Admin` actor.
- Supports metadata search.
- Supports file-purpose filtering.
- Supports `ACTIVE`, `DELETED`, and `ALL` deletion-state filtering.
- Supports uploaded date-range filtering.
- Supports pagination.
- Does not expose a physical storage path.

`manga.usp_Admin_FileResource_GetById`:

- Requires an `ACTIVE Admin` actor.
- Returns metadata required by controlled content endpoints.
- Does not expose physical local storage paths to the Admin-facing response.

`manga.usp_Admin_FileResource_SoftDelete`:

- Requires an `ACTIVE Admin` actor.
- Requires a non-empty deletion reason after trimming.
- Delegates the mutation to the existing business procedure `manga.usp_FileResource_SoftDelete`.
- Does not physically delete the Cloudinary asset.
- Does not call the generic audit append procedure from C#.
- Produces exactly one `FILE_RESOURCE_SOFT_DELETED` audit event through the database business flow.

### Database Transition Compatibility

The Phase 6 SQL was rebuilt on top of the post-merge bootstrap.

Preserved from the latest main transition:

- New genre and tag database work.
- `manga.usp_ChapterPageAnnotation_UpdateText`.
- The hardened `auth.usp_Admin_ChangeUserStatus`.
- Removal of the procedure removed by main.

Not changed by Phase 6:

- `manga.usp_Series_Create`.
- The in-progress Create Series database transition.
- Public role registration behavior.

Testing followed the leader instruction not to depend on procedures currently being migrated.

## Backend Files

Added:

- `MangaManagementSystem/src/MangaManagementSystem.API/Contracts/AdminFileRequests.cs`
- `MangaManagementSystem/src/MangaManagementSystem.API/Controllers/AdminFilesController.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/DTOs/Admin/AdminFileDtos.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Admin/Files/Commands/AdminFileCommandHandlers.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Admin/Files/Commands/AdminFileCommands.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Admin/Files/Queries/AdminFileQueries.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Application/Features/Admin/Files/Queries/AdminFileQueryHandlers.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Domain/Interfaces/IFileResourceRepository.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/Repositories/FileResourceRepository.cs`

Updated:

- `MangaManagementSystem/src/MangaManagementSystem.API/Program.cs`
- `MangaManagementSystem/src/MangaManagementSystem.Infrastructure/DependencyInjection.cs`

## API Endpoints

Added:

```text
GET  /api/admin/files
GET  /api/admin/files/{fileResourceId}
GET  /api/admin/files/{fileResourceId}/preview
GET  /api/admin/files/{fileResourceId}/download
POST /api/admin/files/{fileResourceId}/soft-delete
```

## Security and Architecture

Implemented:

- Internal API key validation.
- Actor user ID validation.
- Actor role validation.
- Database verification that the actor is an `ACTIVE Admin`.
- Backend enforcement rather than UI-only button hiding.
- No direct C# call to `audit.usp_AuditEvent_Append`.
- No physical file deletion.
- Controlled content proxy through authenticated API endpoints.
- Storage locator fields are not returned by the Admin detail DTO.
- Deleted files cannot be downloaded.
- Missing or deleted preview returns a safe SVG placeholder.
- Missing download returns `404`.
- Deleted download returns `410`.
- Repeated soft delete returns `409`.
- Blank deletion reason returns `400`.

## Build Results

### Merge-State Build

Passed before applying Phase 6 backend:

```text
Build succeeded.
53 Warning(s)
0 Error(s)
```

### Phase 6 Backend Build

Passed after adding Repository, CQRS, API, and controlled content:

```text
Build succeeded.
53 Warning(s)
0 Error(s)
```

The warning count did not increase after adding the Phase 6 backend.

Warnings are existing solution warnings involving nullable analysis, MudBlazor analyzers, lowercase placeholder class names, obsolete APIs, and unrelated modules. They were not modified because they are outside the Phase 6 scope.

## Runtime Smoke Tests

### API Startup

Passed:

```text
https://localhost:7256
http://localhost:5234
```

Swagger GET returned HTTP `200`.

### Missing Authentication

Passed:

```text
GET /api/admin/files
-> HTTP 401
-> unauthorized_internal_request
```

### Authorization and Search

Passed:

- Valid internal key with non-Admin role returned `403`.
- Active Admin returned `200`.
- Search returned page 1 with page size 5.
- Local test database reported 88 files and 18 pages.
- Search returned both active and deleted metadata.
- Search did not modify data.

### Detail and Controlled Content

Passed:

- Active file detail returned `200`.
- Detail response did not expose Cloudinary public ID, Cloudinary secure URL, physical path, storage path, or local path.
- Active PDF preview returned `200` with a valid `%PDF` signature.
- Controlled PDF download returned `200` with attachment content disposition.
- Deleted-file preview returned a safe SVG placeholder.
- Deleted-file download returned `410`.
- Missing-file preview returned a safe SVG placeholder.
- Missing-file download returned `404`.
- No file was modified or deleted by these GET tests.

### Soft Delete, Actor Permission, and Audit

A temporary `FileResource` row was created only for the smoke test.

Passed:

- Non-Admin role was denied with `403`.
- The denied request made no database change and created no audit event.
- Blank deletion reason was rejected with `400`.
- The blank-reason request made no database change and created no audit event.
- Active Admin soft-delete returned `200`.
- `deleted_at_utc` was persisted.
- `deleted_by_user_id` matched the Admin actor.
- Exactly one `FILE_RESOURCE_SOFT_DELETED` audit event was created.
- The audit event matched the actor, role, and deletion reason.
- Repeated soft delete returned `409`.
- Repeated delete created no duplicate audit event.
- The temporary FileResource and its test audit event were removed.
- No real application file was modified or deleted.

## Current Git State

Expected changed scope:

- Bootstrap SQL.
- Phase 6 revision.
- Two updated backend registration files.
- Nine new backend files.

Current work remains:

- Unstaged.
- Uncommitted.
- Unpushed.

The original Phase 6 safety stash remains preserved and has not been applied or dropped.

## Backups

Created during the work:

- Backup branch at the original Phase 5 checkpoint.
- Backup branch after the latest main merge:
  - `backup/phase6-post-main-merge-32b6de1`
- Conflict-file backup on Desktop.
- Post-merge SQL backup on Desktop.
- Backend Checkpoint 2 backup on Desktop.
- Phase 6 safety stash:
  - `wip: phase6 sql checkpoint before main 9a103a02`

## Current Result

Phase 6 database and backend support for Admin File Management is complete and runtime smoke-tested:

- Real database list and filtering.
- Admin-only metadata detail.
- Controlled preview.
- Controlled download.
- Safe missing/deleted placeholders.
- Permission-controlled soft delete.
- Required deletion reason.
- Exactly one database audit event.
- No physical file deletion.
- No storage path exposure.

## Remaining Work

- Add the typed Web API client.
- Add the `/admin/files` Admin UI.
- Add Admin navigation.
- Enforce and test direct-URL authorization in the Web application.
- Test Admin and non-Admin behavior through the UI.
- Update Business Flows, Functional Requirements, User Stories, and UI Specification.
- Synchronize REJECTED and GUID guides.
- Update `Proc_Test_Script.sql`.
- Create `Auth_Workflow_Test_Script.sql`.
- Test that direct requests cannot create Admin outside the approved registration workflow.
- Run Board Chief and Board Member regression tests.
- Run logout regression tests.
- Run the final full-solution build.
- Verify latest main and PR #64 mergeability before push.
- Stage, commit, and push only after explicit approval.

## Final validation - 2026-06-21

### Final scope

- Implemented Admin File Management search, detail, controlled preview, controlled download, and audited soft delete.
- Added the Admin-only /admin/files workspace and navigation entry.
- Preserved the database-transition content merged from origin/main.
- Did not modify the Create Series procedure during the Phase 6 reapply.
- Added shared workspace layout styles required by Admin, Editor, Assistant, Dashboard, and Ranking layouts.

### SQL verification

- manga.usp_Admin_FileResource_Search: verified.
- manga.usp_Admin_FileResource_GetById: verified.
- manga.usp_Admin_FileResource_SoftDelete: verified.
- Blank delete reasons are rejected.
- Soft delete persists the deleted state and creates exactly one matching audit event.
- Repeated soft delete is rejected without creating a duplicate audit event.

### API verification

- Requests without the internal API key are rejected.
- A valid internal key with a non-Admin actor is rejected with HTTP 403.
- An active Admin can search and retrieve file details.
- File detail responses do not expose storage locator fields.
- Controlled preview and download return file content through authorized API endpoints.
- Deleted-file download is rejected.
- Missing-file download returns not found.

### Web verification

- Admin dashboard and /admin/files render correctly.
- Search, purpose, state, date, page-size, and pagination controls render correctly.
- File details, preview, and download were manually verified.
- PDF preview opens in a new tab without the obsolete popup error.
- Downloaded files open correctly.
- File-purpose labels no longer overlap the selected value.
- Editor and Assistant layouts render correctly after restoring shared workspace CSS.
- Non-Admin users navigating directly to /admin/files receive Access Denied.

### Final repository state

- Branch: eature/phase5-admin-account-management
- Base merge commit: 32b6de1ad94a5e2061bf54a68be6b3365bc1e6cc
- Safety stash remains preserved.
- Full solution build completed successfully.
- No files were staged, committed, or pushed during validation.