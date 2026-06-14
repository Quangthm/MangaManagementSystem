# API Registration Refactor + Page-Region Workflow Fix

## Date
2026-06-14

## Branch
`feature/Mangaka`

## Task Goal
1. Resolve lingering page-region task/annotation backend service issues so they are stored-procedure-first, cohesive, and correct.
2. Begin the `.API` refactor for the **registration workflow only**, keeping Application reusable and Infrastructure SP details hidden, with thin controllers.
3. Build/verify and produce a handoff note.

## Architecture Decision Update
The earlier decision to *not* recreate `MangaManagementSystem.API` is reversed. The project again uses a dedicated API boundary:

- `MangaManagementSystem.Web` = Blazor Server / MudBlazor frontend host.
- `MangaManagementSystem.API` = ASP.NET Core Web API backend boundary (thin controllers).
- `MangaManagementSystem.Application` = use cases, DTOs, service interfaces, validation/normalization, mappers.
- `MangaManagementSystem.Infrastructure` = EF Core, SQL Server stored-procedure wrappers, Cloudinary, OTP cache, external adapters.
- `MangaManagementSystem.Domain` = entities and domain concepts.

Web code was **not** mass-migrated. Only the registration workflow was given an API boundary. The Web host still owns its existing registration endpoints; the new API endpoints are additive and reuse the same Application services.

### Duplicate / stale project finding
There are **two** `MangaManagementSystem.API.csproj` files:
- `MangaManagementSystem\MangaManagementSystem\src\MangaManagementSystem.API\...` — **referenced by `MangaManagementSystem.slnx`. This is the real API project and the one edited.**
- `MangaManagementSystem\MangaManagementSystem\MangaManagementSystem.API\...` (top-level) — **stale, NOT referenced by the solution.** Left untouched (not deleted) to avoid risk. Recommend removing it in a dedicated cleanup task after confirming nothing depends on it.

## Documents / Skills Files Read
- `docs/business-flows-use-cases.md` (BF-AUTH-001/002/003 registration flows; BF-PAGE-003/004/005/006 region/annotation/task flows)
- `docs/business-rules.md` (BR-USER-*, BR-ANN-*, BR-PGTASK-*, BR-REG-*)
- `docs/context.md` (MVP scope, actor model, modeling decisions)
- Prior handoff: `docs/revision/Mangaka/architecture-cleanup-handoff-2026-06-11-role-region-refactor.md`
- SQL source of truth: `MangaManagementSystem_Procedures_Views_Bootstrap.sql` (verified SP contracts)

## Page-Region Workflow Fixes Completed
The create/resolve paths already routed through stored procedures. Remaining gaps fixed:

1. **Correct stored-procedure actor for task creation.**
   - `usp_ChapterPageTask_Create` treats `@actor_user_id` (creator) and `@assigned_to_user_id` as *separate* contributors and records the actor as `created_by_user_id` + audit actor.
   - Previously the service passed `dto.AssignedToUserId` as the actor (a permission/audit bug).
   - Added `ActorUserId` to `CreateChapterPageTaskDto`; service now passes it as the SP actor. Web caller (`CreatorWorkspace.razor`) now passes `_currentUserId`.

2. **Read DTOs return populated `PageRegions`.**
   - `ChapterPageTaskService.GetChapterPageTaskByIdAsync` and `ChapterPageAnnotationService.GetChapterPageAnnotationByIdAsync` previously used the generic `FindAsync` path (empty navigation collections). Both now use the Include-based `GetByIdWithRegionsAsync` repository methods.

3. **Removed in-memory filtering.**
   - `GetChapterPageTasksByAssignedUserIdAsync` previously called `GetAllAsync()` then filtered with LINQ in memory. It now uses the SQL-level `GetByAssignedUserIdWithRegionsAsync` repository method.

4. **Update workflows load tracked regions.**
   - `UpdateChapterPageTaskAsync` and `UpdateChapterPageAnnotationAsync` previously loaded via the generic `GetByIdAsync` (no regions tracked), then called `PageRegions.Clear()` — EF could not reconcile junction rows it never loaded. Both now load via `GetByIdWithRegionsAsync` so region reconciliation works.

> Note: task/annotation **create and resolve** remain stored-procedure-first (permissions, same-page-version validation, transactions, audit all in SQL). The EF-based `Update`/`Delete` methods are retained for non-workflow field edits (status/title/reassign in the workspace UI); these are candidates for dedicated SPs in a later task (see remaining risks).

## Registration / API Refactor Work Completed
- **Shared OTP cache moved to Infrastructure.** `OtpCacheService` (an `IMemoryCache`-backed adapter implementing the Application interface `IOtpCacheService`) was moved from `MangaManagementSystem.Web/Services` to `MangaManagementSystem.Infrastructure/Services`, so both Web and API hosts reuse one implementation without the API referencing Web.
  - Registered in `AddInfrastructure`: `services.AddMemoryCache()` + `services.AddSingleton<IOtpCacheService, OtpCacheService>()`.
  - Removed the duplicate Web registration and the Web copy of the class. Web `Program.cs` keeps a single `AddMemoryCache()`.
- **API project wired** (`src/MangaManagementSystem.API`):
  - Added project references to `Application` and `Infrastructure`.
  - `Program.cs` now calls `AddApplicationServices()` + `AddInfrastructure(builder.Configuration)`, adds controllers + Swagger. No business logic or SQL in the host.
  - Removed scaffold `WeatherForecast.cs` and `WeatherForecastController.cs`.
- **Thin `RegistrationController`** (`api/registration`):
  - `POST /api/registration/otp` -> validates request shape, maps to `RegisterDto`, calls `IAuthService.SendRegistrationOtpAsync`.
  - `POST /api/registration/complete` -> calls `IAuthService.CompleteRegistrationWithOtpAsync`, returns the created `UserDto`.
  - Catches `InvalidOperationException` (the friendly, user-safe messages the Application already throws) and returns `409 Conflict` / `400 BadRequest`; all other exceptions return a generic `Problem` 500. **No raw SQL/exception text is exposed.**
- **API request contracts** added under `Contracts/` (`SendRegistrationOtpRequest`, `CompleteRegistrationRequest`, `ApiErrorResponse`, `ApiMessageResponse`).

Cloudinary upload + SQL compensation/cleanup for the optional portfolio stays inside `AuthService.CompleteRegistrationWithOtpAsync` (backend workflow), not in the controller. The JSON API contract currently omits portfolio upload (Web still handles multipart portfolio today); a multipart API endpoint is a follow-up.

## Files Changed
**Page-region workflow**
- `src/MangaManagementSystem.Application/DTOs/Manga/ChapterPageTaskDtos.cs` (added `ActorUserId`)
- `src/MangaManagementSystem.Application/Services/ChapterPageTaskService.cs`
- `src/MangaManagementSystem.Application/Services/ChapterPageAnnotationService.cs`
- `src/MangaManagementSystem.Web/Components/Pages/Mangaka/CreatorWorkspace.razor` (pass `ActorUserId: _currentUserId`)

**Registration / API**
- `src/MangaManagementSystem.Infrastructure/Services/OtpCacheService.cs` (new — moved from Web)
- `src/MangaManagementSystem.Infrastructure/DependencyInjection.cs` (register memory cache + OTP cache)
- `src/MangaManagementSystem.Web/Services/OtpCacheService.cs` (deleted)
- `src/MangaManagementSystem.Web/Program.cs` (removed duplicate OTP DI registration)
- `src/MangaManagementSystem.API/MangaManagementSystem.API.csproj` (project references)
- `src/MangaManagementSystem.API/Program.cs` (DI wiring, removed scaffold)
- `src/MangaManagementSystem.API/Controllers/RegistrationController.cs` (new)
- `src/MangaManagementSystem.API/Contracts/RegistrationRequests.cs` (new)
- `src/MangaManagementSystem.API/Contracts/ApiResponses.cs` (new)
- `src/MangaManagementSystem.API/WeatherForecast.cs` (deleted)
- `src/MangaManagementSystem.API/Controllers/WeatherForecastController.cs` (deleted)

## Stored Procedures Used or Planned
**Used (unchanged, verified against schema):**
- `manga.usp_ChapterPageTask_Create` (`@actor_user_id`, `@assigned_to_user_id`, `@page_region_ids_json`, …)
- `manga.usp_ChapterPageAnnotation_Create` (`@actor_user_id`, `@page_region_ids_json`, `@issue_type_code`, `@annotation_text`)
- `manga.usp_ChapterPageAnnotation_Resolve` (`@actor_user_id`, `@chapter_page_annotation_id`, `@resolution_note`)
- `auth.usp_User_Create`, `auth.usp_User_CreateWithOptionalPortfolio` (registration path)

**Planned (future tasks, not implemented this session):**
- A workflow SP for task status transitions (cancel/complete/reassign) to replace EF `Update` on `ChapterPageTask` (cancel SP `manga.usp_ChapterPageTask_Cancel` already exists in the script and should be wired).
- Optional SP for annotation header edits if those become workflow-significant.

## Validation / Normalization Decisions
- Registration normalization (email lower/trim, username trim) and the duplicate-email/duplicate-username checks remain centralized in `AuthService` via `NormalizeEmail` and `EnsureEmailAndUsernameAvailableAsync`. **Email is the primary account-facing lookup** (`GetByEmailAsync`, which includes `Role`); username uniqueness is still enforced and checked deliberately.
- `UserId`/GUID identity is preserved everywhere (claims, audit, FKs, SP actor/target, admin actions). No GUID removed.
- No business-facing `RoleId`; `RoleName` is used throughout (default registration role `Mangaka`, allowed-role set in `UserService`).
- API controller does request-shape validation (DataAnnotations + `ModelState`); business validation stays in Application.

## UX Notes Captured (NOT yet applied — frontend untouched)
- Duplicate email -> friendly "An account with this email already exists." (API returns 409; UI should surface cleanly).
- Duplicate username -> friendly "This username is already taken." (API 409).
- Invalid/expired OTP -> clean retry/resend message (API 400; current message "The verification code is invalid or has expired.").
- Pending-approval login -> clear account-status message (already mapped to `/login?error=account_pending`).
- Rejected/disabled login -> clear access message (`account_rejected` / `account_disabled`).
- File-upload validation -> user-friendly messages (portfolio type/size).
- Raw SQL exception text must never reach UI/API consumers — API already enforces this via the generic 500 `Problem`.

## Build / Verification Result
- `dotnet build MangaManagementSystem.slnx` => **Build succeeded, 0 Errors, 0 Warnings** (final incremental build).
- Verified: solution builds; page task/annotation services compile; registration API compiles; API references Application + Infrastructure; DI for API resolves Application + Infrastructure (incl. shared OTP cache); no dangling references to the removed Web OTP class; no secrets/appsettings credentials changed; stale top-level API project left untouched.

## Remaining Risks
- **API host configuration:** `src/MangaManagementSystem.API/appsettings.json` has **no** `ConnectionStrings:DefaultConnection`, Cloudinary, or SMTP config. `AddInfrastructure` builds the DI graph but the API will fail at runtime for DB/Cloudinary/email until config is supplied via user-secrets/environment variables. Intentionally not added (no credentials in repo).
- **Portfolio upload not in API JSON contract** yet; Web still owns multipart portfolio registration. Needs a multipart API endpoint for full parity.
- **Web not yet calling the API** for registration; the API endpoints are additive. Switching Web to call the API is a deliberate later step.
- **Task EF Update/Delete** still bypass SP workflow for status transitions; wire `manga.usp_ChapterPageTask_Cancel` and a status-transition SP next.
- Stale top-level `MangaManagementSystem.API` project still on disk (unreferenced).

## Next Recommended Prompt
> Wire the Web registration UI to call the new `MangaManagementSystem.API` `api/registration/otp` and `api/registration/complete` endpoints via a typed `HttpClient`, keeping the existing Blazor flow as fallback. Add API host configuration (connection string, Cloudinary, SMTP) via user-secrets only — do not commit secrets. Then add a multipart portfolio-upload registration endpoint to reach parity with the Web flow. Separately, replace `ChapterPageTaskService` EF status transitions (cancel/complete/reassign) with stored-procedure workflows, starting by wiring the existing `manga.usp_ChapterPageTask_Cancel`. Finally, remove the stale top-level `MangaManagementSystem.API` project after confirming it is unreferenced.
