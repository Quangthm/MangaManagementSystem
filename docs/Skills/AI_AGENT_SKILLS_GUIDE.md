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
  1. **Domain** — entities, enums/value objects, and core business rules.
  2. **Application** — use cases, commands, queries, DTOs, validators, service interfaces, and workflow orchestration.
  3. **Infrastructure** — Entity Framework Core, SQL Server implementation, Cloudinary integration, and external service adapters.
  4. **API** — controllers/endpoints, authentication/authorization boundaries, request/response models, and HTTP concerns.
- **Rule:** Business logic must not be placed directly in UI components or database-specific classes when it belongs in Domain/Application.

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

1. Blazor Server UI sends requests to ASP.NET Core API/application services.
2. ASP.NET Core handles authorization, validation, workflow orchestration, Cloudinary upload integration, AI Hub calls, and calls SQL Server stored procedures for important database workflows.
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
6. `design_database_workflow` ➔ Decides whether the feature requires a stored procedure/wrapper procedure and defines SQL transaction/audit boundaries.
7. `generate_acceptance_criteria` ➔ Writes BDD tests validating both C# business rules, stored-procedure effects, and Blazor UI states.
8. `sync_to_github_projects` ➔ Automates GitHub tracking using `gh` CLI.

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
    1. **Frontend (FE):** MudBlazor `.razor` component placement in the Blazor Server web project under the appropriate actor/workspace page folder.
    2. **Backend (BE):** MediatR Command/Query handlers in the `Application` layer, Domain rules in `Domain`, EF Core Fluent API configurations in `Infrastructure`, and Infrastructure repository methods that call SQL Server stored procedures for important database workflows.
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


### 5. Skill Name: `calculate_priority_matrix`
* **System Prompt Constraint:** Rank requirements to protect the project deadline.
* **Execution Rules:**
  - Score Business Value, Relative Cost, and Relative Risk from 1 to 9.
  - Automatically label anything with high cost/risk and low core value as `priority:low` or `out-of-scope`.

### 6. Skill Name: `refine_functional_requirements`
* **System Prompt Constraint:** Enforce high-precision specification language.
* **Execution Rules:**
  - Convert descriptions into `"The system shall..."` structures.
  - Replace ambiguous verbs with specific database/code operations. Specify exact C# data types, Cloudinary `secure_url` properties, stored-procedure/wrapper-procedure names when needed, SQL transaction boundaries, or coordinate ranges `(X, Y, Width, Height)` mapping to `PageRegion`.

### 7. Skill Name: `generate_acceptance_criteria`
* **System Prompt Constraint:** Write comprehensive Behavioral-Driven Development (BDD) testing scenarios.
* **Execution Rules:**
  - Use the Cucumber framework: `Given - When - Then`.
  - Must write 2 Happy Path scenarios and 2 Edge Case scenarios (e.g., unauthorized role rejection, invalid file uploads, stored-procedure rollback, Cloudinary cleanup after SQL failure, audit log write failures, or unauthorized workflow transitions).
  - Explicitly state the MudBlazor UI feedback state (e.g., `<MudProgressCircular>`, `<MudDialog>`, or `ISnackbar` toast notification).

---

## 🚀 COPILOT CLI AUTOMATION CHEATSHEET

Use these explicit commands to force Copilot CLI to leverage this skills file during pull request reviews or code generation:

```bash
# Generate a new service ensuring compliance with the skills guide
gh copilot suggest -f docs/ai_agent_skills/AI_AGENT_SKILLS_GUIDE.md "Create a MediatR command for creating a ChapterPageTask assigned to an Assistant"

# Code Audit before merging a Pull Request
gh copilot explain -f docs/ai_agent_skills/AI_AGENT_SKILLS_GUIDE.md "Review current Git changes to check if the new Blazor view uses MudBlazor components and valid API requests"