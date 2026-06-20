# Phase 5 Audit Log Separation and Filtering

**Date:** 2026-06-20

**Branch:** `feature/phase5-admin-account-management`

## Objective

Implement the leader review request to move the system-wide Audit Event view to a dedicated Admin page while preserving the user-specific Audit History on each User Detail page.

## Changes

- Replaced the `/admin/audit-logs` redirect with a standalone Audit Logs page.
- Added a dedicated Admin Audit API endpoint at `api/admin/audit-events`.
- Added server-side filtering for:
  - search text,
  - action code,
  - entity type,
  - from date,
  - to date.
- Added server-side pagination.
- Added the missing `Entity Type` table column.
- Added distinct Action Code and Entity Type filter options.
- Added actor display information to the system-wide Audit view.
- Added a separate Audit Logs navigation item and Admin Dashboard card.
- Preserved the existing user-specific Audit History on User Detail.
- Kept Audit writes database-owned; no direct C# Audit append was added.
- No database schema or stored procedure change was required because `EntityType` already exists in the Audit Event model and database data.

## Files

### Added

- `Application/DTOs/Admin/AdminAuditDtos.cs`
- `Application/Features/Admin/Audit/Queries/AdminAuditQueries.cs`
- `Application/Features/Admin/Audit/Queries/AdminAuditQueryHandlers.cs`
- `API/Controllers/AdminAuditEventsController.cs`
- `Web/Services/Api/IAdminAuditApiClient.cs`
- `Web/Services/Api/AdminAuditApiClient.cs`

### Updated

- `Domain/Interfaces/IAuditEventRepository.cs`
- `Infrastructure/Repositories/AuditEventRepository.cs`
- `Web/Components/Pages/Admin/AuditLogs.razor`
- `Web/Components/Layouts/AdminLayout.razor`
- `Web/Components/Pages/Admin/AdminDashboard.razor`
- `Web/Program.cs`

## Verification checklist

- [ ] `dotnet restore` succeeds.
- [ ] `dotnet build --configuration Debug` succeeds with zero errors.
- [ ] Admin can open `/admin/audit-logs`.
- [ ] Entity Type column is visible.
- [ ] Search filter works.
- [ ] Action filter works.
- [ ] Entity Type filter works.
- [ ] Date filters work.
- [ ] Clear resets all filters.
- [ ] Pagination works.
- [ ] User Detail still shows user-specific Audit History.
- [ ] Non-Admin access to `/admin/audit-logs` is denied.
- [ ] No direct C# Audit write was introduced.

## Manual verification results

Manual verification was completed locally on 2026-06-20 using the Admin and Mangaka test accounts.

- PASS: Admin Dashboard exposes Audit Logs as a separate navigation item and dashboard card.
- PASS: /admin/audit-logs opens as a standalone Audit Logs page and no longer redirects to User Management.
- PASS: the Audit table includes Time, Action, Entity Type, Entity, Actor, and Details.
- PASS: Entity Type filtering returned only matching USER records (113 of 169 local events).
- PASS: Action filtering returned only User Status Changed records (21 local events).
- PASS: Clear restored the complete local Audit result set (169 events).
- PASS: search for 	estreject returned the single matching Audit event.
- PASS: date filtering from 2026-06-19 through 2026-06-20 returned 19 matching events.
- PASS: page size 10 displayed ten rows and server-side pagination reported 17 pages.
- PASS: User Audit History remains available inside the selected user's User Detail page.
- PASS: a signed-in Mangaka opening /admin/audit-logs was redirected to Access Denied.
- PASS: Action and Entity Type labels display above their dropdowns without overlapping selected values.

Local historical Audit data contains legacy Entity Type variations such as Users and USER. The implementation preserves the stored Audit records and filters by the selected persisted value; no historical Audit data was rewritten.
