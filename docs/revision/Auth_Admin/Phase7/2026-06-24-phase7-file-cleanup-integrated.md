# Phase 7 - Admin File Cleanup Integration

## Branch

feature/phase7-file-cleanup-integrated

## Scope

Integrated Phase 7 admin file cleanup behavior into the Phase 1-6 admin file management flow.

## Implemented

- Removed reliance on manga.FileResource.storage_cleanup_status.
- Kept the existing Phase 6 Admin File Management UI flow and filters.
- Kept Razor UI calling the API client instead of calling storage/file services directly.
- Renamed the UI action from Soft Delete to Delete.
- Delete now marks the database file record as deleted and triggers Cloudinary cleanup through the backend.
- Added cleanup support for already deleted file records.
- Added per-file Cleanup action for deleted records.
- Added Cleanup All action in the Deleted view.
- Removed the separate Preview action button from the row actions.
- File preview is now opened by clicking the file name.
- Kept Download disabled for deleted files.
- Did not modify SQL scripts, schema, migrations, DbContext, or stored procedures.

## Validation

- dotnet build .\MangaManagementSystem.slnx succeeded.
- git diff --check passed without whitespace errors.
- Verified no usage of storage_cleanup_status, StorageCleanupStatus, Storage State, or Pending cleanup in the changed diff.
- Verified AdminFiles.razor does not call file resource/storage services directly.
- Verified cleanup is guarded for deleted files only.

## Main branch note

origin/main was fetched and checked, but it is not included in this branch. A direct merge was not performed because the diff from HEAD..origin/main showed large unrelated changes and deletion of Admin File Management related files. This branch remains based on the Phase 1-6 checkpoint to preserve the required admin file management flow.

## Database note

No database/schema/stored procedure changes were made. If database cleanup-state tracking is required later, it should be reviewed and approved by the leader first.
