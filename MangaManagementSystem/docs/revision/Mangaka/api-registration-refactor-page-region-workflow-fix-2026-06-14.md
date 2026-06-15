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

## Session 2a: setup-secrets.ps1 — Restore Hard-Coded Secrets, Add API Support (2026-06-14)

### Task
The previous session's manual-input/interactive approach was reverted per team request. The team wants the committed convenience script to apply the existing hard-coded development secrets to both Web and API during this transition.

### What was recovered from git history
All 10 original secret values from `c732356` (`git show c732356:MangaManagementSystem/setup-secrets.ps1`):
- SMTP username/password/from email
- Google OAuth Client ID / Client Secret
- Cloudinary cloud name, API key, API secret
- reCAPTCHA site key, secret key

### What was added
- `ConnectionStrings:DefaultConnection` (was missing; required by API project's `AddInfrastructure` at runtime). Value taken from existing `src/MangaManagementSystem.Web/appsettings.json`.
- `-Project` parameter (`Web` / `API` / `Both`), defaults to **`Both`** for the current transition state.
- Reusable `Install-SecretsForProject` helper function that receives the shared `$secrets` hashtable and a target project path — no duplication between Web and API.
- Path resolution via `$PSScriptRoot` (works from any CWD).
- Placeholder validation: if any value matches `PUT_*` or `*_HERE*`, the script refuses to run unless `-AllowPlaceholders` is passed. This guards against accidentally applying template values.

### What was removed
- All interactive `Read-Host` logic, `-AsSecureString` prompts, per-secret parameters, and the `$secretDefs` metadata table from Session 2.

### Projects supported
- `src/MangaManagementSystem.Web/MangaManagementSystem.Web.csproj`
- `src/MangaManagementSystem.API/MangaManagementSystem.API.csproj`
- **Not** the stale top-level `MangaManagementSystem.API/` (not in solution).

### Exact command to run
```powershell
.\setup-secrets.ps1 -Project Both
```
No manual input required. The script applies all 11 secrets (including `ConnectionStrings:DefaultConnection`) to both projects' user-secrets stores silently.

### Verification
- PowerShell syntax: **Syntax OK** (via `[System.Management.Automation.Language.Parser]::ParseFile`).
- Git-recovered values are present; no placeholders remain in the committed file.
- `dotnet user-secrets --help` confirmed available (v10.0.9).
- Both `.csproj` files exist at resolved paths.
- Full solution build: **0 errors, 35 warnings** (pre-existing, unchanged).
- Script only prints safe messages: project name, key names, "done". Never prints values.

## Session 3: Web Registration UI → Typed API Client (2026-06-14)

### Task
Refactor the Web registration page to call the API through a typed `HttpClient` instead of calling `IAuthService` directly, while preserving portfolio upload via a temporary fallback.

### API smoke-test result
- Ran `src/MangaManagementSystem.API` project via `dotnet run --launch-profile http` on `http://localhost:5234`.
- Swagger confirmed at `/swagger/v1/swagger.json` with both paths:
  - `POST /api/registration/otp`
  - `POST /api/registration/complete`
- End-to-end test: `POST /api/registration/otp` with a valid body → **HTTP 200** `{"message":"A verification code has been sent to your email."}` — full pipeline (controller → Application → Infrastructure) works with user-secrets.

### New files created
- `src/MangaManagementSystem.Web/Services/Api/IRegistrationApiClient.cs` — interface with `SendOtpAsync` and `CompleteRegistrationAsync` methods.
- `src/MangaManagementSystem.Web/Services/Api/RegistrationApiClient.cs` — implementation using `HttpClient.PostAsJsonAsync`. Deserializes `ApiErrorResponse` from 400/409 responses and throws `InvalidOperationException` (same exception type the page's catch blocks already handle). No business logic, no SP calls.
- `src/MangaManagementSystem.Web/Options/ApiSettings.cs` — options class with `BaseUrl` property.

### Config changes
- `src/MangaManagementSystem.Web/appsettings.json` — added `ApiSettings:BaseUrl: "http://localhost:5234"`.
- `src/MangaManagementSystem.Web/Program.cs` — registered `ApiSettings` binding, added `AddHttpClient<IRegistrationApiClient, RegistrationApiClient>` with base address from settings.

### Web registration page changes
- `RegisterPage.razor` now injects both `IAuthService` (retained for fallback) and `IRegistrationApiClient` (primary path).
- `@using MangaManagementSystem.Web.Services.Api` added.
- `_useApiClient` flag tracks which path was chosen during OTP send.
- **Branching logic:**
  - If **no portfolio file** selected → use `RegistrationApiClient` (API JSON endpoint).
  - If **portfolio file** selected → use `IAuthService` directly (old path). Marked with `// TODO: Replace with API multipart endpoint once available.`
- No other auth flows (login, Google, admin) were touched.

### Build result
- Full solution: **0 errors, 0 warnings** (incremental build).
- Both Web and API projects compile cleanly.
- No business logic added to Razor components; no SP logic exposed; no GUID changes.

### Remaining design notes
- Portfolio upload still needs a multipart API endpoint for full parity. The temporary dual-path (API for non-portfolio, direct service for portfolio) is safe because both paths use the same Application services underneath.
- Login, Google OAuth, admin user management, and `verify-otp` page all still call `IAuthService` directly. They are deliberately not touched.
- The `RegistrationApiClient` defines a private `ApiErrorResponse` record mirroring the API contract; no coupling to the API project's types.

## Updated Remaining Risks
- ~~**API host configuration:**~~ **RESOLVED** (setup-secrets.ps1).
- **Portfolio upload not in API JSON contract** yet; Web falls back to direct `IAuthService` when portfolio is selected. Needs a multipart API endpoint for full parity.
- ~~**Web not yet calling the API:**~~ **RESOLVED** — registration flow uses typed API client when no portfolio is selected.
- **Login, Google auth, admin users** still call `IAuthService` directly; not yet migrated.
- **Task EF Update/Delete** still bypasses SP workflow for status transitions.
- Stale top-level `MangaManagementSystem.API` project still on disk (unreferenced).

## Session 4: Fix reCAPTCHA JS Interop Issue (2026-06-14)

### Task
Fix repeated `Error rendering register reCAPTCHA: An exception occurred executing JS interop: Null object cannot be converted to a value type` during no-portfolio registration flow.

### Root cause
Two interacting issues in `RegisterPage.razor` and `recaptcha.js`:

1. **`OnAfterRenderAsync` fires on every render** with no `_recaptchaRendered` guard. The `_recaptchaRendered` field was declared but never set to `true`. On every Blazor state change (e.g. `_isSendingOtp` toggling), `OnAfterRenderAsync` re-invoked `recaptchaInterop.render`, which found the existing widget iframe and returned `null`.

2. **Blazor JS interop marshalling of JS `null` → C# `int?` fails.** `InvokeAsync<int?>` throws `"Null object cannot be converted to a value type"` when the JS function returns `null`. This is a known Blazor JS interop limitation with nullable value types.

### Files changed

- **`src/MangaManagementSystem.Web/wwwroot/js/recaptcha.js`** — All `return null` paths in the `render` function changed to `return -1`. This ensures JS always returns a number, avoiding the nullable marshalling issue entirely.

- **`src/MangaManagementSystem.Web/Components/Pages/RegisterPage.razor`**:
  - `OnAfterRenderAsync`: changed `InvokeAsync<int?>` to `InvokeAsync<int>` (non-nullable). Added `_recaptchaRendered` early-return guard so render is only attempted once. Only updates `_recaptchaWidgetId` when `widgetId > -1`. Error messages changed from `"Error rendering..."` to `"Warning: reCAPTCHA render failed: ..."` — no raw interop errors shown to user.
  - `HandleSendOtp`: added `_recaptchaWidgetId is null` guard that shows a friendly snackbar message if reCAPTCHA is unavailable.
  - `ResetRegistration`: changed from `void` to `async Task`. Instead of nulling out the widget ID (which would break the next OTP send), it calls `recaptchaInterop.reset` on the existing widget, keeping the widget ID valid.

### How the JS/C# contract was fixed
- **JS** always returns a number (`-1` for error/already-rendered, widget ID for success).
- **C#** uses `InvokeAsync<int>` (not `int?`), checks `widgetId > -1`.
- **Guard** with `_recaptchaRendered` prevents repeated JS calls.
- **On reset**, the existing widget is reused via `grecaptcha.reset()` rather than destroyed and re-rendered.

### Build result
- Full solution: **0 errors, 0 warnings**.
- OpenCode did not perform runtime testing (API launch, Web launch, browser interop). Manual testing is required.

### Remaining warnings (backlog, not addressed)
- **EF `Chapter.SeriesId1` shadow FK warning** — not chased per task scope.
- **HTTPS redirection warning** (`Failed to determine the https port for redirect`) — caused by running the HTTP launch profile; does not block manual testing.

### Manual testing checklist (developer must run locally)

1. **Setup secrets** (if not already done):
   ```powershell
   .\setup-secrets.ps1 -Project Both
   ```

2. **Start API** (in terminal 1):
   ```powershell
   dotnet run --project src\MangaManagementSystem.API\MangaManagementSystem.API.csproj --launch-profile http
   ```
   Open `http://localhost:5234/swagger` to confirm Swagger loads.

3. **Start Web** (in terminal 2):
   ```powershell
   dotnet run --project src\MangaManagementSystem.Web\MangaManagementSystem.Web.csproj
   ```

4. **Test no-portfolio registration**:
   - Open Web URL (typically `http://localhost:5000` or `https://localhost:5001`).
   - Navigate to `/register`.
   - Fill in: Username, Display Name, Role (`Mangaka`), Email, Password.
   - **Do not select a portfolio file.**
   - Complete the reCAPTCHA challenge.
   - Click **Send OTP**.
   - **Expected**: No repeated "Error rendering register reCAPTCHA" in Web console. Web sends `POST http://localhost:5234/api/registration/otp`. API console shows the request was received and processed.
   - Check email or server console for the 6-digit OTP code.
   - Enter the OTP and click **Verify & Register**.
   - **Expected**: "Registration successful" message. Account is pending admin approval.

5. **Check all consoles**:
   - **Web console**: No repeated reCAPTCHA interop errors.
   - **API console**: Request logged, no crashes.
   - **Browser console** (F12): No JS exceptions.

If the OTP send fails:
- Verify API is running on `http://localhost:5234`.
- Verify `setup-secrets.ps1 -Project Both` was run (API needs user-secrets).
- Check Web's `appsettings.json` has `ApiSettings:BaseUrl: "http://localhost:5234"`.
- Check API console for error details (do not share secrets in logs).

## Session 5: Fix OTP Send Error Handling in RegistrationApiClient (2026-06-14)

### Task
Fix `"Unable to send verification code. Please try again."` shown to user after Web successfully sends `POST /api/registration/otp` to the API.

### Root cause
The `RegistrationApiClient.SendOtpAsync()` only handled `200 OK` and `409 Conflict`. Any other response (e.g., `400 BadRequest` from model validation, or `500 InternalServerError` from server error) fell through to a hardcoded generic `InvalidOperationException("Unable to send verification code. Please try again.")`, discarding whatever clean, safe message the API returned.

The old direct `IAuthService` path never exposed this because Blazor constructs the `RegisterDto` directly without ASP.NET Core model binding — DataAnnotations are not enforced. The API path enforces them via `ModelState.IsValid`, so validation failures (e.g., short password) return `400 ValidationProblemDetails` with specific error messages.

### What was changed
- **`RegistrationApiClient.cs`** — Complete rewrite of both `SendOtpAsync` and `CompleteRegistrationAsync` error handling:
  - Added `ILogger<RegistrationApiClient>` for diagnostics (logs status code and reason phrase only — no secrets, no passwords, no request body).
  - Added `ExtractErrorMessageAsync` helper that reads the response body as JSON and extracts the most relevant message:
    1. `"message"` property → `ApiErrorResponse` format (409 Conflict from controller).
    2. `"detail"` property → `ProblemDetails` format (500 InternalServerError).
    3. `"errors"` dictionary → `ValidationProblemDetails` format (400 BadRequest, returns first field-level error).
    4. `"title"` property → fallback `ProblemDetails` title.
    5. Non-JSON/empty body → generic safe fallback.
  - Both methods now pass the extracted message through `InvalidOperationException`, so the page's `catch (InvalidOperationException ex)` shows the actual API error.

### How error handling was improved
- **400 BadRequest** (e.g., short password, invalid email): User now sees the specific validation message (e.g., `"The field Password must be a string with a minimum length of 8 and a maximum length of 255."`) instead of the generic fallback.
- **409 Conflict** (e.g., duplicate email/username): Existing behavior preserved — user sees `"An account with this email already exists."`.
- **500 InternalServerError**: User now sees `"We could not start registration right now. Please try again later."` (the API's `ProblemDetails.detail`) instead of a different generic message.
- **Non-JSON/unexpected body**: Falls back to safe `"An unexpected error occurred. Please try again."`.
- **Diagnostics**: Web server log now records `"Registration OTP send failed: {StatusCode} {ReasonPhrase}"` — sufficient for debugging without leaking secrets.

### File changed
- `src/MangaManagementSystem.Web/Services/Api/RegistrationApiClient.cs`

### Build result
- Full solution: **0 errors**.
- OpenCode did not perform runtime testing. Manual retest required.

### Manual retest checklist (developer must run locally)

1. **Start API** (terminal 1):
   ```powershell
   dotnet run --project src\MangaManagementSystem.API\MangaManagementSystem.API.csproj --launch-profile http
   ```

2. **Start Web** (terminal 2):
   ```powershell
   dotnet run --project src\MangaManagementSystem.Web\MangaManagementSystem.Web.csproj
   ```

3. **Test no-portfolio registration**:
   - Open Web URL → navigate to `/register`.
   - Fill form (no portfolio), complete reCAPTCHA, click **Send OTP**.
   - **Expected**: Web sends `POST http://localhost:5234/api/registration/otp`.
   - **Expected**: No generic error. See meaningful message:
     - If duplicate email/username: `"An account with this email already exists."` / `"This username is already taken."`.
     - If short password: `"The field Password must be a string with a minimum length of 8..."`.
     - If SMTP fails (and not mock): `"We could not start registration right now. Please try again later."`.
     - If success: `"Verification code sent."` (snackbar) + OTP email/console.

4. **Check API console**: Should log the incoming request and any errors (secrets are never logged by the controller or client).

5. **Check Web console**: Should log `"Registration OTP send failed: {StatusCode}"` only if error — no secrets.

6. **Test the complete path**: If OTP was sent successfully, enter the 6-digit code and click **Verify & Register**. Should complete registration.

## Updated Remaining Risks
- ~~**API host configuration:**~~ **RESOLVED** (setup-secrets.ps1).
- ~~**reCAPTCHA JS interop error:**~~ **RESOLVED** (Session 4).
- ~~**OTP error handling:**~~ **RESOLVED** (Session 5 — client parses all API error shapes).
- **Portfolio upload not in API JSON contract** yet; Web falls back to direct `IAuthService` when portfolio is selected.
- ~~**Web not yet calling the API:**~~ **RESOLVED** — registration flow uses typed API client when no portfolio.
- **Login, Google auth, admin users** still call `IAuthService` directly.
- **Task EF Update/Delete** still bypasses SP workflow.
- **EF `Chapter.SeriesId1` shadow FK** — backlog, not blocking.
- **HTTPS redirect warning** — backlog, only appears with HTTP launch profile.
- Stale top-level `MangaManagementSystem.API` project still on disk (unreferenced).

## Next Recommended Prompt
> Manually retest no-portfolio registration after the error handling fix. Once confirmed working, add a multipart portfolio-upload endpoint to the API (`POST /api/registration/portfolio`) and remove the fallback `IAuthService` path from `RegisterPage.razor`. Then migrate the login flow to the API with an `ILoginApiClient`. Separately, wire `manga.usp_ChapterPageTask_Cancel` in `ChapterPageTaskService` and remove the stale top-level API project.
