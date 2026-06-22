# Project Hand-off & Session Context: Cursor to VS Copilot
**Date:** 2026-05-31
**Project:** Manga Management System
**Tech Stack:** C#, .NET 8, EF Core, MudBlazor, Clean Architecture
**Developer Context:** Transitioning from Java EE (MVC2) to .NET 8. Needs strict adherence to Clean Architecture, Entity Framework best practices, and avoiding legacy Java-style manual session management.

## 1. Current State
- **Database & Architecture:** SQL Server connected successfully. Clean Architecture is fully set up with `IUnitOfWork` and 21 Application Services using `record` DTOs.
- **UI:** Figma-to-MudBlazor static UI is integrated and compiles successfully. 
- **Current Epic:** Authentication & Authorization.

## 2. Strict Business Rules (Must Enforce)
- **BR-USER-001:** 5 Strict RBAC roles.
- **BR-USER-002 & 003 (UPDATED FOR ANTI-SPAM):** Registration must use a Two-Step OTP Email verification. Users are ONLY saved to the database (with `PENDING_APPROVAL` status) AFTER the 6-digit OTP is verified.
- **BR-USER-006:** Login attempts by `PENDING_APPROVAL` or `DISABLED` users must be rejected with explicit messages.

## 3. Immediate Execution Plan for Copilot
Please execute the following tasks sequentially. Provide code for one task at a time and wait for confirmation.

### Task 1: Infrastructure (Security, Email, Cache)
- Install `BCrypt.Net-Next`, `MailKit`, `MimeKit`.
- Implement `IPasswordHasher` and `BcryptPasswordHasher`.
- Implement `IEmailService` and `EmailService` (use `Smtp:UseMock: true` to just log OTPs to the console for dev testing).
- Ensure `IMemoryCache` is registered in `Program.cs` to hold OTPs for 5 minutes.

### Task 2: Application Layer (Auth Service)
- Create `IAuthService` and `AuthService`.
- **SendRegistrationOtpAsync:** Generate 6-digit OTP -> Save to `IMemoryCache` (key: email, TTL: 5m) -> Send via `IEmailService`.
- **CompleteRegistrationWithOtpAsync:** Validate OTP -> Hash Password -> Save User via `_unitOfWork.Users.AddAsync()` with `PENDING_APPROVAL`.
- **LoginAsync:** Validate credentials -> Check `status_code` -> Return `AuthResultDto` with role info.

### Task 3: Blazor Web (Auth State & UI Wiring)
- Implement `CustomAuthenticationStateProvider` using `Microsoft.AspNetCore.Components.Authorization`.
- Wire up `RegisterPage.razor` to handle the 2-step OTP flow using `SendRegistrationOtpAsync` and `CompleteRegistrationWithOtpAsync`.
- Wire up `LoginPage.razor` to handle `LoginAsync` and update the auth state.