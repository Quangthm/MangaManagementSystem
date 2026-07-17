# Editorial Board Notification Bell and Logout Fix

## Date

2026-07-14

## Branch and baseline

- Branch: feature/notification-remaining-flows
- Previous commit: 15a5f3b
- Original main baseline: 4fb8361
- No merge or direct change was made on main.

## Problem confirmed

The existing NotificationBell component was already available through UserAvatarMenu.

However, the Editorial Board pages use the separate MangaFlowShell component, so Board Chief and Board Member users could not see notifications from their normal workflow.

The MangaFlow logout button also navigated to the invalid route /signout, which returned HTTP 404.

## Scope completed

Modified only:

- MangaManagementSystem/src/MangaManagementSystem.Web/Components/Layout/MangaFlowShell.razor

Changes:

- Added the existing NotificationBell component to the authorized MangaFlow topbar.
- Reused the existing global-logout-form.
- Logout now submits POST /api/auth/logout.
- The existing antiforgery token is reused.
- Successful logout redirects to /login.
- Removed the obsolete /signout navigation.
- Removed unused IJSRuntime and NavigationManager dependencies.
- Removed the unused Microsoft.JSInterop import.

## Why the shared shell was changed

The following pages all reuse MangaFlowShell:

- /demo/mangaflow/editorial
- /demo/mangaflow/proposals
- /demo/mangaflow/polls
- /demo/mangaflow/decisions

Changing the shared shell exposes the Bell across the Editorial Board workflow without duplicating code in individual pages.

## Runtime validation

### Editorial Board Chief

- Logged in as TestBoardChief1.
- Bell appeared in the Editorial Board topbar.
- Badge showed one unread notification.
- Bell loaded Publication Frequency Change Request.
- Notification content matched the runtime request.
- Mark as read removed the unread badge.
- Notification remained visible with status Read.
- Database read_at_utc was populated.
- Read-status verification returned PASS.

### Editorial Board Member

- Logged in as TestBoardMember1.
- Bell appeared in the Editorial Board topbar.
- Badge showed one unread notification.
- Bell loaded New Board Poll.
- Notification content matched the Board Poll event.
- Mark as read removed the unread badge.
- Notification remained visible with status Read.
- Database read_at_utc was populated.
- Read-status verification returned PASS.

### Logout

- Opened the MangaFlow account menu.
- Clicked Logout.
- The authenticated session was cleared.
- Browser redirected to https://localhost:7182/login.
- The invalid /signout route was no longer used.
- The previous HTTP 404 behavior was resolved.

### Final read-status summary

- TestBoardChief1: PASS.
- TestBoardMember1: PASS.
- Overall result: 2 of 2 PASS.

## Database impact

- No schema change.
- No migration.
- No stored procedure change.
- No new notification table or API was introduced.
- Existing Notification API and NotificationBell component were reused.

## Build validation

- Changed-file scope was verified.
- Full solution build succeeded.
- Build errors: 0.
- Existing unrelated warnings remained outside this change.
