# AI Agent Skills Guide - Manga Management System (MCWPMS)

This document configures the operational pipeline and system instructions for AI Agents and GitHub Copilot to analyze, decompose, and implement features within the **Manga Creation Workflow and Publishing Management System (MCWPMS)**.

---

## 🧱 SYSTEM ARCHITECTURE BASELINE

AI Agents and GitHub Copilot must treat the following architecture as the baseline for all implementation plans, task decomposition, and code-generation suggestions.

### Architecture Style: Distributed Monolith

The system follows a **Distributed Monolith** style. The core business workflow remains inside one main .NET application, while heavy AI processing is separated into a local AI Hub service. This keeps manga workflow logic maintainable while preventing AI inference tasks from slowing down or tightly coupling with the main business system.

### Frontend / Client Side

- **Technology:** Blazor Server on .NET 8.
- **Reasoning:** Blazor Server is preferred for this student MVP because it is beginner-friendly for a C# team and supports interactive UI behavior through the server-side SignalR connection.
- **UI framework:** MudBlazor for admin pages, dashboards, forms, workflow queues, tables, dialogs, snackbars, and general management screens.
- **Manga reader style:** Webtoon-style vertical scrolling. Chapter pages should be rendered as ordered current page-version images in a continuous vertical reader instead of merging all pages into one giant source image by default.
- **Canvas interaction:** Fabric.js over the HTML5 Canvas API for interactive page overlays, region marking, annotation boxes, panel regions, speech bubble regions, and feedback areas.

### Backend / Server Side

- **Technology:** ASP.NET Core Web API on .NET 8.
- **Architecture pattern:** Clean Architecture with four layers:
  1. **Domain** — entities, enums/value objects, and domain concepts that do not depend on Application, Infrastructure, Web, or API.
  2. **Application** — use cases expressed through CQRS commands/queries, MediatR request/handler types, DTOs, validators, service interfaces when still needed, workflow orchestration, and business validation. Application may depend on Domain, but must not depend on Infrastructure, API, or Web.
  3. **Infrastructure** — EF Core, SQL Server stored-procedure wrappers, repository implementations, Cloudinary integration, SMTP/email, OTP cache, AI Hub adapters, and other external service adapters. Infrastructure may depend on Application and Domain.
  4. **API** — thin ASP.NET Core controllers/endpoints, request/response contracts, authentication/authorization boundaries, HTTP status-code mapping, and HTTP-specific concerns. API may depend on Application and Infrastructure for dependency injection only.
- **Rule:** Business logic must not be placed directly in UI components, API controllers, or database-specific classes when it belongs in Domain/Application.
- **Rule:** SQL Server, EF Core, Cloudinary, SMTP, and AI Hub implementation details must stay in Infrastructure. They must not leak into Web UI components or API controllers.

### Clean Architecture Dependency Rules

AI Agents must preserve these dependency directions exactly:

```text
Domain           <- Application <- Infrastructure
Application      <- API
API              <- Web only through HTTP calls, not project references for business workflows
Web              -> API through typed HTTP clients
```

Allowed project references and calls:

| Source project | May depend on / call | Must not depend on / call |
|---|---|---|
| `MangaManagementSystem.Domain` | Nothing project-specific | Application, Infrastructure, API, Web |
| `MangaManagementSystem.Application` | Domain | Infrastructure, API, Web, EF Core concrete DbContext, Cloudinary SDK, SMTP SDK |
| `MangaManagementSystem.Infrastructure` | Application, Domain | Web, API controllers |
| `MangaManagementSystem.API` | Application services/interfaces, Infrastructure DI registration | Web project, Razor components, UI services |
| `MangaManagementSystem.Web` | Web UI code, typed API clients, local session/cookie helpers | Application services for migrated workflows, Infrastructure services, repositories, DbContext, stored procedures |

Required flow for new business features:

```text
Blazor Web page / component
→ typed Web API client, e.g. IMangakaSeriesApiClient
→ ASP.NET Core API controller, e.g. MangakaSeriesController
→ Application command/query handler through MediatR, e.g. CreateSeriesDraftCommandHandler
→ Infrastructure repository / stored-procedure wrapper
→ SQL Server stored procedure / EF read query
```

Do not implement new business workflows with this anti-pattern:

```text
Blazor Web page
→ Application service directly
→ Infrastructure / DbContext / stored procedure
```

The Web project may temporarily contain legacy direct Application service calls while migration is in progress, but AI Agents must not add new direct Web-to-Application business workflow calls. When touching a migrated or new workflow, route it through the API.


### CQRS + MediatR Application Rules

For new or migrated business features, AI Agents should prefer a **CQRS-style Application layer using MediatR** instead of adding more large, all-purpose Application services.

Required intent:

```text
Command = changes state / performs a business workflow
Query   = reads data / builds a screen or read model
```

Preferred new-feature flow:

```text
Web Razor Page
→ typed Web API client
→ API Controller
→ IMediator.Send(command/query)
→ Application Command/Query Handler
→ Application-facing repository/storage/email abstractions
→ Infrastructure implementation
→ SQL stored procedure / EF read query / external adapter
```

Use commands for workflows such as:

- create series draft,
- submit proposal,
- update profile file,
- reset password,
- assign task,
- complete task,
- approve/reject/cancel workflow actions,
- any action that writes audit events or changes workflow status.

Use queries for read-only screens such as:

- dashboard series list,
- task list,
- profile summary,
- review queues,
- ranking display,
- audit feed.

Recommended folder shape:

```text
MangaManagementSystem.Application
└── Features
    └── Mangaka
        └── Series
            ├── Commands
            │   └── CreateSeriesDraft
            │       ├── CreateSeriesDraftCommand.cs
            │       ├── CreateSeriesDraftCommandHandler.cs
            │       └── CreateSeriesDraftResult.cs
            └── Queries
                └── GetMySeries
                    ├── GetMySeriesQuery.cs
                    ├── GetMySeriesQueryHandler.cs
                    └── MangakaSeriesListItemDto.cs
```

Command/query conventions:

- Use `IRequest<TResponse>` for commands and queries.
- Use one handler per command/query.
- Use `CancellationToken` on all async handler methods.
- Keep request DTOs/contracts in API or Web boundary models when they are HTTP-specific; map them into Application commands/queries.
- Keep business validation in Application handlers or validators, not Razor pages or API controllers.
- Use FluentValidation or explicit validation helpers when available.
- A command handler may orchestrate Cloudinary upload through an Application-facing abstraction and then call an Infrastructure stored-procedure wrapper.
- A query handler may call repository/read-model methods using EF Core for simple reads.
- Do not mix write workflow logic into query handlers.
- Do not perform database writes from query handlers.
- Do not call MediatR from Razor components directly. Razor components call Web typed API clients only.

Compatibility rule for existing code:

- Existing Application services may remain while migration is in progress.
- Do not perform broad refactors just to convert every service into MediatR.
- When touching a new or migrated workflow, prefer adding a command/query and handler.
- If an existing service method already contains valid orchestration and changing it is too risky, a command handler may delegate to that service temporarily, but the session note must record this as transitional technical debt.
- For current branch work, avoid creating duplicate implementations of the same workflow in both a service method and a handler unless one is explicitly marked as transitional.

Example API controller using MediatR:

```csharp
[ApiController]
[Route("api/mangaka/series")]
public sealed class MangakaSeriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MangakaSeriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("drafts")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CreateSeriesDraftResult>> CreateDraftAsync(
        [FromForm] CreateSeriesDraftForm form,
        CancellationToken cancellationToken)
    {
        var actorUserId = /* read from claims or transitional Web-to-API context */;

        var command = form.ToCommand(actorUserId);

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetByIdAsync), new { seriesId = result.SeriesId }, result);
    }
}
```

Example command handler responsibility:

```text
CreateSeriesDraftCommandHandler
- validate title/synopsis/genre/content language
- normalize or generate slug
- validate optional cover file metadata
- upload cover through storage abstraction when present
- call ISeriesRepository.CreateSeriesDraftViaProcAsync
- compensate Cloudinary upload if SQL workflow fails
- return CreateSeriesDraftResult
```


### API Controller Rules

Controllers must be thin HTTP adapters. They may:

- accept route/query/body/form data,
- bind `multipart/form-data` files when needed,
- read the authenticated actor/user id from claims or the current transitional Web-to-API request pattern,
- call one Application use-case method or `IMediator.Send(command/query)`,
- map known Application exceptions to safe HTTP responses,
- return DTOs or response contracts.

Controllers must not:

- call EF Core `DbContext` directly,
- call repositories directly unless the repository is explicitly an Application-facing abstraction and no service exists,
- call stored procedures directly,
- upload to Cloudinary directly,
- compute workflow status transitions directly,
- create audit records directly,
- contain cross-table business rules,
- expose raw SQL errors, stack traces, password hashes, OTP codes, API keys, or secrets.

Example thin API controller shape:

```csharp
[ApiController]
[Route("api/mangaka/series")]
public sealed class MangakaSeriesController : ControllerBase
{
    private readonly ISeriesService _seriesService;

    public MangakaSeriesController(ISeriesService seriesService)
    {
        _seriesService = seriesService;
    }

    [HttpPost("drafts")]
    public async Task<ActionResult<SeriesDto>> CreateDraftAsync(
        [FromForm] CreateSeriesDraftForm request,
        CancellationToken cancellationToken)
    {
        var actorUserId = /* read from claims or transitional Web-to-API context */;

        var result = await _seriesService.CreateSeriesDraftAsync(
            actorUserId,
            request.ToApplicationDto(),
            cancellationToken);

        return CreatedAtAction(nameof(GetByIdAsync), new { seriesId = result.SeriesId }, result);
    }
}
```

### Web Project API Client Rules

The Web project must call the API through typed clients instead of scattering raw `HttpClient` calls across Razor components.

Required Web pattern:

```text
Components/Pages/Mangaka/MangakaDashboard.razor
→ Services/Api/IMangakaSeriesApiClient.cs
→ Services/Api/MangakaSeriesApiClient.cs
→ API endpoint
```

Typed API clients should:

- use `HttpClient` registered through `AddHttpClient`,
- use `ApiSettings:BaseUrl`,
- centralize JSON/multipart request construction,
- parse known `ApiErrorResponse`, `ProblemDetails`, and `ValidationProblemDetails`,
- throw safe `InvalidOperationException` messages for UI snackbar/display handling,
- avoid logging secrets, passwords, OTPs, tokens, or raw request bodies.

Example Web registration:

```csharp
builder.Services.AddHttpClient<IMangakaSeriesApiClient, MangakaSeriesApiClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
});
```

Razor components should inject typed clients:

```csharp
@inject IMangakaSeriesApiClient MangakaSeriesApiClient
```

Razor components should not inject Application services for new/migrated workflows:

```csharp
// Avoid for new API-migrated business workflows:
@inject ISeriesService SeriesService
@inject IChapterPageTaskService TaskService
@inject ApplicationDbContext DbContext
```

### Feature-Based API Organization

Do not create one centralized “God controller” or universal API endpoint. Use feature/resource-based controllers and clients.

Good examples:

```text
API Controllers:
- AuthController                 -> /api/auth/login
- RegistrationController         -> /api/registration/otp, /api/registration/complete
- MangakaSeriesController        -> /api/mangaka/series/drafts
- AdminUsersController           -> /api/admin/users
- ChapterPageTasksController     -> /api/chapter-page-tasks

Web typed clients:
- IAuthApiClient / AuthApiClient
- IRegistrationApiClient / RegistrationApiClient
- IMangakaSeriesApiClient / MangakaSeriesApiClient
- IAdminUserApiClient / AdminUserApiClient
```

Bad examples:

```text
- CentralApiController.DoEverything()
- POST /api/execute-action
- UniversalApiClient.Call(string actionName, object payload)
```

### AI Microservice / AI Hub

- **Technology:** Python FastAPI running locally for MVP.
- **AI model:** YOLO-based model for panel/region detection when processing manga page images.
- **Input/output contract:** The AI Hub returns JSON suggestions such as region type, `x`, `y`, `width`, `height`, and confidence score.
- **Boundary:** AI output is advisory only. It must not automatically approve, reject, cancel, serialize, publish, or otherwise change workflow status. Human users must review AI suggestions before accepted output is saved as `PageRegion` records.

### Database and File Storage

- **Database:** SQL Server 2022+ accessed through Entity Framework Core.
- **Schemas:**
  - `auth` — users, roles, login/account state, and one MVP role per user.
  - `manga` — manga workflow data such as series, proposals, chapters, pages, page versions, regions, annotations, tasks, board polls, board votes, ranking snapshots, notifications, and file metadata.
  - `audit` — append-only audit events for important business/security actions.
- **File storage:** Cloudinary stores actual uploaded files and media assets. SQL Server stores only metadata and references in `manga.FileResource`.
- **File rule:** Business tables must reference uploaded files through `file_resource_id`; they must not store raw Cloudinary URLs directly when a `FileResource` relationship is available.
- **Upload flow:** Backend validates the file and uploads it to Cloudinary outside the SQL transaction. After Cloudinary returns `cloudinary_public_id`, `cloudinary_secure_url`, content metadata, and hash data, the backend must call the relevant SQL Server stored procedure or wrapper procedure so `manga.FileResource`, business-table links, and audit events are written atomically inside SQL Server.

### Database Workflow and Transaction Ownership

AI Agents and GitHub Copilot must follow a **stored-procedure-first rule** for important database workflows.

#### Core Boundary Rule

- **C# / ASP.NET Core responsibilities:**
  - Handle UI/API requests.
  - Validate external input before calling the database.
  - Call external services such as Cloudinary and the local AI Hub.
  - Upload/delete Cloudinary assets.
  - Perform compensation cleanup when an external operation succeeds but the database workflow fails.
  - Call SQL Server stored procedures through Infrastructure repository methods.

- **SQL Server stored procedure responsibilities:**
  - Own important database transactions.
  - Insert/update/link multiple related tables atomically.
  - Validate workflow state transitions, actor permissions, cross-table rules, and concurrency-sensitive conditions.
  - Use `TRY...CATCH`, `SET XACT_ABORT ON`, and transaction ownership patterns where appropriate.
  - Use `sp_getapplock`, `UPDLOCK`, or `HOLDLOCK` when a workflow is concurrency-sensitive.
  - Write audit events through `audit.usp_AuditEvent_Append`.

#### When Stored Procedures Are Required

Use SQL Server stored procedures or wrapper procedures for workflows that involve any of the following:

- multiple table writes,
- status transitions,
- approval/rejection/cancellation decisions,
- account/security actions,
- `FileResource` creation plus business-record linking,
- audit logging,
- concurrency-sensitive updates,
- business rules that must roll back together.

Examples include account creation, registration portfolio linking, admin account-status changes, proposal submission, page-version creation, task completion, board poll open/close/cancel, board result application, publication-frequency changes, and file soft deletion.

#### When EF Core Queries Are Acceptable

EF Core may be used for simple reads, list screens, filters, dashboard queries, and straightforward single-table operations that do not require a business workflow transaction or audit event. Do not use separate uncoordinated EF Core `SaveChanges` calls for multi-table workflows that should be atomic.

#### Calling Stored Procedures from C#

The Infrastructure layer may call stored procedures directly using `Microsoft.Data.SqlClient` / ADO.NET when the procedure has output parameters or complex parameter lists.

Required conventions:

- Use `SqlCommand.CommandType = CommandType.StoredProcedure`.
- Use strongly typed `SqlParameter` values.
- Use `DBNull.Value` for optional SQL parameters.
- Capture output parameters explicitly.
- Keep stored-procedure call methods inside Infrastructure repositories/adapters.
- Expose workflow-oriented methods to Application services instead of leaking SQL details into UI components.

#### Cloudinary + SQL Transaction Rule

Cloudinary operations are outside SQL Server transactions. Therefore:

1. The backend validates the file.
2. The backend uploads the file to Cloudinary.
3. The backend calls one SQL stored procedure/wrapper for the database workflow.
4. The stored procedure handles all SQL writes atomically.
5. If the SQL procedure fails after Cloudinary upload succeeds, the backend must attempt Cloudinary cleanup using the uploaded `cloudinary_public_id`, or log a clear cleanup TODO if immediate cleanup is not yet implemented.

Never keep a SQL transaction open while uploading to Cloudinary.


### High-Level Communication Flow

1. Blazor Server UI sends requests to the ASP.NET Core API through typed Web API clients. New or migrated business workflows must not call Application services directly from Web.
2. ASP.NET Core API controllers handle HTTP binding and authorization boundaries, then call Application use cases. Application orchestrates validation/workflow rules and Infrastructure adapters handle Cloudinary, AI Hub, SMTP, EF Core, and stored-procedure calls.
3. SQL Server stored procedures own important database transactions, multi-table writes, workflow state transitions, file metadata linking, and audit-event appends.
4. Cloudinary stores the actual files/images and provides secure delivery URLs.
5. For AI-assisted detection, the backend sends the page image or image URL to the FastAPI AI Hub.
6. The AI Hub returns JSON region suggestions.
7. The user reviews and accepts useful suggestions before they are saved as `PageRegion` records.

### Current Core Actors

All user stories and RBAC checks must use the current MVP actor model:

- **Mangaka**
- **Assistant**
- **Tantou Editor**
- **Editorial Board Member**
- **Editorial Board Chief**
- **Admin**

Important responsibility boundaries:

- **Editorial Board Chief** opens, closes, and cancels board polls and specifies the board-approved publication frequency when opening a `START_SERIALIZATION` poll.
- **Admin** manages accounts, file deletion workflow, audit visibility, traceability, and system-level management, but does not own normal board poll open/close/cancel behavior.
- **Mangaka** may provide or update desired publication frequency only while the series is in `PROPOSAL_DRAFT`.
- **Cloudinary API secrets must stay on the backend or in local/deployment secrets, never in frontend code or committed Git files.**
- **External services stay in C#; database workflows stay in stored procedures.** The backend may upload to Cloudinary or call the AI Hub, but important SQL changes must be handled through stored procedures or wrapper procedures.

---

## 🛠 SYSTEM WORKFLOW PIPELINE

AI Agents must follow this exact sequential pipeline when interpreting user requests, text specifications, or generating pull requests:

1. `init_product_vision` ➔ Establishes boundaries based on the latest relational database schema.
2. `generate_user_stories` ➔ Creates discrete user stories for the current 6 MVP roles.
3. `decompose_epic_to_tasks` ➔ Splits requirements into Blazor UI and .NET Clean Architecture sub-tasks.
4. `calculate_priority_matrix` ➔ Approves prioritization using Karl Wiegers matrix matching MVP constraints.
5. `refine_functional_requirements` ➔ Converts logic to strict "The system shall..." statements using explicit C# types.
6. `design_application_cqrs_use_case` ➔ Decides the Application command/query, MediatR handler, DTO/result, and validation boundaries.
7. `design_database_workflow` ➔ Decides whether the feature requires a stored procedure/wrapper procedure and defines SQL transaction/audit boundaries.
8. `generate_acceptance_criteria` ➔ Writes BDD tests validating both C# business rules, stored-procedure effects, and Blazor UI states.
9. `write_session_summary` ➔ Writes a clear task/session note under `docs/revision` or the matching use-case subfolder.
10. `sync_to_github_projects` ➔ Automates GitHub tracking using `gh` CLI.

---

## 📋 DETAILED SKILLS SPECIFICATIONS FOR GITHUB COPILOT

### 1. Skill Name: `init_product_vision`
* **System Prompt Constraint:** You are an expert Enterprise Business Analyst. When given any feature request, map it strictly against the MCWPMS architecture.
* **Execution Rules:**
  - Reject requests involving out-of-scope modules: full illustration editors, public reader portals, or platform wallets.
  - Force constraints into 3 core schemas: `auth` (Simple RBAC with 6 MVP roles), `audit` (append-only AuditEvent logging), and `manga` (Workflow operations).

### 2. Skill Name: `generate_user_stories`
* **System Prompt Constraint:** Translate raw feature descriptions into Agile User Stories.
* **Execution Rules:**
  - Strictly assign stories to one of the 6 valid actors: **Mangaka**, **Assistant**, **Tantou Editor**, **Editorial Board Member**, **Editorial Board Chief**, or **Admin**.
  - Format: `As a [Role], I want to [Action] so that [Value].`
  - Automatically attach the corresponding RBAC authorization tag: `[Authorize(Roles = "RoleName")]`.

### 3. Skill Name: `decompose_epic_to_tasks`
* **System Prompt Constraint:** Break down complex Epics into technical checklist sub-tasks.
* **Execution Rules:**
  - Every technical breakdown MUST generate four distinct layers formatted as a Markdown checklist (`- [ ]`):
    1. **Frontend (FE):** MudBlazor `.razor` component placement in the Blazor Server Web project plus typed Web API client changes. Web pages must call API clients for new/migrated business workflows, not Application services directly.
    2. **Backend (BE):** Feature-based API controller/endpoints, request/response contracts, MediatR commands/queries/handlers, Application DTOs/results/validators, Domain rules, EF Core Fluent API configurations in `Infrastructure`, and Infrastructure repository methods that call SQL Server stored procedures for important database workflows.
    3. **AI Integration:** Async integration hooks to Python FastAPI (YOLO-based panel/region detection) if processing `ChapterPage` canvases.
    4. **Exceptions & Audit:** Custom middleware handling, stored-procedure audit calls through `audit.usp_AuditEvent_Append`, and compensation logic for external operations such as Cloudinary cleanup after SQL failure.

### 4. Skill Name: `design_database_workflow`
* **System Prompt Constraint:** Decide the correct database-access pattern before generating implementation code.
* **Execution Rules:**
  - Use SQL Server stored procedures or wrapper procedures for business workflows that write multiple tables, change status, write audit events, require concurrency protection, or must roll back atomically.
  - Do not implement multi-table workflow writes as several separate EF Core `SaveChangesAsync` calls unless an explicit shared application transaction is already established and approved.
  - Prefer one wrapper procedure for one business action, such as `auth.usp_User_CreateWithOptionalPortfolio` for user creation plus optional portfolio `FileResource` creation/linking.
  - C# must call stored procedures directly from the Infrastructure layer using `Microsoft.Data.SqlClient` / ADO.NET when output parameters or complex stored-procedure contracts are required.
  - EF Core is acceptable for simple reads, search/list screens, dashboard queries, and uncomplicated single-table operations.
  - External systems such as Cloudinary must be called from C# before or after the SQL workflow; SQL Server procedures must not call external cloud APIs.
  - For Cloudinary-backed workflows, upload first, then call the SQL wrapper procedure, then attempt Cloudinary cleanup if the SQL procedure fails.
  - Every generated workflow must explicitly state its transaction boundary, audit behavior, and rollback/compensation behavior.



### 4A. Skill Name: `design_application_cqrs_use_case`
* **System Prompt Constraint:** Before generating backend code, decide whether the Application boundary should be implemented as a command, query, or existing transitional service method.
* **Execution Rules:**
  - Use a **Command** for any operation that changes state, writes files, calls stored procedures, changes workflow status, writes audit events, sends OTP/email, or performs security/account actions.
  - Use a **Query** for read-only dashboard/list/detail screens.
  - Prefer MediatR `IRequest<TResponse>` and one handler per command/query for new or migrated features.
  - Place new command/query code under a feature-based Application folder such as `Features/Mangaka/Series/Commands/CreateSeriesDraft`.
  - API controllers should call `IMediator.Send(...)` for new CQRS handlers.
  - Handlers may call Application-facing repository/storage/email abstractions, but must not depend on Web, API, EF `DbContext`, SQL client code, or Cloudinary SDK directly.
  - Important mutating command handlers must call Infrastructure stored-procedure wrappers instead of performing several independent EF `SaveChangesAsync` calls.
  - Query handlers may use read repository methods or EF-backed query abstractions for simple read models.
  - Do not call MediatR directly from Blazor Razor components. Web must still call API through typed API clients.
  - If a legacy Application service remains in use, mark it as transitional in the session note and do not create duplicate business logic in multiple places.


### 5. Skill Name: `enforce_clean_architecture`
* **System Prompt Constraint:** Before generating or modifying code, verify that the proposed implementation preserves Clean Architecture dependency boundaries and the Web-to-API rule.
* **Execution Rules:**
  - For every feature, explicitly state which code belongs in `Domain`, `Application`, `Infrastructure`, `API`, and `Web`.
  - Web must call the API via typed API clients for all new or migrated business workflows.
  - API controllers must be thin and must delegate business work to Application commands/queries through MediatR or to an existing Application service when a workflow has not yet been migrated.
  - Application services must not depend on API/Web/Infrastructure concrete implementation details.
  - Infrastructure must hide EF Core, stored-procedure, Cloudinary, SMTP, and AI Hub details behind Application-facing interfaces.
  - Do not add new direct injections of Application workflow services into Razor components. If an existing Web page already injects an Application service, either leave it untouched when out of scope or migrate that workflow to an API client when the page is modified.
  - Do not create centralized all-purpose controllers or universal API clients. Use feature-based controllers and typed clients.
  - When reviewing code, flag these violations explicitly:
    - Razor component calls `DbContext`, repositories, stored procedures, or Infrastructure services.
    - Razor component directly calls Application service for a new/migrated workflow.
    - API controller contains Cloudinary upload logic, SQL/stored-procedure logic, audit writes, or workflow status rules.
    - Application references Infrastructure, API, or Web.
    - Infrastructure references Web.
    - Controller returns password hashes, OTPs, secrets, raw SQL errors, or stack traces.
  - Required output for each implementation plan:
    ```text
    Clean Architecture placement:
    - Web: ...
    - API: thin controller route and IMediator.Send(command/query) or existing use-case call
    - Application: command/query/handler or transitional service method, DTOs/results/validators
    - Infrastructure: ...
    - Domain: ...

    Web-to-API flow:
    Razor Page -> Typed API Client -> API Controller -> Application Service -> Infrastructure -> DB/SP
    ```

#### Example: Create Series Draft

Correct implementation:

```text
Web:
- MangakaDashboard.razor collects title/synopsis/genre/optional cover.
- IMangakaSeriesApiClient sends multipart/form-data to API.

API:
- MangakaSeriesController exposes POST /api/mangaka/series/drafts.
- Controller reads form fields and optional file bytes, resolves actor id, calls Application.

Application:
- Prefer CreateSeriesDraftCommand + CreateSeriesDraftCommandHandler through MediatR for new/migrated code.
- If the existing ISeriesService.CreateSeriesDraftAsync path is kept temporarily, document it as transitional.
- The handler/service validates draft input, slug rules, actor intent, and coordinates storage/SP workflow through interfaces.

Infrastructure:
- Uploads cover to Cloudinary if present.
- Computes SHA256 hash.
- Calls manga.usp_Series_Create with output parameters.
- Cleans up Cloudinary asset if SQL workflow fails.

Domain:
- Series entity and domain constants/status concepts.
```

Incorrect implementation:

```text
MangakaDashboard.razor injects ISeriesService and IFileStorageService, uploads cover directly, creates FileResource manually, and calls EF SaveChanges.
```

### 6. Skill Name: `calculate_priority_matrix`
* **System Prompt Constraint:** Rank requirements to protect the project deadline.
* **Execution Rules:**
  - Score Business Value, Relative Cost, and Relative Risk from 1 to 9.
  - Automatically label anything with high cost/risk and low core value as `priority:low` or `out-of-scope`.

### 7. Skill Name: `refine_functional_requirements`
* **System Prompt Constraint:** Enforce high-precision specification language.
* **Execution Rules:**
  - Convert descriptions into `"The system shall..."` structures.
  - Replace ambiguous verbs with specific database/code operations. Specify exact C# data types, Cloudinary `secure_url` properties, stored-procedure/wrapper-procedure names when needed, SQL transaction boundaries, or coordinate ranges `(X, Y, Width, Height)` mapping to `PageRegion`.

### 8. Skill Name: `generate_acceptance_criteria`
* **System Prompt Constraint:** Write comprehensive Behavioral-Driven Development (BDD) testing scenarios.
* **Execution Rules:**
  - Use the Cucumber framework: `Given - When - Then`.
  - Must write 2 Happy Path scenarios and 2 Edge Case scenarios (e.g., unauthorized role rejection, invalid file uploads, stored-procedure rollback, Cloudinary cleanup after SQL failure, audit log write failures, or unauthorized workflow transitions).
  - Explicitly state the MudBlazor UI feedback state (e.g., `<MudProgressCircular>`, `<MudDialog>`, or `ISnackbar` toast notification).

### 9. Skill Name: `write_session_summary`
* **System Prompt Constraint:** At the end of every coding session, interrupted continuation, merge-conflict fix, architecture refactor, API migration, or completed feature task, write a durable Markdown session note in the repository so future AI agents and developers can resume accurately.
* **Required Folder Root:** `MangaManagementSystem\MangaManagementSystem\docs\revision`
* **Use-Case Folder Rule:** Put the note in the most specific existing use-case/actor folder. If no matching folder exists, create one.

#### Required folder selection

Use these rules when choosing where to write the note:

| Task scope | Session note folder |
|---|---|
| Mangaka use cases, Mangaka dashboard, creator workspace, series draft/proposal/chapter/page workflows | `docs/revision/Mangaka` |
| Assistant use cases, assigned tasks, assistant studio/work submission | `docs/revision/Assistant` |
| Tantou Editor review/annotation/proposal-review workflows | `docs/revision/Editor` or create `docs/revision/TantouEditor` if that is the established repo naming |
| Editorial Board Member or Board Chief poll/vote workflows | `docs/revision/Board` or create `docs/revision/BoardChief` for Chief-only work |
| Admin account/file/audit/system management | `docs/revision/Admin` |
| General user/profile/avatar/portfolio/password flows | `docs/revision/Profile` or `docs/revision/GeneralUser` depending on existing repo folder naming |
| Auth, login, registration, OTP, Google auth, session/cookie work | `docs/revision/Auth` or `docs/revision/Registration` depending on existing repo folder naming |
| Cross-cutting architecture, Clean Architecture/CQRS/API-client conventions, shared infrastructure | `docs/revision` root or `docs/revision/Architecture` |
| Merge conflict resolution involving several modules | `docs/revision/Merge` or the dominant affected use-case folder |

If a folder does not exist, the AI Agent must create it before writing the note.

#### Required filename convention

Use a readable, chronological Markdown filename:

```text
yyyy-MM-dd-short-task-slug.md
```

Examples:

```text
docs/revision/Mangaka/2026-06-16-create-series-draft.md
docs/revision/Profile/2026-06-17-profile-password-reset-api-fix.md
docs/revision/Architecture/2026-06-17-clean-architecture-cqrs-api-client-rules.md
```

Do not overwrite an unrelated previous note. If updating the same session note is explicitly appropriate, append a clearly labeled update section with timestamp/context.

#### Required session note content

Every session note must include these sections when applicable:

```markdown
# Session Note — <Task Name>

- **Branch:** `<current git branch>`
- **Date:** `<yyyy-MM-dd>`
- **Scope:** `<actor/use case/module>`
- **Applies to:** `<current branch / main / incoming merge branch / cross-cutting>`
- **Related workflow:** `<short workflow name>`

## Task summary

Explain what was requested and what problem this session solved.

## Important decisions

List important architecture and business decisions made during the session.
Separate branch-specific decisions when needed:

### Applies to current/main branch
- ...

### Applies to incoming/merged branch only
- ...

### Deferred / out of scope
- ...

## Architecture flow

Show the final intended flow, for example:

```text
Razor Page
  -> typed Web API client
  -> API controller
  -> IMediator.Send(command/query)
  -> Application handler
  -> Infrastructure repository/SP wrapper
  -> SQL Server stored procedure / EF read query
```

## Files changed

Group by layer:

### Web
- `path/to/file` — what changed and why.

### API
- `path/to/file` — endpoint/contract changes.

### Application
- `path/to/file` — command/query/handler/DTO/validator/service changes.

### Infrastructure
- `path/to/file` — repository, SP wrapper, Cloudinary/SMTP/EF changes.

### Domain
- `path/to/file` — entity/value-object/domain-rule changes.

### Database / SQL
- `path/to/file-or-procedure` — stored procedure/table/mapping effects.

### Documentation
- `path/to/file` — docs/revision updates.

## CQRS / MediatR changes

- **Commands added/changed:** ...
- **Queries added/changed:** ...
- **Handlers added/changed:** ...
- **Validators added/changed:** ...
- **Controller dispatch pattern:** `IMediator.Send(...)` or explain transitional exception.

## API endpoints added/changed

- `METHOD /api/...` — request/response and purpose.

## Web API clients added/changed

- `I...ApiClient` / `...ApiClient` — endpoint called and UI pages using it.

## Database / stored procedure behavior

- Stored procedure used:
- Transaction owner:
- Audit behavior:
- Rollback/compensation behavior:

## UI behavior changed

Describe visible UI changes, dialogs, forms, buttons, snackbars, validation messages, and navigation.

## Validation and build result

- Command run: `dotnet build ...`
- Result: `Build succeeded` or exact failure summary.
- Runtime tests: state whether runtime tests were intentionally not run.

## Known issues / problems discovered

List current problems clearly. If the problem belongs to another branch, say so explicitly.

## Manual test checklist

1. ...
2. ...
3. ...

## Remaining follow-up tasks

- ...

## Resume prompt for next AI agent

Write a short continuation prompt that tells the next AI agent exactly what to inspect and continue, including branch scope and what not to touch.
```

#### Final response requirement

After writing the session note, the AI Agent's final response must mention:

- the session note path,
- files changed,
- build result,
- remaining follow-up tasks,
- and any branch-specific warnings.

#### Session summary quality rules

- Be specific enough that a new AI agent can resume without rereading the whole chat.
- Include exact file paths and API routes.
- Include exact stored procedure names when relevant.
- Distinguish completed work from partially completed or broken work.
- Distinguish current/main branch decisions from incoming branch issues during merges.
- Do not claim runtime testing was done if it was not done.
- Do not hide known failures; record them under `Known issues / problems discovered`.
- Do not paste secrets, passwords, OTPs, API keys, connection strings, or private tokens.


---

## ✅ CLEAN ARCHITECTURE REVIEW CHECKLIST

Before accepting generated code, AI Agents and reviewers must check the following:

### Web Project

- [ ] Razor pages/components call typed API clients for new or migrated business workflows.
- [ ] Razor pages do not directly call `DbContext`, repositories, stored procedures, Cloudinary, SMTP, or Infrastructure services.
- [ ] Razor pages do not inject Application services for workflows that are already API-migrated.
- [ ] UI-only concerns remain in Web: MudBlazor state, dialogs, snackbars, loading flags, validation display, local Blazor auth cookie/session creation.

### API Project

- [ ] Controllers are feature/resource-based, not centralized God controllers.
- [ ] Controllers are thin and call one Application use case per business action.
- [ ] Controllers map expected failures to safe HTTP responses.
- [ ] Controllers do not expose raw exception text, SQL errors, stack traces, password hashes, OTPs, or secrets.

### Application Project

- [ ] New or migrated use cases are modeled as CQRS commands/queries with MediatR handlers where practical.
- [ ] Commands perform state-changing workflows; queries remain read-only.
- [ ] API controllers call `IMediator.Send(...)` for CQRS use cases or one existing Application service method for transitional workflows.
- [ ] Blazor Web components do not call MediatR directly.
- [ ] Use-case methods own business validation and workflow orchestration.
- [ ] Application depends on Domain and abstractions, not Infrastructure concrete classes.
- [ ] Application DTOs do not expose database-only or security-sensitive fields.

### Infrastructure Project

- [ ] EF Core and stored-procedure details stay inside Infrastructure.
- [ ] Stored procedures are used for workflow transactions, status transitions, multi-table writes, file-resource linking, and audit writes.
- [ ] Cloudinary/SMTP/AI Hub details stay behind Application-facing interfaces.
- [ ] Cloudinary uploads are compensated/cleaned up if SQL workflow fails after upload.

### Database / Stored Procedure

- [ ] No fake SQL columns are added just to satisfy EF.
- [ ] EF mappings match the source-of-truth schema.
- [ ] Stored-procedure output parameters and errors are handled safely.

### Documentation / Session Notes

- [ ] A Markdown session note was created or updated under `docs/revision` or the matching use-case folder.
- [ ] The note uses the `yyyy-MM-dd-short-task-slug.md` naming convention.
- [ ] The note records branch name, scope, files changed, architecture flow, CQRS/MediatR changes, API endpoints, Web API clients, DB/SP behavior, build result, manual test checklist, known issues, and remaining follow-ups.
- [ ] Merge notes distinguish current/main branch decisions from incoming branch-only issues.
- [ ] The final response mentions the session note path.

## 🚀 COPILOT CLI AUTOMATION CHEATSHEET

Use these explicit commands to force Copilot CLI to leverage this skills file during pull request reviews or code generation:

```bash
# Generate a new service ensuring compliance with the skills guide
gh copilot suggest -f docs/ai_agent_skills/AI_AGENT_SKILLS_GUIDE.md "Create a CQRS MediatR command and handler for creating a ChapterPageTask assigned to an Assistant, with Web calling API through a typed client"

# Code Audit before merging a Pull Request
gh copilot explain -f docs/ai_agent_skills/AI_AGENT_SKILLS_GUIDE.md "Review current Git changes to check if the new Blazor view uses MudBlazor components and valid API requests"