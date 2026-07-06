# 2026-06-27 - Admin File PDF Preview and Local Internal API Config

## Summary

Updated the local development configuration and Admin File Management preview behavior based on leader feedback.

## Changes

### Internal API local development config

- Added Development-only `InternalApi` configuration for both API and Web projects.
- This removes the need to manually run `dotnet user-secrets set InternalApi:Key ...` just to start the project locally.
- Existing user-secrets flow is not removed for real secrets such as SMTP, Google, Cloudinary, or Recaptcha.

Changed files:

- `src/MangaManagementSystem.API/appsettings.Development.json`
- `src/MangaManagementSystem.Web/appsettings.Development.json`

### Admin File Management PDF preview

- Updated Admin File Management so PDF files are no longer rendered inside the small preview iframe.
- When an admin selects a PDF file name in File Management, the PDF now opens in a new browser tab.
- Existing image preview behavior is preserved. Image files still open in the existing preview modal.
- Refactored PDF content-type detection through a reusable helper.
- Added reusable JavaScript helper for opening proxied file content as a browser tab.

Changed files:

- `src/MangaManagementSystem.Web/Components/Pages/Admin/AdminFiles.razor`
- `src/MangaManagementSystem.Web/wwwroot/js/admin-file-content.js`

## Scope control

Not changed:

- Profile Settings
- User Approval
- Database schema
- Stored procedures
- File cleanup business logic
- Main branch merge/conflict resolution

## Validation

- `dotnet build .\MangaManagementSystem.slnx` passed.
- API started successfully in Development environment.
- Web started successfully in Development environment.
- Admin File Management image preview was tested and still opens in the modal.
- Admin File Management PDF preview was tested and opens in a new browser tab.