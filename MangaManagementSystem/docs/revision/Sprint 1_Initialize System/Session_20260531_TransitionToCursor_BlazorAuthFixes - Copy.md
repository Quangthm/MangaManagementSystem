# Project Hand-off & Session Context: Copilot to Cursor
**Date:** 2026-05-31
**Project:** Manga Management System
**Tech Stack:** C#, .NET 8 (Blazor Interactive Server), EF Core, MudBlazor

## 1. Current State (What is working perfectly)
- **Database & Services:** SQL Server is connected. `AuthService.LoginAsync` correctly verifies BCrypt passwords and logs "Login success" to the CMD.
- **OTP Registration:** Real SMTP Email sending (MailKit) is fully functional.
- **Minimal APIs:** We have setup `app.MapPost("/api/auth/login", ...)` in `Program.cs` to handle cookie authentication and role-based redirects.

## 2. The Core Problems (Blazor 8 Architecture Limitations)
We are currently blocked by Blazor's enhanced navigation (SignalR/Fetch) swallowing standard HTTP responses.

- **Issue 1 (Email Login):** CMD shows "Login success", but the web UI does not redirect the user to their workspace (`.razor` pages). Reason: Blazor is intercepting the form submission. It swallows the `302 Redirect` and fails to set the auth cookie because SignalR cannot modify HTTP headers.
- **Issue 2 (Google Login):** Clicking "Sign in with Google" throws an error or does nothing. Reason: Initiating a Google OAuth Challenge requires a full HTTP POST request, which Blazor's `@onclick` or `<EditForm>` prevents.

## 3. Strict Execution Plan for Cursor
Please read the context above and execute the following fixes meticulously. You must bypass Blazor entirely for these auth triggers.

### Task 1: Fix `LoginPage.razor` (Force Standard HTTP Posts)
Refactor the UI to use raw HTML forms with `data-enhance="false"`.
1. **Email Form:**
   - Remove `<EditForm>` and any `@onsubmit` handlers.
   - Use: `<form action="/api/auth/login" method="post" data-enhance="false">`
   - Include `<AntiforgeryToken />` and standard `<input name="username">`, `<input name="password">`.
2. **Google Button:**
   - Wrap the button in a form: `<form action="/api/auth/google-login" method="post" data-enhance="false">`
   - Include `<AntiforgeryToken />` and a `type="submit"` button.

### Task 2: Implement Role-Based Redirects in `Program.cs`
Update the `app.MapPost("/api/auth/login", ...)` endpoint to ensure it returns actual redirects after setting the cookie (`HttpContext.SignInAsync`).
- Evaluate `AuthResultDto.User.RoleId`:
  - `RoleId == 1` -> `return Results.Redirect("/workspace/mangaka");`
  - `RoleId == 2` -> `return Results.Redirect("/workspace/assistant");`
  - `RoleId == 3` -> `return Results.Redirect("/workspace/editor");`
  - `RoleId == 4` -> `return Results.Redirect("/workspace/board");`
  - `RoleId == 5` -> `return Results.Redirect("/admin/user-approval");`
- Fallback: `return Results.Redirect("/login?error=InvalidCredentials");`

### Task 3: Fix Google OAuth Endpoint in `Program.cs`
Ensure the `/api/auth/google-login` endpoint correctly triggers the challenge:
```csharp
app.MapPost("/api/auth/google-login", () => 
{
    var properties = new AuthenticationProperties { RedirectUri = "/" };
    return Results.Challenge(properties, [Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme]);
});
```
### Task 4: System-Wide Auth Review (Fix Potential Traps)
Antiforgery: Since we are using standard HTML forms with `<AntiforgeryToken />`, ensure builder.Services.AddAntiforgery(); and app.UseAntiforgery(); are correctly placed in Program.cs.

Namespaces: Add any missing using statements for Results.Redirect, AuthenticationProperties, or GoogleDefaults.

Form Binding: Ensure the Minimal API endpoint in Program.cs correctly uses `[FromForm]` so it doesn't fail to read the username/password.

Please output the complete, modified code for LoginPage.razor and Program.cs and ensure the project can build successfully without errors.