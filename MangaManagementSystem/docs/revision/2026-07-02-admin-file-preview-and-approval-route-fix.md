# 2026-07-02 - Admin File Preview and Approval Route Fix

## Context
Leader reviewed Admin Dashboard and File Management behavior.

Issues addressed:
- Manage User Approvals should open the dedicated User Approval page, not User Account Management with a pending filter.
- File Management preview/open behavior should use a consistent controlled flow and avoid opening direct storage links.
- DOC/DOCX should be allowed through the Admin File preview/open flow.

## Changes
- Updated Admin Dashboard links:
  - Pending Approvals Manage -> /admin/user-approval
  - Manage User Approvals -> /admin/user-approval
  - Manage User Accounts remains -> /admin/users

- Updated Admin File preview behavior:
  - Image files preview inside the File Management dialog.
  - PDF files open in a new browser tab through the controlled preview flow.
  - DOC/DOCX files are allowed through the same controlled preview/open flow; browser behavior may download/open them because browsers do not render DOCX like PDF.
  - File content is still loaded through Admin API/backend authorization before the browser receives a temporary blob URL.
  - No direct Cloudinary/storage URL is exposed.

- Added DOC/DOCX MIME types to Admin File previewable content types:
  - application/msword
  - application/vnd.openxmlformats-officedocument.wordprocessingml.document

## Files Changed
- src/MangaManagementSystem.Web/Components/Pages/Admin/AdminDashboard.razor
- src/MangaManagementSystem.Web/Components/Pages/Admin/AdminFiles.razor
- src/MangaManagementSystem.Application/Features/Admin/Files/Queries/AdminFileQueryHandlers.cs

## Validation
- Manual runtime check:
  - Image preview opens inside File Management dialog.
  - PDF opens in a new tab and displays correctly.
  - DOCX opens through browser handling/download via the controlled flow.
  - Manage User Approvals opens /admin/user-approval.
  - Manage User Accounts opens /admin/users.

- Build:
  - dotnet build .\MangaManagementSystem.slnx
  - Result: Build succeeded with 26 warning(s).

## Notes
- No SQL changes.
- No PageRegionService changes.
- No Program.cs changes.
- No Cloudinary/storage logic changes.
- ../tem was not committed.
