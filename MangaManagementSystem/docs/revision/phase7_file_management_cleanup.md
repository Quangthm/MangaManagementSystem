# Phase 7 - Admin File Cleanup

## Scope

Implement Admin file cleanup according to option 2.

## Final direction from leader

- Keep the existing database schema.
- Do not add cleanup tracking fields.
- Do not show cleanup status/state as a stored database value.
- Admin list/reload/search reads database records only.
- Storage provider is called only when Admin clicks `Cleanup` or `Cleanup All`.

## Implementation summary

- Added `/admin/files` page.
- Enabled File Management entry from Admin Dashboard.
- Added DB-only file listing, search, purpose filtering and pagination.
- Kept normal file action: `Open`.
- Added `Cleanup` action for one soft-deleted file.
- Added `Cleanup All` action for a batch of soft-deleted files.
- Cleanup candidates are selected by:
  - `deleted_at_utc IS NOT NULL`
- Cloudinary delete result `ok` or `not found` is treated as cleanup success for the current action.
- Cleanup result is returned to the UI through the action response only.
- No cleanup status/state is persisted to database.

## Database note

No real database changes were executed.

The PR keeps the existing database schema as-is and does not modify:
- `MangaManagementSystem_Schema.sql`
- `MangaManagementSystem_Procedures_Views_Bootstrap.sql`

## Validation

- Cleanup flow no longer depends on a database cleanup-status field.
- Extra cleanup tracking fields were removed from code and UI.
- Cleanup-state dropdown/column was removed from Admin File Management UI.
- Build succeeded with existing warnings.


## Latest UI Alignment

The Admin File Management page was aligned with the previous file-management workflow:

- Restored Details action.
- Restored Preview action.
- Restored Download action.
- Restored Soft Delete action.
- Kept Cleanup action for soft-deleted files.
- Kept Cleanup All for soft-deleted file records.
- Removed cleanup-state UI that depended on DB cleanup tracking.
- The page still reads DB records only during loading/filtering.
- Storage provider calls still happen only when Cleanup or Cleanup All is confirmed.
