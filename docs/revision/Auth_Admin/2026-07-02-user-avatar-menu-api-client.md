# 2026-07-02 - User Avatar Menu API Client Refactor

## Context
Leader review noted that user profile data in the layout should not be loaded by calling application services directly from the Web UI.

## Scope
Web component refactor only.

## Changes
- Refactored UserAvatarMenu to use IProfileApiClient.
- Removed direct IUserService injection from UserAvatarMenu.
- Removed direct IFileResourceService injection from UserAvatarMenu.
- Removed duplicate profile loading logic from OnInitializedAsync.
- Kept the existing authenticated profile menu behavior and avatar fallback behavior.

## Not changed
- No SQL files changed.
- No stored procedures changed.
- No database/schema/seed changes.
- No Profile API endpoint changes.
- No ProfileApiClient changes.
- No AdminDashboard changes.
- No File Management changes.

## Validation checklist
- Build solution.
- Confirm UserAvatarMenu no longer references IUserService/UserService.
- Confirm UserAvatarMenu no longer references IFileResourceService/FileResourceService.
- Confirm UserAvatarMenu uses IProfileApiClient.
- Login and confirm the sidebar avatar/menu still renders.
