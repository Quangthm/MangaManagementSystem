# 2026-07-02 - Admin UI Cleanup and Login Password Eye Fix

## Context
Addressed UI issues reported during leader review.

## Scope
UI-only changes.

## Changes
- LoginPage: added CSS guard to hide native browser password reveal controls so only the custom password visibility icon is shown.
- AdminDashboard: removed the System Settings card because the feature is not currently available.
- NavMenu: removed the System Settings navigation link to avoid exposing an unavailable placeholder page.

## Not changed
- No database changes.
- No stored procedure changes.
- No API/auth/reCAPTCHA logic changes.
- No File Management stored procedure fix in this commit.
- No UserAvatarMenu architecture refactor in this commit.

## Validation checklist
- Build solution.
- Open Login page and confirm only one password eye icon is visible.
- Open Admin Dashboard and confirm System Settings card is not shown.
- Confirm System Settings link is not shown in the admin sidebar.
