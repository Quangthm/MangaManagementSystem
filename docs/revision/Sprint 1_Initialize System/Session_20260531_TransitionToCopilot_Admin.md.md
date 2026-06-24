# Project Hand-off & Session Context: Cursor to VS Copilot
**Date:** 2026-05-31
**Project:** Manga Management System
**Tech Stack:** C#, .NET 8, EF Core, MudBlazor, Clean Architecture

## 1. Current State (What was completed)
- **Database & Architecture:** SQL Server connected. Clean Architecture fully established (`IUnitOfWork`, 21 Application Services, `record` DTOs).
- **Authentication Epic (100% Complete):** - Implemented Email OTP Registration (2-step flow) to prevent spam. Users are saved directly to `auth.Users` with `status_code = "PENDING_APPROVAL"`.
  - Implemented Blazor 8 Interactive Server Cookie Authentication via `CustomAuthenticationStateProvider`.
  - Login properly rejects `PENDING_APPROVAL` and `DISABLED` users.

## 2. Strict Business Rules (Must Enforce)
- **BR-USER-001:** Strict 5 RBAC roles (1 = Mangaka, 2 = Assistant, 3 = Tantou Editor, 4 = Editorial Board Member, 5 = Admin).
- **BR-USER-010:** Do NOT create a separate `RegistrationRequest` table. We use the `status_code` ("PENDING_APPROVAL", "ACTIVE", "REJECTED") directly on the `User` entity.

## 3. Immediate Execution Plan for Copilot (Admin Epic)
The system currently has users stuck in `PENDING_APPROVAL`. We need to build the Admin User-Approval screen to unblock them (User Story: `US-ADMIN-001`). 
Please execute the following tasks sequentially.

### Task 1: Extend `UserService` for Admin Actions
Update `IUserService.cs` and `UserService.cs` with:
- `Task<IEnumerable<UserDto>> GetUsersByStatusAsync(string status);`
- `Task<UserDto> ApproveUserAsync(int userId, short assignedRoleId);` (Sets status to `ACTIVE`, updates `role_id`, calls `SaveChangesAsync`).
- `Task RejectUserAsync(int userId);` (Sets status to `REJECTED` or soft deletes).

### Task 2: Create Admin Approval Page (`Pages/Admin/UserApproval.razor`)
- Add `@attribute [Authorize(Roles = "Admin")]`.
- Build a `MudTable` fetching "PENDING_APPROVAL" users.
- Add an action column with:
  1. A `MudSelect<short>` to pick the final Role (Defaults to user's current role).
  2. "Approve" button (Calls `ApproveUserAsync`).
  3. "Reject" button (Calls `RejectUserAsync`).
- Use `ISnackbar` for feedback and refresh the table state.

### Task 3: Secure the Navigation Menu
- Locate the main Sidebar/NavMenu component.
- Wrap the link to the Admin Approval page inside an `<AuthorizeView Roles="Admin">` tag so only Admins can see it.