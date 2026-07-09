# 2026-07-10 - Admin Search Enter Pages

## Branch
fix/admin-search-enter-pages

## Scope
Fix Enter key behavior for admin search/filter inputs on three admin pages.

## Problem
Manual testing showed that typing a keyword and pressing Enter did not trigger search/filter reload on:
- User Account Management
- Audit Logs
- File Management

The existing Search/Apply buttons already worked, so the issue was in the Web UI key handling, not in the backend API filtering logic.

## Files changed
- MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Admin/UserAccounts.razor
- MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Admin/AuditLogs.razor
- MangaManagementSystem/src/MangaManagementSystem.Web/Components/Pages/Admin/AdminFiles.razor

## Implementation
Added KeyboardEventArgs support and OnKeyDown handling to the search text fields.

Each page now has a HandleSearchKeyDownAsync handler. When the pressed key is Enter and the page is not loading, the handler calls ApplyFiltersAsync(), reusing the same logic as the existing Search/Apply button.

No backend API, database schema, stored procedure, or filter query logic was changed.

## Manual verification
Verified Enter search behavior on:
- /admin/users
  - Typed "Phan" and pressed Enter.
  - User list reloaded and showed matching accounts.

- /admin/audit-logs
  - Typed "Phan" and pressed Enter.
  - Audit events list reloaded and showed matching events.

- /admin/files
  - Typed "Phan" and pressed Enter.
  - File list reloaded according to the filter.

## Build verification
Run:
dotnet build C:\jira\MangaManagementSystem\MangaManagementSystem.slnx

Result:
Build succeeded.

## Notes
The fix intentionally reuses ApplyFiltersAsync() instead of creating duplicate search logic.
Temporary local files such as leader_followup_*.txt and tem are not part of this task and must not be committed.
