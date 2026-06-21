\# File Management - Cloudinary Cleanup and Preview UI Update



Date: 2026-06-21  

Scope: Admin File Management / Cloudinary Cleanup / Preview UI



\## Summary



Implemented storage cleanup support for soft-deleted file resources.



The existing Soft Delete flow remains unchanged. Soft Delete only marks the file as deleted in the database and preserves the physical file in Cloudinary.



The new Cleanup flow allows Admin users to permanently remove the physical file from Cloudinary while preserving the database record, metadata, and audit history.



\## Completed Changes



\### Database



Added storage cleanup metadata to `manga.FileResource`:



\- `storage\_cleanup\_status`

\- `storage\_cleaned\_at\_utc`

\- `storage\_cleaned\_by\_user\_id`

\- `storage\_cleanup\_error`



Supported storage cleanup statuses:



\- `AVAILABLE`

\- `CLEANED`

\- `MISSING`

\- `FAILED`



Updated File Management procedures:



\- `manga.usp\_Admin\_FileResource\_Search`

\- `manga.usp\_Admin\_FileResource\_GetById`



Added cleanup persistence procedures:



\- `manga.usp\_FileResource\_UpdateStorageCleanupResult`

\- `manga.usp\_FileResource\_GetStorageCleanupCandidates`



Database responsibility is limited to query, update, and audit persistence.



\### Application Layer



Implemented cleanup business logic in C# Application layer.



Application layer is responsible for:



\- Checking actor Admin permission

\- Checking file existence

\- Checking file is already soft-deleted

\- Preventing cleanup for Active files

\- Preventing repeated cleanup for Cleaned/Missing files

\- Calling Cloudinary delete

\- Mapping Cloudinary result:

&#x20; - Success -> `CLEANED`

&#x20; - Not Found -> `MISSING`

&#x20; - Error -> `FAILED`

\- Calling database persistence flow for update and audit



This follows the leader's requirement that business decisions and orchestration must be handled in C# Application layer, not in the database.



\### API



Added endpoints:



\- `POST /api/admin/files/{fileResourceId}/cleanup`

\- `POST /api/admin/files/cleanup-deleted`



Existing endpoints remain unchanged:



\- File search

\- File detail

\- Soft Delete

\- Preview

\- Download



\### Web UI



Updated Admin File Management UI:



\- Added Storage status column

\- Added Cleanup button for Deleted + Available/Failed files

\- Disabled Cleanup for Active/Cleaned/Missing files

\- Added Cleanup All button

\- Added confirmation modal for Cleanup

\- Added confirmation modal for Cleanup All

\- Added cleanup result toast/summary

\- Improved image preview with custom modal layout

\- Kept PDF preview behavior unchanged

\- Deleted/Cleaned/Missing files still show safe placeholder for preview



\### Audit



Cleanup writes audit event:



\- `FILE\_RESOURCE\_STORAGE\_CLEANED`



Audit details include:



\- File purpose code

\- Original file name

\- Content type

\- Previous storage cleanup status

\- Cleanup result

\- Cleanup reason

\- Cleanup error if any



\## Test Results



\### Build



Command executed:



```powershell

dotnet build .\\MangaManagementSystem.sln

