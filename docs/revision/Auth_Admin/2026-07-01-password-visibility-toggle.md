# 2026-07-01 - Password Visibility Toggle

## Context
Added show/hide password eye toggles to password input fields in Web UI.

## Scope
Updated UI only. No database, stored procedure, schema, migration, or API logic changes.

## Updated files
- `src/MangaManagementSystem.Web/Components/Pages/LoginPage.razor`
- `src/MangaManagementSystem.Web/Components/Pages/RegisterPage.razor`
- `src/MangaManagementSystem.Web/Components/Pages/ResetPasswordPage.razor`
- `src/MangaManagementSystem.Web/Components/Pages/ProfileSettings.razor`

## Behavior
- Login password field can toggle between hidden and visible.
- Register password field can toggle between hidden and visible.
- Reset Password page has independent toggles for New Password and Confirm New Password.
- Profile Settings security tab has independent toggles for New Password and Confirm New Password.

## Validation checklist
- Build solution.
- Open Login page and toggle password visibility.
- Open Register page and toggle password visibility.
- Open Reset Password page with a token link and toggle both password fields.
- Open Profile Settings > Security and toggle both password fields.
