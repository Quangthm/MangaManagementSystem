# Project Hand-off & Session Context: Cursor to VS Copilot
**Date:** 2026-05-31
**Project:** Manga Management System
**Tech Stack:** C#, .NET 8 (Blazor Interactive Server), EF Core, MudBlazor

## 1. Current State (What is working)
- **Database & Data:** SQL Server is connected. The `auth.Users` table has been seeded manually with `admin@test.com`, `mangaka@test.com`, and `editor@test.com` (BCrypt passwords, `ACTIVE` status).
- **OTP Registration:** Real SMTP Email sending is fully functional. Users receive 6-digit OTPs to their real Gmail accounts.
- **Auth Logic:** The backend `AuthService` successfully verifies credentials, handles BCrypt hashes, and logs detailed outcomes (`ILogger`).

## 2. The Problem (Blazor Architecture Issues)
We are hitting issues caused by the difference between Blazor SignalR circuits and standard HTTP requests:
1. **Cookie Issue:** Backend logs "Login success", but the UI shows "Unable to sign in". `HttpContext.SignInAsync` cannot write cookies over a Blazor Interactive Server SignalR connection.
2. **Missing Redirects:** Successful logins do not redirect users to their role-specific workspaces.
3. **Google OAuth Fails:** Clicking the Google login button throws an error because triggering an OAuth Challenge requires a full HTTP POST, not a Blazor component click event.

## 3. Immediate Execution Plan for Copilot
Please execute the following tasks to fix the Blazor UI desync and complete the Auth Epic.

### Task 1: Fix Cookie Authentication (Blazor 8 SSR/Form Post)
- Refactor `LoginPage.razor` to use a full HTTP POST. Use `<EditForm Model="LoginModel" OnValidSubmit="HandleLogin" FormName="LoginForm" method="post">` and mark the model with `[SupplyParameterFromForm]`.
- Ensure `HttpContext` can perform `SignInAsync` correctly during the static server rendering phase (or use a Minimal API endpoint to handle the cookie setting).

### Task 2: Implement Role-Based Redirects
- Update the post-login logic to route users based on their role:
  - Role 5 (Admin) -> `/admin/user-approval`
  - Role 1 (Mangaka) -> `/workspace/mangaka`
  - Role 2 (Assistant) -> `/workspace/assistant`
  - Role 3 (Tantou Editor) -> `/workspace/editor`
  - Role 4 (Editorial Board) -> `/workspace/board`
- Fallback to `/` if the role is unknown.

### Task 3: Fix Google OAuth Challenge
- In `Program.cs`, map a POST endpoint for Google Login:
  `app.MapPost("/api/auth/google-login", () => Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [GoogleDefaults.AuthenticationScheme]));`
- In `LoginPage.razor`, update the "Login with Google" button to be a standard HTML `<form action="/api/auth/google-login" method="post">` (including `<AntiforgeryToken />`) so it forces a standard browser redirect.